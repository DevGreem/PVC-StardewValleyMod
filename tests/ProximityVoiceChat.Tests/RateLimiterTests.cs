namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Network;
using Xunit;

public class RateLimiterTests
{
    [Fact]
    public void AcceptsNormalSpeechRate()
    {
        RateLimiter limiter = new(maxPacketsPerWindow: 60);

        for (int i = 0; i < 50; i++)
            Assert.True(limiter.TryAcquire(i * 0.02d));
    }

    [Fact]
    public void DropsFloodFromOneSender()
    {
        RateLimiter limiter = new(maxPacketsPerWindow: 60);
        int accepted = 0;

        for (int i = 0; i < 60; i++)
        {
            if (limiter.TryAcquire(i * 0.01d))
                accepted++;
        }

        Assert.Equal(60, accepted);

        for (int i = 60; i < 100; i++)
            Assert.False(limiter.TryAcquire(i * 0.01d));
    }

    [Fact]
    public void BudgetRefillsAfterWindow()
    {
        RateLimiter limiter = new(maxPacketsPerWindow: 60);

        for (int i = 0; i < 60; i++)
            Assert.True(limiter.TryAcquire(0d));

        Assert.False(limiter.TryAcquire(0.5d));
        Assert.True(limiter.TryAcquire(1.01d));
    }
}
