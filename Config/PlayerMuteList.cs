using System.Collections.Generic;

namespace ProximityVoiceChat.Config;

/// <summary>Fast runtime view of the persisted per-player mute list.</summary>
public sealed class PlayerMuteList
{
    private readonly HashSet<long> _muted = new();

    /// <summary>Get whether a player is muted locally.</summary>
    public bool IsMuted(long playerId)
    {
        return this._muted.Contains(playerId);
    }

    /// <summary>Rebuild the runtime set from the persisted config.</summary>
    public void Rebuild(ModConfig config)
    {
        this._muted.Clear();
        foreach (MutedPlayer player in config.MutedPlayers)
        {
            if (player.PlayerId > 0)
                this._muted.Add(player.PlayerId);
        }
    }

    /// <summary>Toggle a player's mute state and keep the config list in sync.</summary>
    public bool Toggle(ModConfig config, long playerId, string playerName)
    {
        if (this._muted.Remove(playerId))
        {
            config.MutedPlayers.RemoveAll(player => player.PlayerId == playerId);
            return false;
        }

        this._muted.Add(playerId);
        foreach (MutedPlayer player in config.MutedPlayers)
        {
            if (player.PlayerId == playerId)
            {
                player.PlayerName = playerName;
                return true;
            }
        }

        config.MutedPlayers.Add(new MutedPlayer { PlayerId = playerId, PlayerName = playerName });
        return true;
    }
}
