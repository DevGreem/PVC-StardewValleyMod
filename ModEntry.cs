using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProximityVoiceChat.Audio;
using ProximityVoiceChat.Config;
using ProximityVoiceChat.Integrations;
using ProximityVoiceChat.Network;
using ProximityVoiceChat.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ProximityVoiceChat;

/// <summary>SMAPI entry point: wires lifecycle hooks, capture, transport, playback and the HUD indicator.</summary>
public sealed class ModEntry : Mod
{
    private readonly PlaybackMixer _mixer = new();
    private readonly VoiceIndicator _indicator = new();
    private readonly ConcurrentDictionary<long, SpeakerChannel> _speakers = new();
    private readonly List<SpeakerChannel> _speakerScratch = new();
    private readonly LocalMonitorSource _testMonitorSource = new();

    private ModConfig _config = new();
    private ConfigManager _configManager = null!;
    private AudioEngine _audio = null!;
    private CapturePipeline _capture = null!;
    private VoiceTransport _transport = null!;

    private SButton _pushToTalkKey = SButton.V;
    private SButton _microphoneToggleKey = SButton.F7;
    private SButton _microphoneTestKey = SButton.F9;
    private SButton _cycleRadiusKey = SButton.F8;
    private bool _microphoneEnabled;
    private bool _microphoneTestActive;
    private bool _microphoneWasEnabledBeforeTest;
    private bool _testPlaybackWasRunningBefore;
    private bool _testMonitorAvailable;
    private volatile float _microphoneTestLevel;
    private bool _pushToTalkHeld;
    private bool _loggedUpdateTick;
    private bool _loggedModMessageHook;
    private long _lastFramesFilled;
    private long _lastFramesTransmitted;
    private double _nextPlaybackAttempt;

    /// <summary>The live configuration object shared with the settings panel.</summary>
    public ModConfig Config => this._config;

    public bool IsMicrophoneEnabled => this._microphoneEnabled;

    public bool IsMicrophoneTestActive => this._microphoneTestActive;

    public bool IsMicrophoneTestMonitoring => this._microphoneTestActive && this._testMonitorAvailable;

    public float MicrophoneTestLevel => this._microphoneTestLevel;

    public override void Entry(IModHelper helper)
    {
        NativeLibraryResolver.Register(this.Monitor, helper.DirectoryPath);

        this._configManager = new ConfigManager(helper, this.Monitor);
        this._config = this._configManager.Load();
        this.ParseKeys();
        this._mixer.SetMasterVolume(this._config.PlaybackVolume / 100f);

        this._audio = new AudioEngine(this.Monitor);
        this._transport = new VoiceTransport(helper, this.Monitor);
        this._capture = new CapturePipeline(this._transport.Enqueue);

        this._transport.PacketReceived += this.OnVoicePacketReceived;
        this._transport.PeerLost += this.OnPeerLost;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Input.ButtonReleased += this.OnButtonReleased;
        helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
    }

    /// <summary>Read the device list for the settings panel.</summary>
    public IReadOnlyList<string> GetInputDevices()
    {
        return this._audio.GetInputDevices();
    }

    /// <summary>Read the output device list for the settings panel.</summary>
    public IReadOnlyList<string> GetOutputDevices()
    {
        return this._audio.GetOutputDevices();
    }

    /// <summary>Apply a settings change live without restarting the game.</summary>
    public void OnConfigChanged()
    {
        this._config.Normalize();
        this.ParseKeys();
        this.UpdateCaptureSettings();
        this._mixer.SetMasterVolume(this._config.PlaybackVolume / 100f);

        if (this._microphoneTestActive)
            this.ApplyTestMonitor();

        this._audio.ApplyDevices(this._config.InputDeviceName, this._config.OutputDeviceName);

        if (this._microphoneEnabled && !this.IsMultiplayerWorld())
            this.SetMicrophoneEnabled(false);
    }

