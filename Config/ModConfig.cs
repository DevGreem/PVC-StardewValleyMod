using System;
using System.Collections.Generic;

namespace ProximityVoiceChat.Config;

/// <summary>How captured microphone audio becomes transmitted voice.</summary>
public enum ActivationMode
{
    /// <summary>Transmit only while the push-to-talk key is held.</summary>
    PushToTalk,

    /// <summary>Transmit frames whose loudness exceeds the sensitivity threshold.</summary>
    VoiceActivity
}

/// <summary>The persisted settings of the mod.</summary>
public sealed class ModConfig
{
    /// <summary>Name of the microphone to capture; an empty value means the system default.</summary>
    public string InputDeviceName { get; set; } = "";

    /// <summary>Name of the output device used for voice playback; an empty value means the system default.</summary>
    public string OutputDeviceName { get; set; } = "";

    /// <summary>Master playback volume for incoming voice, from 0 to 100.</summary>
    public int PlaybackVolume { get; set; } = ModConstants.DefaultPlaybackVolume;

    /// <summary>The host's preference for static wall occlusion; clients ignore their local value during a session.</summary>
    public bool WallOcclusion { get; set; } = true;

    /// <summary>How far the local player's voice carries, in tiles. Must be one of <see cref="ModConstants.ProximityRadii"/>.</summary>
    public int ProximityRadius { get; set; } = ModConstants.DefaultProximityRadius;

    /// <summary>How the microphone decides when to transmit.</summary>
    public ActivationMode ActivationMode { get; set; } = ActivationMode.VoiceActivity;

    /// <summary>
    /// Voice-activity sensitivity from 0 (only loud speech) to 100 (very quiet speech).
    /// Ignored in push-to-talk mode.
    /// </summary>
    public int VoiceSensitivity { get; set; } = ModConstants.DefaultSensitivity;

    /// <summary>
    /// Whether a multiplayer session starts with the microphone enabled. The in-game
    /// toggle changes only the current session and never rewrites this setting.
    /// </summary>
    public bool StartMicrophoneEnabled { get; set; } = false;

    /// <summary>Key held to transmit while in push-to-talk mode.</summary>
    public string PushToTalkKey { get; set; } = "V";

    /// <summary>Key that toggles the microphone.</summary>
    public string MicrophoneToggleKey { get; set; } = "F7";

    /// <summary>Key that starts and stops the local microphone test.</summary>
    public string MicrophoneTestKey { get; set; } = "F9";

    /// <summary>Whether the local microphone test monitors the input through the output device.</summary>
    public bool MicrophoneTestMonitor { get; set; } = false;

    /// <summary>Key that cycles the voice reach.</summary>
    public string CycleRadiusKey { get; set; } = "F8";

    /// <summary>Key that opens the local player-mute menu.</summary>
    public string PlayerMuteKey { get; set; } = "F10";

    /// <summary>Whether the on-screen voice indicator is drawn.</summary>
    public bool ShowIndicator { get; set; } = true;

    /// <summary>Players muted locally by this config owner.</summary>
    public List<MutedPlayer> MutedPlayers { get; set; } = new();

    /// <summary>Restore every persisted setting to its documented default.</summary>
    public void ResetToDefaults()
    {
        this.InputDeviceName = "";
        this.OutputDeviceName = "";
        this.PlaybackVolume = ModConstants.DefaultPlaybackVolume;
        this.WallOcclusion = true;
        this.ProximityRadius = ModConstants.DefaultProximityRadius;
        this.ActivationMode = ActivationMode.VoiceActivity;
        this.VoiceSensitivity = ModConstants.DefaultSensitivity;
        this.StartMicrophoneEnabled = false;
        this.PushToTalkKey = "V";
        this.MicrophoneToggleKey = "F7";
        this.MicrophoneTestKey = "F9";
        this.MicrophoneTestMonitor = false;
        this.CycleRadiusKey = "F8";
        this.PlayerMuteKey = "F10";
        this.ShowIndicator = true;
        this.MutedPlayers = new();
    }

