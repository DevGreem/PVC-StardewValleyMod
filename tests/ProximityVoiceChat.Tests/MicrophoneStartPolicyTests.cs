namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using Xunit;

public class MicrophoneStartPolicyTests
{
    [Fact]
    public void StartsOnlyWhenPreferenceWorldMultiplayerAndAudioAllowIt()
    {
        Assert.True(MicrophoneStartPolicy.ShouldStartMicrophone(true, true, true, true));
    }

    [Fact]
    public void DisabledPreferenceNeverStarts()
    {
        Assert.False(MicrophoneStartPolicy.ShouldStartMicrophone(false, true, true, true));
    }

    [Fact]
    public void TitleScreenNeverStarts()
    {
        Assert.False(MicrophoneStartPolicy.ShouldStartMicrophone(true, false, true, true));
        Assert.False(MicrophoneStartPolicy.ShouldStartMicrophone(true, false, false, true));
    }

    [Fact]
    public void SinglePlayerNeverStarts()
    {
        Assert.False(MicrophoneStartPolicy.ShouldStartMicrophone(true, true, false, true));
    }

    [Fact]
    public void UnavailableAudioNeverStarts()
    {
        Assert.False(MicrophoneStartPolicy.ShouldStartMicrophone(true, true, true, false));
    }
}
