namespace ProximityVoiceChat.Tests;

using System.Text;
using ProximityVoiceChat.Network;
using Xunit;

public class VoicePacketTests
{
    [Fact]
    public void MaxEncodedBase64Chars_CoversTheLargestValidPacket()
    {
        Assert.Equal(492, VoicePacket.MaxEncodedBase64Chars);
    }

    [Fact]
    public void RoundTrip_PreservesEveryField()
    {
        byte[] audio = { 1, 2, 3, 4, 5, 250 };
        VoicePacket original = new(VoicePacket.CurrentVersion, 123456789UL, 10, "FarmHouse", audio);

        byte[] wire = original.Serialize();
        bool parsed = VoicePacket.TryParse(wire, out VoicePacket? copy, out string error);

        Assert.True(parsed, error);
        Assert.NotNull(copy);
        Assert.Equal(original.Version, copy!.Version);
        Assert.Equal(original.Sequence, copy.Sequence);
        Assert.Equal(original.ProximityRadius, copy.ProximityRadius);
        Assert.Equal(original.LocationName, copy.LocationName);
        Assert.Equal(original.Audio, copy.Audio);
    }

    [Fact]
    public void Parse_RejectsShortPacket()
    {
        Assert.False(VoicePacket.TryParse(new byte[3], out _, out string error));
        Assert.Contains("shorter", error);
    }

    [Fact]
    public void Parse_RejectsEmptyLocation()
    {
        byte[] audio = { 1 };
        VoicePacket packet = new(VoicePacket.CurrentVersion, 0, 10, "", audio);
        byte[] wire = packet.Serialize();

        Assert.False(VoicePacket.TryParse(wire, out _, out string error));
        Assert.Contains("location", error);
    }

    [Fact]
    public void Parse_RejectsOverlongLocation()
    {
        string location = new('x', VoicePacket.MaxLocationBytes + 1);
        VoicePacket packet = new(VoicePacket.CurrentVersion, 0, 10, location, new byte[] { 1 });
        byte[] wire = packet.Serialize();

        Assert.False(VoicePacket.TryParse(wire, out _, out string error));
        Assert.Contains("location", error);
    }

    [Fact]
    public void Parse_RejectsOversizedAudioPayload()
    {
        byte[] audio = new byte[VoicePacket.MaxAudioBytes + 1];
        VoicePacket packet = new(VoicePacket.CurrentVersion, 0, 10, "Town", audio);
        byte[] wire = packet.Serialize();

        Assert.False(VoicePacket.TryParse(wire, out _, out string error));
        Assert.Contains("maximum", error);
    }

    [Fact]
    public void Parse_RejectsEmptyAudioPayload()
    {
        VoicePacket packet = new(VoicePacket.CurrentVersion, 0, 10, "Town", Array.Empty<byte>());
        byte[] wire = packet.Serialize();

        Assert.False(VoicePacket.TryParse(wire, out _, out string error));
        Assert.Contains("audio", error);
    }

    [Fact]
    public void Parse_RejectsInvalidUtf8Location()
    {
        byte[] location = { 0xC3, 0x28 };
        byte[] wire = new byte[VoicePacket.HeaderBytes + location.Length + 1];
        wire[0] = VoicePacket.CurrentVersion;
        wire[13] = 0;
        wire[14] = (byte)location.Length;
        location.CopyTo(wire, VoicePacket.HeaderBytes);
        wire[^1] = 1;

        Assert.False(VoicePacket.TryParse(wire, out _, out string error));
        Assert.Contains("UTF-8", error);
    }

    [Fact]
    public void Validator_RejectsUnsupportedVersion()
    {
        VoicePacket packet = new((byte)(VoicePacket.CurrentVersion + 1), 0, 10, "Town", new byte[] { 1 });

        Assert.False(PacketValidator.IsValid(packet, out string error));
        Assert.Contains("version", error);
    }

    [Fact]
    public void Validator_RejectsUnsupportedRadius()
    {
        VoicePacket packet = new(VoicePacket.CurrentVersion, 0, 4, "Town", new byte[] { 1 });

        Assert.False(PacketValidator.IsValid(packet, out string error));
        Assert.Contains("radius", error);
    }

    [Fact]
    public void LocationNameFitsWireCap()
    {
        Assert.True(VoicePacket.IsValidLocationName("FarmHouse"));
        Assert.False(VoicePacket.IsValidLocationName(""));
        Assert.False(VoicePacket.IsValidLocationName(new string('x', VoicePacket.MaxLocationBytes + 1)));

        string multibyte = new('é', VoicePacket.MaxLocationBytes / 2);
        Assert.True(VoicePacket.IsValidLocationName(multibyte));
        Assert.False(VoicePacket.IsValidLocationName(new string('é', VoicePacket.MaxLocationBytes)));
    }
}
