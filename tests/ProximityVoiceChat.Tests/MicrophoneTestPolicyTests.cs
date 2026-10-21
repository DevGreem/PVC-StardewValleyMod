namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using Xunit;

public class MicrophoneTestPolicyTests
{
    [Fact]
    public void AllowsTestInLoadedWorldWithAudio()
    {
        Assert.True(MicrophoneTestPolicy.CanStart(worldReady: true, audioAvailable: true));
    }

    [Fact]
    public void RejectsTestAtTitleOrWithoutWorld()
    {
        Assert.False(MicrophoneTestPolicy.CanStart(worldReady: false, audioAvailable: true));
    }

    [Fact]
    public void RejectsTestWithoutAudio()
    {
        Assert.False(MicrophoneTestPolicy.CanStart(worldReady: true, audioAvailable: false));
    }
}
