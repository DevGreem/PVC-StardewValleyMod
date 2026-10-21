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
    private readonly List<HudSpeaker> _hudSpeakers = new();
    private readonly HashSet<long> _hudSpeakerIds = new();
    private readonly Dictionary<long, int> _knownVoiceReach = new();
    private readonly LocalMonitorSource _testMonitorSource = new();

    private ModConfig _config = new();
    private ConfigManager _configManager = null!;
    private AudioEngine _audio = null!;
    private CapturePipeline _capture = null!;
    private VoiceTransport _transport = null!;
    private readonly PlayerMuteList _mutes = new();

    private SButton _pushToTalkKey = SButton.V;
    private SButton _microphoneToggleKey = SButton.F7;
    private SButton _microphoneTestKey = SButton.F9;
    private SButton _cycleRadiusKey = SButton.F8;
    private SButton _playerMuteKey = SButton.F10;
    private bool _microphoneEnabled;
    private bool _microphoneTestActive;
    private bool _microphoneWasEnabledBeforeTest;
    private bool _testPlaybackWasRunningBefore;
    private bool _testMonitorAvailable;
    private bool _effectiveWallOcclusion = true;
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

    /// <summary>The host-controlled wall-occlusion value used by local playback.</summary>
    public bool EffectiveWallOcclusion => this._effectiveWallOcclusion;

    public override void Entry(IModHelper helper)
    {
        NativeLibraryResolver.Register(this.Monitor, helper.DirectoryPath);

        this._configManager = new ConfigManager(helper, this.Monitor);
        this._config = this._configManager.Load();
        this._mutes.Rebuild(this._config);
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
        helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
        helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
        helper.Events.Multiplayer.ModMessageReceived += this.OnWallOcclusionMessageReceived;
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
        this._mutes.Rebuild(this._config);
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

    /// <summary>Change the host-authoritative wall-occlusion setting; clients cannot override it.</summary>
    public void SetWallOcclusion(bool enabled)
    {
        if (!Context.IsMainPlayer)
        {
            this.Monitor.Log("Only the host can change wall occlusion; clients follow the host.", LogLevel.Info);
            return;
        }

        if (this._config.WallOcclusion == enabled && this._effectiveWallOcclusion == enabled)
            return;

        this._config.WallOcclusion = enabled;
        this._effectiveWallOcclusion = enabled;
        this.OnConfigChanged();
        this.SendWallOcclusion();
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
        this._knownVoiceReach.Clear();
        this.ClearSpeakers();

        this._effectiveWallOcclusion = WallOcclusionSync.ResolveEffective(
            Context.IsMainPlayer,
            this._config.WallOcclusion,
            hasHostValue: false,
            hostValue: false);

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

        if (Context.IsMainPlayer)
            this.SendWallOcclusion();
        else
            this.RequestWallOcclusion();
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
        this._knownVoiceReach.Clear();
        this.ClearSpeakers();

        this._effectiveWallOcclusion = true;
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
        if (e.Button == this._playerMuteKey)
        {
            this.OpenPlayerMuteMenu();
            this.Helper.Input.Suppress(e.Button);
            return;
        }

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

    private void OpenPlayerMuteMenu()
    {
        if (!this.IsMultiplayerWorld() || Game1.player is null)
        {
            this.Monitor.Log("The player-mute menu is only available in a multiplayer world.", LogLevel.Trace);
            return;
        }

        if (Game1.activeClickableMenu is not null)
            return;

        Game1.activeClickableMenu = new PlayerMuteMenu(
            this.BuildPlayerMuteCandidates,
            this.TogglePlayerMute);
        Game1.playSound("bigSelect");
    }

    private IReadOnlyList<PlayerMuteCandidate> BuildPlayerMuteCandidates()
    {
        List<PlayerMuteCandidate> players = new();
        if (!this.IsMultiplayerWorld() || Game1.player is null)
            return players;

        foreach (Farmer farmer in Game1.otherFarmers.Values)
        {
            long playerId = farmer.UniqueMultiplayerID;
            if (playerId == Game1.player.UniqueMultiplayerID)
                continue;

            int voiceReach = this._knownVoiceReach.TryGetValue(playerId, out int knownReach)
                ? knownReach
                : ModConstants.ProximityRadii[ModConstants.ProximityRadii.Length - 1];

            players.Add(new PlayerMuteCandidate(
                playerId,
                farmer.Name,
                farmer.currentLocation?.NameOrUniqueName ?? "",
                (int)farmer.Tile.X,
                (int)farmer.Tile.Y,
                voiceReach,
                this._mutes.IsMuted(playerId)));
        }

        return players;
    }

    private void TogglePlayerMute(long playerId, string playerName)
    {
        this._mutes.Toggle(this._config, playerId, playerName);
        this.SaveConfig();
        this.Monitor.Log($"Player {playerName} is now {(this._mutes.IsMuted(playerId) ? "muted" : "unmuted")} locally.", LogLevel.Info);
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        this.Monitor.Log($"Player {e.Peer.PlayerID} disconnected; releasing their voice state.", LogLevel.Trace);
        this._transport.Release(e.Peer.PlayerID);
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        this.SendWallOcclusion(e.Peer.PlayerID);
    }

    private void SendWallOcclusion(long? playerId = null)
    {
        if (!Context.IsMainPlayer || !this.IsMultiplayerWorld())
            return;

        this.Helper.Multiplayer.SendMessage(
            new WallOcclusionMessage { Enabled = this._config.WallOcclusion },
            ModConstants.WallOcclusionMessageType,
            new[] { ModConstants.ModId },
            playerId.HasValue ? new[] { playerId.Value } : null);
    }

    private void RequestWallOcclusion()
    {
        if (Context.IsMainPlayer || !this.IsMultiplayerWorld())
            return;

        long hostPlayerId = Game1.MasterPlayer?.UniqueMultiplayerID ?? 0L;
        this.Helper.Multiplayer.SendMessage(
            new WallOcclusionMessage { Request = true },
            ModConstants.WallOcclusionMessageType,
            new[] { ModConstants.ModId },
            hostPlayerId != 0L ? new[] { hostPlayerId } : null);
    }

    private void OnWallOcclusionMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMultiplayer)
            return;

        if (!WallOcclusionSync.IsSettingsMessage(e.FromModID, e.Type))
            return;

        WallOcclusionMessage? message;
        try
        {
            message = e.ReadAs<WallOcclusionMessage>();
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Discarded a wall-occlusion setting that could not be read: {ex.Message}", LogLevel.Trace);
            return;
        }

        if (message is null)
            return;

        if (Context.IsMainPlayer)
        {
            long localPlayerId = Game1.player?.UniqueMultiplayerID ?? 0L;
            if (WallOcclusionSync.ShouldReplyToRequest(
                isMainPlayer: true,
                e.FromPlayerID,
                localPlayerId,
                e.FromModID,
                e.Type,
                message.Request))
            {
                this.SendWallOcclusion(e.FromPlayerID);
            }

            return;
        }

        if (message.Request)
            return;

        long hostPlayerId = Game1.MasterPlayer?.UniqueMultiplayerID ?? 0L;
        if (!WallOcclusionSync.ShouldApplyFromHost(
            Context.IsMainPlayer,
            e.FromPlayerID,
            hostPlayerId,
            e.FromModID,
            e.Type))
        {
            return;
        }

        this._effectiveWallOcclusion = message.Enabled;
        this.Monitor.Log($"Host wall occlusion: {(message.Enabled ? "enabled" : "disabled")}.", LogLevel.Info);
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

        this._knownVoiceReach[playerId] = packet.ProximityRadius;

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
        channel.Receive(packet, GetTimestampSeconds(), enqueueAudio: !this._mutes.IsMuted(playerId));
    }

    private void OnPeerLost(long playerId)
    {
        this._knownVoiceReach.Remove(playerId);

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
        this._hudSpeakers.Clear();
        this._hudSpeakerIds.Clear();

        foreach (SpeakerChannel channel in this._speakers.Values)
        {
            bool isMuted = this._mutes.IsMuted(channel.PlayerId);
            if (!IndicatorRules.IsNearbySpeakerVisible(channel.IsNearby, channel.IsSpeaking(now), isMuted))
                continue;

            this.AddHudSpeaker(channel.PlayerId, channel.PlayerName, isMuted, channel.DistanceTiles);
        }

        // A muted player may be silent and have no fresh voice packet, so keep
        // them listed from the live farmer data while they are in range.
        if (Game1.player?.currentLocation is not null && Game1.otherFarmers is not null)
        {
            string localLocation = Game1.player.currentLocation.NameOrUniqueName;
            foreach (Farmer farmer in Game1.otherFarmers.Values)
            {
                long playerId = farmer.UniqueMultiplayerID;
                if (playerId == Game1.player.UniqueMultiplayerID
                    || !this._mutes.IsMuted(playerId)
                    || this._hudSpeakerIds.Contains(playerId)
                    || farmer.currentLocation is null
                    || !ProximityMath.IsSameLocation(localLocation, farmer.currentLocation.NameOrUniqueName))
                {
                    continue;
                }

                float distance = Vector2.Distance(farmer.Tile, Game1.player.Tile);
                int reach = this._speakers.TryGetValue(playerId, out SpeakerChannel? channel)
                    ? channel.ProximityRadius
                    : Math.Max(this._config.ProximityRadius, ModConstants.DefaultProximityRadius);
                if (!ProximityMath.IsInRange(distance, reach))
                    continue;

                this.AddHudSpeaker(playerId, farmer.Name, isMuted: true, distanceTiles: distance);
            }
        }

        this._hudSpeakers.Sort((first, second) =>
        {
            int distanceOrder = first.DistanceTiles.CompareTo(second.DistanceTiles);
            return distanceOrder != 0
                ? distanceOrder
                : StringComparer.OrdinalIgnoreCase.Compare(first.Name, second.Name);
        });

        int speakerCount = Math.Min(this._hudSpeakers.Count, 8);
        VoiceIndicatorSpeaker[] speakers = new VoiceIndicatorSpeaker[speakerCount];
        for (int i = 0; i < speakerCount; i++)
            speakers[i] = new VoiceIndicatorSpeaker(this._hudSpeakers[i].Name, this._hudSpeakers[i].IsMuted);

        this._indicator.Draw(
            e.SpriteBatch,
            this._microphoneEnabled,
            this._capture.IsTransmittingNow,
            this._config.ActivationMode,
            this._config.ProximityRadius,
            speakers);
    }

    private void AddHudSpeaker(long playerId, string name, bool isMuted, double distanceTiles)
    {
        if (!this._hudSpeakerIds.Add(playerId))
            return;

        this._hudSpeakers.Add(new HudSpeaker(name, isMuted, distanceTiles));
    }

    private readonly record struct HudSpeaker(string Name, bool IsMuted, double DistanceTiles);

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
        this._mixer.SetCaveMode(false);
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
        this._mixer.SetCaveMode(false);

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
        this._mixer.SetCaveMode(false);

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
            this._mixer.SetCaveMode(false);
            this._mixer.SetChannels(Array.Empty<SpeakerChannel>());
            return;
        }

        double now = GetTimestampSeconds();
        string localLocation = Game1.player.currentLocation?.NameOrUniqueName ?? "";
        bool inCave = EnvironmentAcoustics.IsCave(Game1.player.currentLocation);
        this._mixer.SetCaveMode(inCave);
        this._mixer.SetMasterVolume(this._config.PlaybackVolume / 100f);
        this._speakerScratch.Clear();

        foreach (KeyValuePair<long, SpeakerChannel> pair in this._speakers)
        {
            SpeakerChannel channel = pair.Value;
            channel.IsNearby = false;
            double lastPacket = channel.LastPacketTime;
            if (lastPacket > 0d
                && now - lastPacket > ModConstants.StaleChannelMilliseconds / 1000d
                && !this._mutes.IsMuted(pair.Key))
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
                channel.OcclusionAlphaTarget = 1f;
                continue;
            }

            float distance = Vector2.Distance(farmer.Tile, Game1.player.Tile);
            channel.DistanceTiles = distance;

            if (!ProximityMath.IsInRange(distance, channel.ProximityRadius))
            {
                channel.Volume = 0f;
                channel.OcclusionAlphaTarget = 1f;
                continue;
            }

            channel.IsNearby = true;

            if (this._mutes.IsMuted(channel.PlayerId))
            {
                channel.Volume = 0f;
                channel.OcclusionAlphaTarget = 1f;
                channel.DiscardBufferedAudio();
                continue;
            }

            float distanceGain = ProximityMath.CalculateVolume(
                localLocation,
                farmer.currentLocation.NameOrUniqueName,
                distance,
                channel.ProximityRadius);

            if (distanceGain <= 0f)
            {
                channel.Volume = 0f;
                channel.OcclusionAlphaTarget = 1f;
                continue;
            }

            int obstacles = this._effectiveWallOcclusion
                ? WallOcclusion.CountObstacles(Game1.player.currentLocation, farmer.Tile, Game1.player.Tile)
                : 0;

            float gain = distanceGain * OcclusionMath.Gain(obstacles);
            float alpha = OcclusionMath.LowPassAlpha(obstacles);

            channel.Volume = EnvironmentAcoustics.ApplyGain(gain, inCave);
            channel.OcclusionAlphaTarget = EnvironmentAcoustics.ApplyLowPassAlpha(alpha, inCave);

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
        this._playerMuteKey = KeybindUtil.Parse(this._config.PlayerMuteKey, SButton.F10);
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
        this._mixer.SetCaveMode(false);
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
