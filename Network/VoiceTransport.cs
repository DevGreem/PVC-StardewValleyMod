using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ProximityVoiceChat.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ProximityVoiceChat.Network;

/// <summary>The JSON envelope SMAPI serializes; the voice packet itself stays a binary blob.</summary>
public sealed class VoiceMessage
{
    /// <summary>Base64-encoded <see cref="VoicePacket"/> bytes.</summary>
    public string Data { get; set; } = "";
}

/// <summary>Sends and receives voice packets over SMAPI mod messages, with validation and rate limiting.</summary>
public sealed class VoiceTransport : IDisposable
{
    private const int MaxQueuedPackets = 120;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly ConcurrentQueue<VoicePacket> _outgoing = new();
    private readonly ConcurrentDictionary<long, RateLimiter> _rateLimiters = new();
    private int _queuedPackets;
    private bool _loggedFirstReceive;
    private bool _loggedFirstOwnPacketIgnored;
    private bool _loggedNoRecipients;

    public VoiceTransport(IModHelper helper, IMonitor monitor)
    {
        this._helper = helper;
        this._monitor = monitor;
        this._helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
    }

    /// <summary>Raised for each accepted packet from a known, non-local player.</summary>
    public event Action<long, VoicePacket>? PacketReceived;

    /// <summary>Raised when a peer disconnects so playback state can be released immediately.</summary>
    public event Action<long>? PeerLost;

    /// <summary>Queue one packet for the next game tick. Safe to call from the capture callback.</summary>
    public void Enqueue(VoicePacket packet)
    {
        if (Interlocked.Increment(ref this._queuedPackets) > MaxQueuedPackets)
        {
            Interlocked.Decrement(ref this._queuedPackets);
            return;
        }

        this._outgoing.Enqueue(packet);
    }