    /// <summary>Clamp or repair values loaded from disk so every consumer gets a valid config.</summary>
    public void Normalize()
    {
        this.InputDeviceName ??= "";
        this.OutputDeviceName ??= "";
        this.PlaybackVolume = Math.Clamp(this.PlaybackVolume, 0, 100);
        this.VoiceSensitivity = Math.Clamp(this.VoiceSensitivity, 0, 100);

        this.ProximityRadius = NormalizeRadius(this.ProximityRadius);

        if (!Enum.IsDefined(typeof(ActivationMode), this.ActivationMode))
            this.ActivationMode = ActivationMode.VoiceActivity;

        this.PushToTalkKey = NormalizeKey(this.PushToTalkKey, "V");
        this.MicrophoneToggleKey = NormalizeKey(this.MicrophoneToggleKey, "F7");
        this.MicrophoneTestKey = NormalizeKey(this.MicrophoneTestKey, "F9");
        this.CycleRadiusKey = NormalizeKey(this.CycleRadiusKey, "F8");
        this.PlayerMuteKey = NormalizeKey(this.PlayerMuteKey, "F10");
        this.MicrophoneTestKey = ResolveTestKeyConflict(
            this.MicrophoneTestKey,
            this.PushToTalkKey,
            this.MicrophoneToggleKey,
            this.CycleRadiusKey,
            this.PlayerMuteKey);

        this.MutedPlayers ??= new();
        List<MutedPlayer> normalizedPlayers = new();
        HashSet<long> seenPlayers = new();
        foreach (MutedPlayer? player in this.MutedPlayers)
        {
            if (player == null || player.PlayerId <= 0 || !seenPlayers.Add(player.PlayerId))
                continue;

            string name = PlayerNameSanitizer.Sanitize(player.PlayerName, player.PlayerId);
            normalizedPlayers.Add(new MutedPlayer { PlayerId = player.PlayerId, PlayerName = name });
        }

        this.MutedPlayers = normalizedPlayers;
    }

    /// <summary>Get whether a radius belongs to the closed set the mod offers.</summary>
    public static bool IsOfferedRadius(int radius)
    {
        foreach (int value in ModConstants.ProximityRadii)
        {
            if (value == radius)
                return true;
        }

        return false;
    }

    /// <summary>Get whether a radius was offered by an older build and is still accepted on the wire.</summary>
    public static bool IsLegacyRadius(int radius)
    {
        return radius is 6 or 10 or 20;
    }

    /// <summary>Map a loaded radius to the current set, preserving the player's intended tier.</summary>
    private static int NormalizeRadius(int radius)
    {
        return radius switch
        {
            6 => 3,
            10 => 15,
            20 => 30,
            _ => IsOfferedRadius(radius) ? radius : ModConstants.DefaultProximityRadius
        };
    }

    /// <summary>Get the next offered radius after the given one, wrapping around.</summary>
    public static int NextRadius(int current)
    {
        for (int i = 0; i < ModConstants.ProximityRadii.Length; i++)
        {
            if (ModConstants.ProximityRadii[i] == current)
                return ModConstants.ProximityRadii[(i + 1) % ModConstants.ProximityRadii.Length];
        }

        return ModConstants.DefaultProximityRadius;
    }

    private static string NormalizeKey(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value!;
    }

    /// <summary>Keep the test key distinct from the other PVC actions without changing their binds.</summary>
    private static string ResolveTestKeyConflict(string testKey, string pushToTalk, string microphoneToggle, string cycleRadius, string playerMute)
    {
        if (!Conflicts(testKey, pushToTalk)
            && !Conflicts(testKey, microphoneToggle)
            && !Conflicts(testKey, cycleRadius)
            && !Conflicts(testKey, playerMute))
        {
            return testKey;
        }

        string[] candidates = { "F9", "F11", "F12", "F6", "F5", "F4", "F3", "F2", "F1" };
        foreach (string candidate in candidates)
        {
            if (!Conflicts(candidate, pushToTalk)
                && !Conflicts(candidate, microphoneToggle)
                && !Conflicts(candidate, cycleRadius)
                && !Conflicts(candidate, playerMute))
            {
                return candidate;
            }
        }

        return testKey;
    }

    private static bool Conflicts(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