    /// <summary>Persist the current settings.</summary>
    public void SaveConfig()
    {
        this._configManager.Save(this._config);
    }

    /// <summary>Toggle the microphone and open or close the capture stream immediately.</summary>
    public void SetMicrophoneEnabled(bool enabled)
    {
        if (this._microphoneTestActive)
        {
            this.Monitor.Log("The microphone test is active; stop the test before changing normal microphone state.", LogLevel.Warn);
            return;
        }

        if (enabled == this._microphoneEnabled)
            return;

        if (enabled)
        {
            if (!this.IsMultiplayerWorld())
            {
                this.Monitor.Log("The microphone can only be enabled in a loaded multiplayer world.", LogLevel.Warn);
                return;
            }

            if (!this._audio.IsAvailable)
            {
                this.Monitor.Log("The microphone cannot be enabled because the audio backend is unavailable.", LogLevel.Error);
                return;
            }

            this._microphoneEnabled = true;
            this._capture.MicrophoneEnabled = true;

            if (!this._audio.StartCapture(this._config.InputDeviceName, this._capture.AppendSamples, out string error))
            {
                this._microphoneEnabled = false;
                this._capture.MicrophoneEnabled = false;
                this._audio.StopCapture();
                this.Monitor.Log($"Voice capture disabled: {error}.", LogLevel.Error);
                return;
            }

            this.Monitor.Log("Microphone enabled.", LogLevel.Info);
            return;
        }

        this._microphoneEnabled = false;
        this._capture.MicrophoneEnabled = false;
        this._audio.StopCapture();
        this._transport.ClearOutgoing();
        this.Monitor.Log("Microphone disabled.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Monitor.Log($"Proximity Voice Chat {this.ModManifest.Version} launched.", LogLevel.Info);
        this._audio.Initialize();
        new GmcmIntegration(this, this.Helper, this.Monitor, this._config).Register();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Monitor.Log($"Save loaded. Multiplayer world: {Context.IsMultiplayer}.", LogLevel.Info);

        this.StopMicrophoneTest(restoreMicrophone: false);
        this._microphoneEnabled = false;
        this._capture.MicrophoneEnabled = false;
        this._capture.ResetSession();
        this._audio.StopCapture();
        this._transport.ReleaseAll();
        this.ClearSpeakers();

        this._capture.SessionActive = this.IsMultiplayerWorld();
        this.UpdateCaptureSettings();
        this._nextPlaybackAttempt = 0d;
        this._mixer.SetMasterVolume(this._config.PlaybackVolume / 100f);

        if (!this.IsMultiplayerWorld())
        {
            this._audio.StopPlayback();
            return;
        }

        if (!this._audio.IsPlaybackRunning && !this._audio.StartPlayback(this._config.OutputDeviceName, this._mixer, out string error))
            this.Monitor.Log($"Incoming voice is unavailable: {error}.", LogLevel.Error);

        this.StartMicrophoneFromConfig();
    }

    /// <summary>Apply the persisted start preference once the multiplayer session is ready.</summary>
    private void StartMicrophoneFromConfig()
    {
        bool shouldStart = MicrophoneStartPolicy.ShouldStartMicrophone(
            this._config.StartMicrophoneEnabled,
            Context.IsWorldReady,
            this.IsMultiplayerWorld(),
            this._audio.IsAvailable);

        if (shouldStart)
        {
            this.SetMicrophoneEnabled(true);
            return;
        }

        if (this._config.StartMicrophoneEnabled && this.IsMultiplayerWorld() && !this._audio.IsAvailable)
            this.Monitor.Log("StartMicrophoneEnabled is enabled, but the audio backend is unavailable; the session starts muted.", LogLevel.Warn);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.Monitor.Log("Returned to title; stopping voice capture and playback.", LogLevel.Info);

        this.StopMicrophoneTest(restoreMicrophone: false);
        this._microphoneEnabled = false;
        this._capture.MicrophoneEnabled = false;
        this._capture.SessionActive = false;
        this._audio.StopCapture();
        this._audio.StopPlayback();
        this._transport.ReleaseAll();
        this.ClearSpeakers();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!this._loggedUpdateTick)
        {
            this._loggedUpdateTick = true;
            this.Monitor.Log("Update tick hook active.", LogLevel.Trace);
        }

        this._pushToTalkHeld = this.IsMultiplayerWorld() && this.Helper.Input.IsDown(this._pushToTalkKey);
        this._capture.PushToTalkHeld = this._pushToTalkHeld;
        this.UpdateCaptureSettings();

        if (this._microphoneTestActive && !Context.IsWorldReady)
            this.StopMicrophoneTest(restoreMicrophone: false);

        this.EnsurePlaybackRunning();
        this._transport.DrainOutgoing();
        this.UpdateSpeakerState();
    }