    /// <summary>Send queued packets on the game thread so SMAPI networking sees one caller at a time.</summary>
    public void DrainOutgoing()
    {
        while (this._outgoing.TryDequeue(out VoicePacket? packet))
        {
            Interlocked.Decrement(ref this._queuedPackets);
            try
            {
                long[] recipients = GetVoiceRecipients(packet);
                if (recipients.Length == 0)
                {
                    if (!this._loggedNoRecipients)
                    {
                        this._loggedNoRecipients = true;
                        this._monitor.Log(
                            $"No voice recipients for location '{packet.LocationName}' at radius {packet.ProximityRadius}; farmers={Game1.otherFarmers?.Count ?? 0}.",
                            LogLevel.Trace);
                    }

                    continue;
                }

                byte[] bytes = packet.Serialize();
                this._helper.Multiplayer.SendMessage(
                    new VoiceMessage { Data = Convert.ToBase64String(bytes) },
                    ModConstants.VoiceMessageType,
                    new[] { ModConstants.ModId },
                    recipients);
            }
            catch (Exception ex)
            {
                this._monitor.Log($"Could not send a voice packet: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    /// <summary>Get the peers in the sender's location and inside the packet's declared reach.</summary>
    private static long[] GetVoiceRecipients(VoicePacket packet)
    {
        Farmer? local = Game1.player;
        if (local?.currentLocation is null || Game1.otherFarmers is null)
            return Array.Empty<long>();

        List<VoiceRecipientCandidate> candidates = new();
        HashSet<long> candidateIds = new();
        foreach (Farmer farmer in Game1.otherFarmers.Values)
        {
            if (farmer.UniqueMultiplayerID == local.UniqueMultiplayerID
                || farmer.currentLocation is null
                || !candidateIds.Add(farmer.UniqueMultiplayerID))
            {
                continue;
            }

            candidates.Add(new VoiceRecipientCandidate(
                farmer.UniqueMultiplayerID,
                farmer.currentLocation.NameOrUniqueName,
                farmer.Tile.X,
                farmer.Tile.Y));
        }

        // A farmhand's Game1.otherFarmers does not always include the host.
        // Include the master player explicitly so client-to-host voice still works.
        Farmer? host = !Context.IsMainPlayer ? Game1.MasterPlayer : null;
        if (host is not null
            && host.UniqueMultiplayerID != local.UniqueMultiplayerID
            && host.currentLocation is not null
            && candidateIds.Add(host.UniqueMultiplayerID))
        {
            candidates.Add(new VoiceRecipientCandidate(
                host.UniqueMultiplayerID,
                host.currentLocation.NameOrUniqueName,
                host.Tile.X,
                host.Tile.Y));
        }

        long[] recipients = VoiceRecipientRules.Select(
            local.currentLocation.NameOrUniqueName,
            local.Tile.X,
            local.Tile.Y,
            packet.ProximityRadius,
            candidates);

        // The host is the session authority and may need to receive client voice
        // even if its replicated tile is briefly stale. The host applies its own
        // location/range gate before playback, so this does not force playback.
        if (!Context.IsMainPlayer
            && host is not null
            && host.UniqueMultiplayerID != local.UniqueMultiplayerID
            && Array.IndexOf(recipients, host.UniqueMultiplayerID) < 0)
        {
            Array.Resize(ref recipients, recipients.Length + 1);
            recipients[^1] = host.UniqueMultiplayerID;
        }

        return recipients;
    }

    /// <summary>Get whether an envelope sender ID belongs to a current farmer or the host.</summary>
    public static bool IsKnownFarmer(long playerId, IEnumerable<long> farmerIds, long hostPlayerId = 0L)
    {
        if (playerId > 0L && playerId == hostPlayerId)
            return true;

        foreach (long farmerId in farmerIds)
        {
            if (farmerId == playerId)
                return true;
        }

        return false;
    }

    /// <summary>Drop a peer's rate-limit state as soon as it disconnects.</summary>
    public void Release(long playerId)
    {
        this._rateLimiters.TryRemove(playerId, out _);
        this.PeerLost?.Invoke(playerId);
    }

    /// <summary>Drop every sender's rate-limit state and any queued packets when the session ends.</summary>
    public void ReleaseAll()
    {
        this._rateLimiters.Clear();
        this.ClearOutgoing();
    }

    /// <summary>Discard frames captured but not yet sent, e.g. when the microphone is muted.</summary>
    public void ClearOutgoing()
    {
        while (this._outgoing.TryDequeue(out _))
        {
            // Drain without touching the counter for every item.
        }

        Interlocked.Exchange(ref this._queuedPackets, 0);
    }

    public void Dispose()
    {
        this._helper.Events.Multiplayer.ModMessageReceived -= this.OnModMessageReceived;
    }

    /// <summary>Get whether a mod message belongs to this mod and came from another player.</summary>
    public static bool IsVoiceMessageForThisMod(string? fromModId, string? type, long fromPlayerId, long localPlayerId)
    {
        return string.Equals(fromModId, ModConstants.ModId, StringComparison.Ordinal)
            && string.Equals(type, ModConstants.VoiceMessageType, StringComparison.Ordinal)
            && fromPlayerId != localPlayerId;
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMultiplayer)
            return;

        long localPlayerId = Game1.player?.UniqueMultiplayerID ?? 0L;
        if (!IsVoiceMessageForThisMod(e.FromModID, e.Type, e.FromPlayerID, localPlayerId))
        {
            if (e.FromPlayerID == localPlayerId
                && string.Equals(e.FromModID, ModConstants.ModId, StringComparison.Ordinal)
                && string.Equals(e.Type, ModConstants.VoiceMessageType, StringComparison.Ordinal)
                && !this._loggedFirstOwnPacketIgnored)
            {
                this._loggedFirstOwnPacketIgnored = true;
                this._monitor.Log("Ignored an echoed voice packet from the local player.", LogLevel.Trace);
            }

            return;
        }

        long hostPlayerId = !Context.IsMainPlayer
            ? Game1.MasterPlayer?.UniqueMultiplayerID ?? 0L
            : 0L;
        if (Game1.otherFarmers is null
            || !IsKnownFarmer(e.FromPlayerID, Game1.otherFarmers.Keys, hostPlayerId))
        {
            return;
        }

        VoiceMessage? message;
        try
        {
            message = e.ReadAs<VoiceMessage>();
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Discarded a voice message that could not be read: {ex.Message}", LogLevel.Trace);
            return;
        }

        if (message?.Data is null || message.Data.Length == 0)
            return;

        // Reject oversized Base64 before Convert.FromBase64String allocates the
        // decoded buffer. SMAPI has already allocated the JSON string in ReadAs.
        if (message.Data.Length > VoicePacket.MaxEncodedBase64Chars)
            return;

        // Apply the sender budget before parsing so a flood of malformed messages
        // costs no more than dropping them.
        RateLimiter limiter = this._rateLimiters.GetOrAdd(
            e.FromPlayerID,
            _ => new RateLimiter(ModConstants.MaxPacketsPerSecond));

        if (!limiter.TryAcquire(GetTimestampSeconds()))
            return;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(message.Data);
        }
        catch (FormatException)
        {
            return;
        }

        if (!VoicePacket.TryParse(bytes, out VoicePacket? packet, out string parseError) || packet is null)
        {
            this._monitor.Log($"Discarded a malformed voice packet from {e.FromPlayerID}: {parseError}.", LogLevel.Trace);
            return;
        }

        if (!PacketValidator.IsValid(packet, out string validationError))
        {
            this._monitor.Log($"Discarded an invalid voice packet from {e.FromPlayerID}: {validationError}.", LogLevel.Trace);
            return;
        }

        if (!this._loggedFirstReceive)
        {
            this._loggedFirstReceive = true;
            this._monitor.Log("Voice transport is receiving packets.", LogLevel.Trace);
        }

        this.PacketReceived?.Invoke(e.FromPlayerID, packet);
    }

    private static double GetTimestampSeconds()
    {
        return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    }
}
