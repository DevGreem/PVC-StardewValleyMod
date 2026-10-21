using System;
using System.Collections.Generic;
using System.Globalization;
using ProximityVoiceChat.Config;
using StardewModdingAPI;

namespace ProximityVoiceChat.Integrations;

/// <summary>Registers the optional Generic Mod Config Menu panel.</summary>
public sealed class GmcmIntegration
{
    private const string ApiModId = "spacechase0.GenericModConfigMenu";

    private readonly ModEntry _mod;
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly ModConfig _config;
    private IGenericModConfigMenuApi? _api;

    public GmcmIntegration(ModEntry mod, IModHelper helper, IMonitor monitor, ModConfig config)
    {
        this._mod = mod;
        this._helper = helper;
        this._monitor = monitor;
        this._config = config;
    }

    public void Register()
    {
        this._api = this._helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ApiModId);
        if (this._api is null)
        {
            this._monitor.Log("Generic Mod Config Menu is not installed; using the settings file.", LogLevel.Trace);
            return;
        }

        IGenericModConfigMenuApi api = this._api;
        IManifest manifest = this._mod.ModManifest;

        api.Register(manifest, this.ResetToDefaults, this._mod.SaveConfig);

        api.AddSectionTitle(manifest, () => "Audio devices");
        string[] inputDevices = this.BuildDeviceValues(this._mod.GetInputDevices(), this._config.InputDeviceName);
        string[] outputDevices = this.BuildDeviceValues(this._mod.GetOutputDevices(), this._config.OutputDeviceName);

        api.AddTextOption(
            manifest,
            () => this._config.InputDeviceName,
            value => { this._config.InputDeviceName = value; this._mod.OnConfigChanged(); },
            () => "Input device",
            () => "Microphone used for voice capture. The system default is used when unset.",
            inputDevices,
            FormatDevice,
            "input-device");

        api.AddTextOption(
            manifest,
            () => this._config.OutputDeviceName,
            value => { this._config.OutputDeviceName = value; this._mod.OnConfigChanged(); },
            () => "Output device",
            () => "Device that plays incoming voice. The system default is used when unset.",
            outputDevices,
            FormatDevice,
            "output-device");

        api.AddNumberOption(
            manifest,
            () => this._config.PlaybackVolume,
            value => { this._config.PlaybackVolume = value; this._mod.OnConfigChanged(); },
            () => "Playback volume",
            () => "Volume of incoming voice, from 0 to 100.",
            min: 0,
            max: 100,
            interval: 5,
            fieldId: "playback-volume");

