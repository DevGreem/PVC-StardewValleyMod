namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Config;
using Xunit;

public class PlayerNameSanitizerTests
{
    [Fact]
    public void Sanitize_TrimsAndRemovesControlCharacters()
    {
        Assert.Equal("Alice", PlayerNameSanitizer.Sanitize("  Alice  ", 10));
        Assert.Equal("AB", PlayerNameSanitizer.Sanitize("A\nB\r\t", 10));
    }

    [Fact]
    public void Sanitize_CapsTheVisibleLength()
    {
        string name = new('x', PlayerNameSanitizer.MaxVisibleLength + 10);

        Assert.Equal(new string('x', PlayerNameSanitizer.MaxVisibleLength), PlayerNameSanitizer.Sanitize(name, 10));
    }

    [Fact]
    public void Sanitize_UsesAPlayerFallbackWhenNothingRemains()
    {
        Assert.Equal("Player 42", PlayerNameSanitizer.Sanitize(" \n\r\t ", 42));
        Assert.Equal("Player 42", PlayerNameSanitizer.Sanitize(null, 42));
    }
}