    private void EnsurePlaybackRunning()
    {
        if (this._microphoneTestActive || !this._audio.IsAvailable || !this.IsMultiplayerWorld() || this._audio.IsPlaybackRunning)
            return;

        double now = GetTimestampSeconds();
        if (now < this._nextPlaybackAttempt)
            return;

        this._nextPlaybackAttempt = now + 5d;
        if (!this._audio.StartPlayback(this._config.OutputDeviceName, this._mixer, out string error))
            this.Monitor.Log($"Incoming voice is unavailable: {error}.", LogLevel.Warn);
    }

    private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        this._audio.EnsureDevices();

        if (this._microphoneTestActive)
        {
            if (!this._audio.IsCaptureRunning)
            {
                this.Monitor.Log("Microphone test stopped because the input device is no longer available.", LogLevel.Warn);
                this.StopMicrophoneTest(restoreMicrophone: false);
                return;
            }

            if (this._testMonitorAvailable && !this._audio.IsPlaybackRunning)
            {
                this._testMonitorAvailable = false;
                this._testMonitorSource.MonitoringEnabled = false;
                this.Monitor.Log("Microphone test monitoring stopped because the output device is no longer available; the level meter remains active.", LogLevel.Warn);
            }
        }

        long filled = this._capture.FramesFilled;
        long transmitted = this._capture.FramesTransmitted;
        long filledDelta = filled - this._lastFramesFilled;
        long transmittedDelta = transmitted - this._lastFramesTransmitted;
        this._lastFramesFilled = filled;
        this._lastFramesTransmitted = transmitted;

