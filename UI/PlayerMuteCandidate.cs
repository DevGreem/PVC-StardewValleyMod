namespace ProximityVoiceChat.UI;

/// <summary>A current server player as seen by the player-mute menu.</summary>
public readonly record struct PlayerMuteCandidate(
    long PlayerId,
    string Name,
    string Location,
    int TileX,
    int TileY,
    bool IsMuted);

/// <summary>A rendered row in the player-mute menu.</summary>
public sealed record PlayerMuteRow(
    long PlayerId,
    string Name,
    string Location,
    bool IsMuted,
    float Distance);
