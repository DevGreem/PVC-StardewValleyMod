using System;
using ProximityVoiceChat.Config;

namespace ProximityVoiceChat.Audio;

/// <summary>Decides whether a captured frame should be encoded and transmitted.</summary>
public static class ActivationDecision
{
    /// <summary>Get whether a frame should be transmitted under the current mode and state.</summary>
    public static bool ShouldTransmit(
        bool microphoneEnabled,
        bool inMultiplayerWorld,
        ActivationMode mode,
        bool pushToTalkHeld,
        float rms,
        int sensitivity)
    {
        if (!microphoneEnabled || !inMultiplayerWorld)
            return false;

        if (mode == ActivationMode.PushToTalk)
            return pushToTalkHeld;

        return rms >= ThresholdFromSensitivity(sensitivity);
    }

    /// <summary>
    /// Map a 0-100 sensitivity to an RMS threshold. Higher sensitivity means a lower
    /// threshold: 0 requires loud speech, 100 accepts very quiet speech.
    /// </summary>
    public static float ThresholdFromSensitivity(int sensitivity)
    {
        int clamped = Math.Clamp(sensitivity, 0, 100);
        float decibels = -10f - (0.5f * clamped); // -10 dBFS .. -60 dBFS
        return (float)Math.Pow(10d, decibels / 20d);
    }

    /// <summary>Calculate the root mean square level of a frame of float samples.</summary>
    public static float CalculateRms(ReadOnlySpan<float> samples)
    {
        if (samples.Length == 0)
            return 0f;

        double sum = 0d;
        foreach (float sample in samples)
            sum += sample * sample;

        return (float)Math.Sqrt(sum / samples.Length);
    }
}
