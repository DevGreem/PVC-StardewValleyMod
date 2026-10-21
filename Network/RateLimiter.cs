using System;

namespace ProximityVoiceChat.Network;

/// <summary>A sliding-window packet budget for one sender.</summary>
public sealed class RateLimiter
{
    private readonly double[] _timestamps;
    private readonly double _windowSeconds;
    private readonly int _maxPackets;
    private int _nextIndex;
    private int _count;

    public RateLimiter(int maxPacketsPerWindow, double windowSeconds = 1d)
    {
        if (maxPacketsPerWindow < 0)
            throw new ArgumentOutOfRangeException(nameof(maxPacketsPerWindow));
        if (windowSeconds <= 0d)
            throw new ArgumentOutOfRangeException(nameof(windowSeconds));

        this._maxPackets = maxPacketsPerWindow;
        this._windowSeconds = windowSeconds;
        this._timestamps = maxPacketsPerWindow > 0 ? new double[maxPacketsPerWindow] : Array.Empty<double>();
    }

    /// <summary>Try to spend one packet from the sender's budget at the given time.</summary>
    public bool TryAcquire(double nowSeconds)
    {
        if (this._maxPackets == 0)
            return false;

        this.DropExpired(nowSeconds);

        if (this._count >= this._maxPackets)
            return false;

        this._timestamps[this._nextIndex] = nowSeconds;
        this._nextIndex = (this._nextIndex + 1) % this._maxPackets;
        this._count++;
        return true;
    }

    private void DropExpired(double nowSeconds)
    {
        double cutoff = nowSeconds - this._windowSeconds;

        while (this._count > 0)
        {
            int oldestIndex = (this._nextIndex - this._count + this._maxPackets) % this._maxPackets;
            if (this._timestamps[oldestIndex] > cutoff)
                break;

            this._count--;
        }
    }
}
