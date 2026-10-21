using System;

namespace ProximityVoiceChat.Audio;

/// <summary>A pullable source of fixed-size mono audio frames for the playback mixer.</summary>
public interface IAudioFrameSource
{
    /// <summary>Gain applied to this source before mixing.</summary>
    float Volume { get; }

    /// <summary>Pull one frame; returns false when the source has no audio ready.</summary>
    bool TryReadFrame(Span<float> destination, double nowSeconds);
}
