namespace ProximityVoiceChat.Config;

/// <summary>A player muted locally by the config owner.</summary>
public sealed class MutedPlayer
{
    public long PlayerId { get; set; }

    public string PlayerName { get; set; } = "";
}
