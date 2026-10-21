# Design

## Context

See `proposal.md` for motivation and the security audit for evidence. `VoiceTransport` currently broadcasts each voice packet to every peer and creates per-sender rate limiters from the envelope ID before validation. `ModEntry` handles wall-occlusion messages without a budget, reads PVC keybinds while the chat box may be active, and uses raw `Farmer.Name` values. The audit accepted `FromPlayerID` spoofing as a documented SMAPI limitation.

## Goals / Non-Goals

**Goals:**

- Stop honest clients from broadcasting voice to peers that cannot hear them.
- Bound state and allocations created by forged IDs or oversized payloads.
- Prevent PVC keybinds and push-to-talk from interfering with chat input.
- Keep remote names safe for display, logs and persistence.
- Make the documented privacy and attribution limits accurate.

**Non-Goals:**

- Authenticating `FromPlayerID` or making local mute a cryptographic guarantee.
- Host relay or end-to-end encryption; those remain a protocol-level follow-up.
- Changing the voice packet wire format or its version.
- Changing the player-mute menu's filtering/rows or the HUD's layout.

## Decisions

### D1 — Filter recipients at the sender, not the receiver

`VoiceTransport` will build a recipient list from the current farmers using the sender's location, the sender's tile and the radius declared in the outgoing packet. It will pass that list to `SMAPI`'s `SendMessage` overload; no eligible recipients means the packet is dropped.

- **Why:** it removes the accidental full-session broadcast and unnecessary traffic without changing the protocol.
- **Limitation:** a modified client can still address every peer directly, and the host still receives all honest packets it is eligible for. This is explicitly a mitigation, not confidentiality.
- **Alternative rejected:** host relay/E2E is stronger but requires a protocol change and does not solve host trust; defer it.

### D2 — Reject unknown senders before creating limiter state

The transport checks the envelope sender ID against `Game1.otherFarmers` before `ReadAs`/`Convert.FromBase64String` and before `_rateLimiters.GetOrAdd`. The existing per-session cleanup remains.

- **Why:** this closes the unbounded dictionary/OOM vector even though the claimed ID is unauthenticated.
- **Trade-off:** a legitimate packet that arrives before the farmer list contains the peer is dropped; this is a very short window and safer than allocating on an unverified ID.

### D3 — Bound Base64 length before decoding

Add a maximum encoded length derived from `VoicePacket.MaxPacketBytes` and reject longer `Data` strings before `Convert.FromBase64String`.

- **Why:** Base64 decoding allocates roughly three quarters of the string size; the structural limit must apply before that allocation.
- **Limitation:** SMAPI's `ReadAs` already allocated the JSON string; the guard removes the second allocation and the decode/CPU work, not all input cost.
- **Alternative rejected:** only checking after decode leaves the cheap amplification path open.

### D4 — Rate-limit wall-occlusion settings messages

Use a dedicated `RateLimiter` with a small per-second budget before parsing the message. The host still answers legitimate joins and explicit setting changes.

- **Why:** settings messages are rare; a global budget is enough and does not need per-sender state, avoiding another unbounded map.
- **Alternative rejected:** per-sender limiters would recreate the unbounded-state problem for spoofed IDs.

### D5 — Disable PVC input while chat is active

`OnUpdateTicked` forces push-to-talk off when `Game1.chatBox?.isActive()` is true, and `OnButtonPressed` returns before PVC key handling while chat is active. `OnButtonReleased` still clears held state.

- **Why:** the chat box owns text input; swallowing characters or transmitting while typing is a privacy/input bug.
- **Trade-off:** a player cannot toggle mute/microphone while typing; close chat first.

### D6 — Sanitize remote names at every boundary

Add a pure sanitizer that trims, strips control characters, caps at 32 chars and falls back to `Player <id>`. Apply it when building menu candidates, creating/updating speaker channels, rendering the muted-farmer HUD fallback, logging toggles, and normalizing persisted mute entries.

- **Why:** names are remote-controlled and flow into the HUD, logs and config.
- **Alternative rejected:** sanitizing only at draw time would leave log injection and config bloat.

### D7 — Document accepted attribution risk

Keep `FromPlayerID` unauthenticated. README and specs will state that proximity is a playback rule, not a confidentiality boundary, and that voice attribution/mute are advisory.

- **Why:** the user accepted this risk to preserve compatibility and modified-client access; the docs must not promise otherwise.

## Risks / Trade-offs

- **[Modified clients can still broadcast]** → Document the limitation; no claim of confidentiality.
- **[Recipient list can be stale]** → Use the game's current farmer state; unknown/no recipients simply drops the packet.
- **[Unknown-sender guard drops very early voice]** → Accepted short connection window; it prevents unbounded limiter growth.
- **[Name capping loses information]** → 32 chars is enough for the HUD/menu and keeps config/logs bounded; fallback is deterministic.
- **[Chat guard blocks convenience keybinds while typing]** → Intentional; close chat to use PVC controls.

## Migration Plan

No data migration. Deploy the code and documentation together; existing voice packets and config files remain valid. If a fix causes issues, roll back the change; no wire-format or config-format change was introduced.
