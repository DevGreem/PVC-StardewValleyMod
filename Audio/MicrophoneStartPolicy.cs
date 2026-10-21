namespace ProximityVoiceChat.Audio;

/// <summary>Decides whether a world load should start the microphone enabled.</summary>
public static class MicrophoneStartPolicy
{
    /// <summary>
    /// Get whether the persisted start preference should open the microphone now.
    /// A title screen, a single-player world or an unavailable audio backend always wins.
    /// </summary>
    public static bool ShouldStartMicrophone(
        bool startMicrophoneEnabled,
        bool worldReady,
        bool multiplayer,
        bool audioAvailable)
    {
        return startMicrophoneEnabled && worldReady && multiplayer && audioAvailable;
    }
}
