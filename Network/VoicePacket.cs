using System;
using System.Buffers.Binary;
using System.Text;

namespace ProximityVoiceChat.Network;

/// <summary>
/// A voice packet in the mod's versioned wire format:
/// [version:1][sequence:8][proximity:4][location length:2][location:n][audio:m], big-endian.
/// </summary>
public sealed class VoicePacket
{
    public const byte CurrentVersion = 1;

    /// <summary>Maximum encoded audio bytes accepted in one packet.</summary>
    public const int MaxAudioBytes = 256;

    /// <summary>Maximum UTF-8 bytes accepted for a location name.</summary>
    public const int MaxLocationBytes = 96;

    /// <summary>Size of the fixed header before the location name.</summary>
    public const int HeaderBytes = 1 + 8 + 4 + 2;

    /// <summary>Maximum total packet size the receiver will consider.</summary>
    public const int MaxPacketBytes = HeaderBytes + MaxLocationBytes + MaxAudioBytes;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public VoicePacket(byte version, ulong sequence, int proximityRadius, string locationName, byte[] audio)
    {
        this.Version = version;
        this.Sequence = sequence;
        this.ProximityRadius = proximityRadius;
        this.LocationName = locationName;
        this.Audio = audio;
    }

    /// <summary>Wire-format version of this packet.</summary>
    public byte Version { get; }

    /// <summary>Per-sender frame sequence number.</summary>
    public ulong Sequence { get; }

    /// <summary>Proximity radius declared by the sender, in tiles.</summary>
    public int ProximityRadius { get; }

    /// <summary>Game location the sender was in when the frame was captured.</summary>
    public string LocationName { get; }

    /// <summary>Encoded Opus frame.</summary>
    public byte[] Audio { get; }

    /// <summary>Serialize this packet to its wire representation.</summary>
    public byte[] Serialize()
    {
        byte[] locationBytes = StrictUtf8.GetBytes(this.LocationName);
        byte[] output = new byte[HeaderBytes + locationBytes.Length + this.Audio.Length];

        output[0] = this.Version;
        BinaryPrimitives.WriteUInt64BigEndian(output.AsSpan(1, 8), this.Sequence);
        BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(9, 4), this.ProximityRadius);
        BinaryPrimitives.WriteUInt16BigEndian(output.AsSpan(13, 2), (ushort)locationBytes.Length);
        locationBytes.CopyTo(output, HeaderBytes);
        this.Audio.CopyTo(output, HeaderBytes + locationBytes.Length);
        return output;
    }

    /// <summary>Parse and structurally validate a wire packet.</summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out VoicePacket? packet, out string error)
    {
        packet = null;

        if (data.Length < HeaderBytes)
        {
            error = "packet is shorter than the fixed header";
            return false;
        }

        if (data.Length > MaxPacketBytes)
        {
            error = "packet exceeds the maximum accepted size";
            return false;
        }

        byte version = data[0];
        ulong sequence = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(1, 8));
        int radius = BinaryPrimitives.ReadInt32BigEndian(data.Slice(9, 4));
        int locationLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(13, 2));

        if (locationLength <= 0)
        {
            error = "location name is empty";
            return false;
        }

        if (locationLength > MaxLocationBytes)
        {
            error = "location name exceeds the maximum accepted length";
            return false;
        }

        int remaining = data.Length - HeaderBytes - locationLength;
        if (remaining <= 0)
        {
            error = "audio payload is empty";
            return false;
        }

        if (remaining > MaxAudioBytes)
        {
            error = "audio payload exceeds the maximum accepted size";
            return false;
        }

        string location;
        try
        {
            location = StrictUtf8.GetString(data.Slice(HeaderBytes, locationLength));
        }
        catch (DecoderFallbackException)
        {
            error = "location name is not valid UTF-8";
            return false;
        }

        if (location.Length == 0)
        {
            error = "location name is empty";
            return false;
        }

        byte[] audio = data.Slice(HeaderBytes + locationLength, remaining).ToArray();
        packet = new VoicePacket(version, sequence, radius, location, audio);
        error = "";
        return true;
    }

    /// <summary>Get whether this packet uses a wire-format version the mod supports.</summary>
    public bool HasSupportedVersion()
    {
        return this.Version == CurrentVersion;
    }

    /// <summary>Get whether a location name fits the wire-format cap.</summary>
    public static bool IsValidLocationName(string? name)
    {
        return !string.IsNullOrEmpty(name) && StrictUtf8.GetByteCount(name) <= MaxLocationBytes;
    }
}
