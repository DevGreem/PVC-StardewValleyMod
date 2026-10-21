namespace ProximityVoiceChat;

/// <summary>Constants shared by the audio, transport and playback pipelines.</summary>
public static class ModConstants
{
    public const string ModId = "Greem.ProximityVoiceChat";
    public const string VoiceMessageType = "voice";
    public const string WallOcclusionMessageType = "wall-occlusion";

    public const int SampleRate = 48000;
    public const int Channels = 1;
    public const int FrameSamples = 960; // 20 ms at 48 kHz
    public const int FrameMilliseconds = 20;
    public const int Bitrate = 16000;

    public const int MaxPacketsPerSecond = 60;
    public const int MaxConcurrentSpeakers = 8;
    public const int JitterTargetFrames = 3;
    public const int MaxBufferedFrames = 20;
    public const int SpeakingHoldMilliseconds = 500;
    public const int StaleChannelMilliseconds = 5000;

    /// <summary>The closed set of voice-reach radii offered to players, in tiles.</summary>
    public static readonly int[] ProximityRadii = { 3, 10, 20 };

    public const int DefaultProximityRadius = 10;
    public const int DefaultSensitivity = 50;
    public const int DefaultPlaybackVolume = 100;

    public const string ConfigFileName = "config.json";
}
