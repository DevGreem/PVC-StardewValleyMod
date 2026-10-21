namespace ProximityVoiceChat.Network;

/// <summary>The host's wall-occlusion preference sent to every client.</summary>
public sealed class WallOcclusionMessage
{
    public bool Enabled { get; set; }
}
