namespace ProximityVoiceChat.Audio;

/// <summary>Decides whether the local microphone test may start.</summary>
public static class MicrophoneTestPolicy
{
    /// <summary>Test mode requires a loaded world and an initialized audio backend.</summary>
    public static bool CanStart(bool worldReady, bool audioAvailable)
    {
        return worldReady && audioAvailable;
    }
}