        api.AddSectionTitle(manifest, () => "Voice");
        string[] radiusValues = Array.ConvertAll(ModConstants.ProximityRadii, value => value.ToString(CultureInfo.InvariantCulture));
        api.AddTextOption(
            manifest,
            () => this._config.ProximityRadius.ToString(CultureInfo.InvariantCulture),
            value =>
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius) && ModConfig.IsOfferedRadius(radius))
                    this._config.ProximityRadius = radius;

                this._mod.OnConfigChanged();
            },
            () => "Voice reach",
            () => "How far your voice carries, in tiles. This is your outgoing reach: it does not filter the voice you hear, because each speaker's own reach controls that.",
            radiusValues,
            value => $"{value} tiles",
            "proximity-radius");

        string[] modes = { nameof(ActivationMode.PushToTalk), nameof(ActivationMode.VoiceActivity) };
        api.AddTextOption(
            manifest,
            () => this._config.ActivationMode.ToString(),
            value =>
            {
                if (Enum.TryParse(value, ignoreCase: true, out ActivationMode mode))
                    this._config.ActivationMode = mode;

                this._mod.OnConfigChanged();
            },
            () => "Activation mode",
            () => "Push-to-talk transmits only while the key is held; voice activity transmits frames above the sensitivity threshold.",
            modes,
            FormatActivationMode,
            "activation-mode");

        api.AddNumberOption(
            manifest,
            () => this._config.VoiceSensitivity,
            value => { this._config.VoiceSensitivity = value; this._mod.OnConfigChanged(); },
            () => "Voice sensitivity",
            () => "Voice-activity threshold: lower values require louder speech, higher values pick up quieter speech.",
            min: 0,
            max: 100,
            interval: 5,
            fieldId: "voice-sensitivity");

        api.AddBoolOption(
            manifest,
            () => this._config.StartMicrophoneEnabled,
            value =>
            {
                this._config.StartMicrophoneEnabled = value;
                this._mod.OnConfigChanged();
                this._mod.SaveConfig();
            },
            () => "Start microphone enabled",
            () => "When enabled, the next multiplayer world starts with the microphone on. This changes the saved preference only; the current session microphone is not changed.",
            "start-microphone-enabled");

        api.AddKeybind(
            manifest,
            () => KeybindUtil.Parse(this._config.PushToTalkKey, SButton.V),
            value => { this._config.PushToTalkKey = KeybindUtil.Format(value); this._mod.OnConfigChanged(); },
            () => "Push-to-talk key",
            () => "Hold to transmit in push-to-talk mode.",
            "push-to-talk-key");

        api.AddKeybind(
            manifest,
            () => KeybindUtil.Parse(this._config.MicrophoneToggleKey, SButton.F7),
            value => { this._config.MicrophoneToggleKey = KeybindUtil.Format(value); this._mod.OnConfigChanged(); },
            () => "Microphone toggle key",
            () => "Toggles the microphone during play.",
            "microphone-toggle-key");

        api.AddKeybind(
            manifest,
            () => KeybindUtil.Parse(this._config.MicrophoneTestKey, SButton.F9),
            value => { this._config.MicrophoneTestKey = KeybindUtil.Format(value); this._mod.OnConfigChanged(); },
            () => "Microphone test key",
            () => "Starts and stops the local microphone test. Test audio is never transmitted.",
            "microphone-test-key");

        api.AddKeybind(
            manifest,
            () => KeybindUtil.Parse(this._config.CycleRadiusKey, SButton.F8),
            value => { this._config.CycleRadiusKey = KeybindUtil.Format(value); this._mod.OnConfigChanged(); },
            () => "Cycle radius key",
            () => "Cycles through the offered proximity radii.",
            "cycle-radius-key");

        api.AddSectionTitle(manifest, () => "Interface");
        api.AddBoolOption(
            manifest,
            () => this._config.MicrophoneTestMonitor,
            value => { this._config.MicrophoneTestMonitor = value; this._mod.OnConfigChanged(); },
            () => "Monitor microphone during test",
            () => "Plays the local microphone test through the selected output at Playback volume. Use headphones to avoid feedback; off by default.",
            "microphone-test-monitor");

        api.AddBoolOption(
            manifest,
            () => this._mod.IsMicrophoneTestActive,
            value => this._mod.SetMicrophoneTestActive(value),
            () => "Microphone test active",
            () => "Starts or stops the local microphone test. Session only: the test never transmits voice and is not saved.",
            "microphone-test-active");

        api.AddBoolOption(
            manifest,
            () => this._config.ShowIndicator,
            value => { this._config.ShowIndicator = value; this._mod.OnConfigChanged(); },
            () => "Show voice indicator",
            () => "Draws microphone state, nearby speakers and the active radius on screen.",
            "show-indicator");

        api.AddBoolOption(
            manifest,
            () => this._config.WallOcclusion,
            value => { this._config.WallOcclusion = value; this._mod.OnConfigChanged(); },
            () => "Wall occlusion",
            () => "Walls and closed doors reduce and muffle incoming voice. This is local: it does not change what other players hear.",
            "wall-occlusion");

        api.AddBoolOption(
            manifest,
            () => this._mod.IsMicrophoneEnabled,
            value => this._mod.SetMicrophoneEnabled(value),
            () => "Microphone enabled",
            () => "Runtime microphone state for this session only. It never changes the persisted Start microphone enabled setting.",
            "microphone-enabled");

        this._monitor.Log("Registered the Generic Mod Config Menu panel.", LogLevel.Trace);
    }

    private void ResetToDefaults()
    {
        this._config.ResetToDefaults();
        this._mod.SetMicrophoneTestActive(false);
        this._mod.SetMicrophoneEnabled(false);
        this._mod.OnConfigChanged();
        this._mod.SaveConfig();
    }

    private string[] BuildDeviceValues(IReadOnlyList<string> devices, string currentValue)
    {
        List<string> values = new() { "" };
        foreach (string device in devices)
        {
            if (!values.Contains(device))
                values.Add(device);
        }

        if (currentValue.Length > 0 && !values.Contains(currentValue))
            values.Add(currentValue);

        return values.ToArray();
    }

    private static string FormatDevice(string value)
    {
        return value.Length == 0 ? "System default" : value;
    }

    private static string FormatActivationMode(string value)
    {
        return value == nameof(ActivationMode.PushToTalk)
            ? "Push to talk"
            : "Voice activity";
    }
}