        if (this._microphoneEnabled)
            this.Monitor.Log($"Capture active: {filledDelta} frame(s) filled, {transmittedDelta} transmitted in the last second.", LogLevel.Trace);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == this._microphoneTestKey)
        {
            this.SetMicrophoneTestActive(!this._microphoneTestActive);
            this.Helper.Input.Suppress(e.Button);
            this.Monitor.Log($"Microphone test toggle hook fired ({e.Button}).", LogLevel.Trace);
            return;
        }

        if (e.Button == this._microphoneToggleKey)
        {
            this.SetMicrophoneEnabled(!this._microphoneEnabled);
            this.Helper.Input.Suppress(e.Button);
            this.Monitor.Log($"Microphone toggle hook fired ({e.Button}).", LogLevel.Trace);
            return;
        }

        if (e.Button == this._cycleRadiusKey)
        {
            this._config.ProximityRadius = ModConfig.NextRadius(this._config.ProximityRadius);
            this.UpdateCaptureSettings();
            this.SaveConfig();
            this.Helper.Input.Suppress(e.Button);
            this.Monitor.Log($"Voice reach changed to {this._config.ProximityRadius} tiles.", LogLevel.Info);
            return;
        }

        if (e.Button == this._pushToTalkKey)
        {
            this._pushToTalkHeld = true;
            this._capture.PushToTalkHeld = true;
            this.Monitor.Log($"Push-to-talk hook fired ({e.Button}).", LogLevel.Trace);
        }
    }

    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        if (e.Button == this._pushToTalkKey)
        {
            this._pushToTalkHeld = false;
            this._capture.PushToTalkHeld = false;
            this._transport.ClearOutgoing();
            this.Monitor.Log($"Push-to-talk released ({e.Button}).", LogLevel.Trace);
        }
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        this.Monitor.Log($"Player {e.Peer.PlayerID} disconnected; releasing their voice state.", LogLevel.Trace);
        this._transport.Release(e.Peer.PlayerID);
    }

    private void OnVoicePacketReceived(long playerId, VoicePacket packet)
    {
        if (this._microphoneTestActive)
            return;

        if (!this._loggedModMessageHook)
        {
            this._loggedModMessageHook = true;
            this.Monitor.Log("Mod message received hook active; accepting voice packets.", LogLevel.Trace);
        }

        if (!this.IsMultiplayerWorld() || Game1.player is null)
            return;

        if (!Game1.otherFarmers.TryGetValue(playerId, out Farmer? farmer) || farmer?.currentLocation is null)
            return;

        string localLocation = Game1.player.currentLocation?.NameOrUniqueName ?? "";
        string speakerLocation = farmer.currentLocation.NameOrUniqueName;
        if (!ProximityMath.IsSameLocation(localLocation, speakerLocation))
            return;

        // Distance is evaluated in UpdateSpeakerState with the radius declared by
        // the packet, so a speaker can shrink their reach and be updated immediately.
        float distance = Vector2.Distance(farmer.Tile, Game1.player.Tile);

        SpeakerChannel channel = this._speakers.GetOrAdd(playerId, _ => new SpeakerChannel(playerId, farmer.Name));
        channel.PlayerName = farmer.Name;
        channel.DistanceTiles = distance;
        channel.Receive(packet, GetTimestampSeconds());
    }

    private void OnPeerLost(long playerId)
    {
        if (this._speakers.TryRemove(playerId, out SpeakerChannel? channel))
        {
            channel.Volume = 0f;
            channel.Dispose();
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (this._microphoneTestActive)
        {
            this._indicator.DrawMicrophoneTest(
                e.SpriteBatch,
                this._microphoneTestLevel,
                this.IsMicrophoneTestMonitoring,
                this._config.PlaybackVolume,
                this._microphoneTestKey.ToString(),
                Context.IsMultiplayer);
            return;
        }

        if (!IndicatorRules.ShouldDraw(this._config.ShowIndicator, Context.IsWorldReady, Context.IsMultiplayer) || !this.IsMultiplayerWorld())
            return;

        double now = GetTimestampSeconds();
        this._speakerScratch.Clear();

        foreach (SpeakerChannel channel in this._speakers.Values)
        {
            if (IndicatorRules.IsNearbySpeaking(channel.Volume, channel.IsSpeaking(now)))
                this._speakerScratch.Add(channel);
        }

        this._speakerScratch.Sort((first, second) => first.DistanceTiles.CompareTo(second.DistanceTiles));
        string[] names = new string[Math.Min(this._speakerScratch.Count, 8)];
        for (int i = 0; i < names.Length; i++)
            names[i] = this._speakerScratch[i].PlayerName;

        this._indicator.Draw(
            e.SpriteBatch,
            this._microphoneEnabled,
            this._capture.IsTransmittingNow,
            this._config.ActivationMode,
            this._config.ProximityRadius,
            names);
    }

    /// <summary>Start or stop the local microphone test from the panel or keybind.</summary>
    public void SetMicrophoneTestActive(bool active)
    {
        if (active == this._microphoneTestActive)
            return;

        if (active)
            this.StartMicrophoneTest();
        else
            this.StopMicrophoneTest(restoreMicrophone: true);
    }

    private void StartMicrophoneTest()
    {
        if (!MicrophoneTestPolicy.CanStart(Context.IsWorldReady, this._audio.IsAvailable))
        {
            this.Monitor.Log("The microphone test requires a loaded world and an available audio backend.", LogLevel.Warn);
            return;
        }

        this._microphoneWasEnabledBeforeTest = this._microphoneEnabled;
        this._testPlaybackWasRunningBefore = this._audio.IsPlaybackRunning;

        this._microphoneEnabled = false;
        this._capture.MicrophoneEnabled = false;
        this._audio.StopCapture();
        this._transport.ClearOutgoing();
        this.ClearSpeakers();
        this._testMonitorSource.Clear();
        this._testMonitorSource.MonitoringEnabled = false;
        this._testMonitorAvailable = false;
        this._microphoneTestLevel = 0f;
        this._microphoneTestActive = true;

        if (!this._audio.StartCapture(this._config.InputDeviceName, this.OnTestSamples, out string error))
        {
            this._microphoneTestActive = false;
            this._testMonitorSource.Clear();
            this._microphoneEnabled = false;
            this._capture.MicrophoneEnabled = false;

            if (this._microphoneWasEnabledBeforeTest && this.IsMultiplayerWorld())
                this.SetMicrophoneEnabled(true);

            this.Monitor.Log($"Microphone test could not start: {error}.", LogLevel.Error);
            return;
        }

        this.ConfigureTestMonitor();
        this.Monitor.Log($"Microphone test started (voice chat paused; monitor: {this._config.MicrophoneTestMonitor}).", LogLevel.Info);
    }

    private void StopMicrophoneTest(bool restoreMicrophone)
    {
        if (!this._microphoneTestActive)
            return;

        this._microphoneTestActive = false;
        this._audio.StopCapture();
        this._testMonitorSource.MonitoringEnabled = false;
        this._testMonitorSource.Clear();
        this._testMonitorAvailable = false;
        this._microphoneTestLevel = 0f;
        this._mixer.SetSources(Array.Empty<IAudioFrameSource>());

        if (!this._testPlaybackWasRunningBefore && !this.IsMultiplayerWorld())
            this._audio.StopPlayback();

        this._microphoneEnabled = false;
        this._capture.MicrophoneEnabled = false;

        if (restoreMicrophone && this._microphoneWasEnabledBeforeTest && this.IsMultiplayerWorld())
            this.SetMicrophoneEnabled(true);

        this.Monitor.Log("Microphone test stopped.", LogLevel.Info);
    }

    private void OnTestSamples(float[] samples, int count)
    {
        this._testMonitorSource.AppendSamples(samples, count);
        this._microphoneTestLevel = this._testMonitorSource.Level;
    }

    private void ApplyTestMonitor()
    {
        if (this._microphoneTestActive)
            this.ConfigureTestMonitor();
    }

    /// <summary>Apply the monitor setting to the active test without restarting normal voice chat.</summary>
    private void ConfigureTestMonitor()
    {
        this._testMonitorAvailable = false;

        if (!this._config.MicrophoneTestMonitor)
        {
            this._testMonitorSource.MonitoringEnabled = false;
            this._mixer.SetSources(Array.Empty<IAudioFrameSource>());
            if (!this.IsMultiplayerWorld())
                this._audio.StopPlayback();

            return;
        }

        this._testMonitorSource.MonitoringEnabled = true;

        if (!this._audio.IsPlaybackRunning
            && !this._audio.StartPlayback(this._config.OutputDeviceName, this._mixer, out string error))
        {
            this._testMonitorSource.MonitoringEnabled = false;
            this.Monitor.Log($"Microphone test monitor unavailable: {error}.", LogLevel.Warn);
            return;
        }

        this._mixer.SetMasterVolume(this._config.PlaybackVolume / 100f);
        this._mixer.SetSources(new IAudioFrameSource[] { this._testMonitorSource });
        this._testMonitorAvailable = true;
    }

    private void UpdateSpeakerState()
    {
        if (this._microphoneTestActive)
            return;

        if (!this.IsMultiplayerWorld() || Game1.player is null)
        {
            this._mixer.SetChannels(Array.Empty<SpeakerChannel>());
            return;
        }

        double now = GetTimestampSeconds();
        string localLocation = Game1.player.currentLocation?.NameOrUniqueName ?? "";
        this._mixer.SetMasterVolume(this._config.PlaybackVolume / 100f);
        this._speakerScratch.Clear();

        foreach (KeyValuePair<long, SpeakerChannel> pair in this._speakers)
        {
            SpeakerChannel channel = pair.Value;
            double lastPacket = channel.LastPacketTime;
            if (lastPacket > 0d && now - lastPacket > ModConstants.StaleChannelMilliseconds / 1000d)
            {
                if (this._speakers.TryRemove(pair.Key, out SpeakerChannel? removed))
                {
                    removed.Volume = 0f;
                    removed.Dispose();
                }

                continue;
            }

            if (!Game1.otherFarmers.TryGetValue(pair.Key, out Farmer? farmer) || farmer?.currentLocation is null)
            {
                channel.Volume = 0f;
                continue;
            }

            if (!ProximityMath.IsSameLocation(localLocation, farmer.currentLocation.NameOrUniqueName))
            {
                channel.Volume = 0f;
                continue;
            }

            float distance = Vector2.Distance(farmer.Tile, Game1.player.Tile);
            channel.DistanceTiles = distance;

            if (!ProximityMath.IsInRange(distance, channel.ProximityRadius))
            {
                channel.Volume = 0f;
                continue;
            }

            channel.Volume = ProximityMath.CalculateVolume(
                localLocation,
                farmer.currentLocation.NameOrUniqueName,
                distance,
                channel.ProximityRadius);

            if (channel.Volume > 0f)
                this._speakerScratch.Add(channel);
        }

        this._speakerScratch.Sort((first, second) => first.DistanceTiles.CompareTo(second.DistanceTiles));
        int count = Math.Min(ModConstants.MaxConcurrentSpeakers, this._speakerScratch.Count);
        SpeakerChannel[] snapshot = new SpeakerChannel[count];
        for (int i = 0; i < count; i++)
            snapshot[i] = this._speakerScratch[i];

        this._mixer.SetChannels(snapshot);
    }

    private void UpdateCaptureSettings()
    {
        this._capture.MicrophoneEnabled = this._microphoneEnabled;
        this._capture.SessionActive = this.IsMultiplayerWorld();
        this._capture.PushToTalkHeld = this._pushToTalkHeld;
        this._capture.ActivationMode = this._config.ActivationMode;
        this._capture.Sensitivity = this._config.VoiceSensitivity;
        this._capture.ProximityRadius = this._config.ProximityRadius;
        this._capture.LocationName = Game1.player?.currentLocation?.NameOrUniqueName ?? "";
    }

    private void ParseKeys()
    {
        this._pushToTalkKey = KeybindUtil.Parse(this._config.PushToTalkKey, SButton.V);
        this._microphoneToggleKey = KeybindUtil.Parse(this._config.MicrophoneToggleKey, SButton.F7);
        this._microphoneTestKey = KeybindUtil.Parse(this._config.MicrophoneTestKey, SButton.F9);
        this._cycleRadiusKey = KeybindUtil.Parse(this._config.CycleRadiusKey, SButton.F8);
    }

    private bool IsMultiplayerWorld()
    {
        return Context.IsWorldReady
            && Context.IsMultiplayer
            && Game1.player is not null
            && Game1.otherFarmers is not null;
    }

    private void ClearSpeakers()
    {
        this._mixer.SetChannels(Array.Empty<SpeakerChannel>());

        foreach (SpeakerChannel channel in this._speakers.Values)
            channel.Dispose();

        this._speakers.Clear();
    }

    private static double GetTimestampSeconds()
    {
        return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    }
}
