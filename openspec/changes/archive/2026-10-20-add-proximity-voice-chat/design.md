# Design

## Context

See `proposal.md` — Why for motivation. What shapes the approach:

- **This repository is empty** apart from scaffolding (`README.md`, `LICENSE`, `.gitignore`, `openspec/`, `.opencode/`). Everything described here is new code; there is no existing source to fit into.
- **Two reference implementations inform the design, neither is a dependency.**
  - `LVC-StardewValleyMod` (the predecessor, separate repo, not modified here) contributes the SMAPI plumbing patterns: lifecycle events, mod-message relay to the host, GMCM registration, i18n. Its audio never enters the game — Discord carries it — which is exactly what this change removes. It is indexed in XERJ as `ax-ref-lvc-stardewvalleymod-*`.
  - `dailyxcz/StardewValleyMods` → `VoiceChat` (MIT) already solves in-game proximity voice with the same library family chosen below. It is used as an architectural reference; PVC is written from scratch, so no third-party code or attribution obligation enters this repository.
- **Constraints that bound the design:**
  - `net6.0`, targeting Stardew Valley 1.6 / SMAPI 4.x.
  - SMAPI's mod-message API is the only networking primitive available without adding a protocol stack. It is a *reliable, host-relayed* channel, not a real-time media channel.
  - SMAPI loads mods from the game's `Mods` folder and does not run with a runtime identifier, so native libraries are not on the default probing path.
  - Cross-platform: Windows x64, macOS x64/arm64, Linux x64/arm64 — SMAPI supports all of them.

## Goals / Non-Goals

**Goals:**

- Capture, encode, transport and play back voice entirely inside the game process, with playback volume driven by in-world distance.
- Zero external accounts or setup: install the mod and talk. This is the single biggest behavioural difference from the predecessor.
- Keep the audio pipeline independent of the game's update loop, so a stutter, an autosave or a loading screen cannot make voices stutter.
- Keep the pieces that can be reasoned about in isolation — packet framing, proximity math, validation — separate from the audio thread, so they are testable without audio hardware.
- Be resilient by construction: a malformed, spoofed or flooding peer degrades that peer, never the session.

**Non-Goals (design-level, beyond the proposal's scope list):**

- Not building a real-time media protocol. The latency of the game's message channel is accepted for this change.
- No NAT traversal, no sockets, no relay server.
- No occlusion, stereo panning, whisper, per-player mute, noise suppression or sidetone.
- No recording, no logging of audio, no persistence of voice data.
- No changes to `LVC-StardewValleyMod`.

## Decisions

### D1 — Voice travels over SMAPI mod messages, behind an interface

Packets are sent with the helper's multiplayer message API and received through its message event.

- **Why:** zero setup for the player, works in vanilla multiplayer sessions (including the Steam/GOG relay), needs no port forwarding and adds no dependency. The reference implementation demonstrates it is workable.
- **Alternatives rejected:** raw UDP sockets (NAT traversal, firewalls and a relay we do not have — and it is already tracked as future work); Steam networking (not exposed through SMAPI); Discord (explicitly rejected: the point of this mod is that voice does not leave the game).
- **Cost:** latency is bounded by the game's message channel rather than by a dedicated media path, and the host relays every packet.
- **Consequence:** the transport sits behind an interface with capture and playback on either side, so a UDP backend can be added later without touching either.

### D2 — One pre-serialized binary blob per message

The message body is a single `byte[]` holding an already-serialized packet.

- **Why:** mod messages are JSON-serialized. Declaring each field as a JSON property would put serialization cost on a path that runs ~50 times per second per speaker. One base64 blob is markedly cheaper.
- **Alternative rejected:** a strongly-typed message with many JSON fields — simpler to read, but measurably heavier at voice rates.

### D3 — Versioned, self-describing, length-prefixed wire format

`[version][sequence][proximity][location-name length][location name][audio]`, big-endian, with hard caps on both variable-length fields.

- **Why:** the audio payload is variable length and must never be trusted. Length prefixes plus caps let a receiver reject a bad packet *before* allocating anything.
- **Alternative rejected:** JSON with base64 audio — larger on the wire and slower to parse.
- **Consequence:** a version byte makes an incompatible peer fail cleanly (packet discarded, silence) instead of producing garbage audio. All players must run the same major version.

### D4 — Opus via Concentus, 48 kHz mono, 20 ms frames, 16 kbps

- **Why:** Opus is built for voice at low bitrate and is resilient to loss. Concentus is a pure-managed implementation, so it adds no native dependency and behaves identically on every platform. 20 ms is the standard voice frame; 16 kbps puts a packet at roughly 40 bytes, so voice is a rounding error next to the game's own traffic.
- **Alternatives rejected:** raw PCM (~192 KB/s per talker — unusable); Speex (worse quality per bit); native libopus via P/Invoke (another native library to ship and resolve per platform, for no benefit at this scale).
- **FEC is enabled** because the transport can lose packets; the encoder only emits redundancy when it is told loss is expected.

### D5 — Audio I/O via PortAudio (PortAudioSharp2)

- **Why:** one API covers capture and playback on Windows, macOS and Linux, and the package ships native binaries for `win-x64`, `linux-x64`, `linux-aarch64`, `osx-x64` and `osx-arm64`.
- **Alternatives rejected:** NAudio (Windows-only — fails the cross-platform requirement); SDL2 (SMAPI already loads SDL2 for the game; a second copy risks version conflicts); OpenAL (playback only, would need a separate capture path).
- **Consequence — the sharpest edge in this design:** SMAPI runs the mod from the game's `Mods` folder with no runtime identifier, so the native library is not found by default. The mod must install a `DllImport` resolver that maps the requested library name to the *platform-appropriate* file name (`portaudio.dll`, `libportaudio.so`, `libportaudio.dylib`) inside the mod folder. Getting this wrong is the most likely cause of "the mod does nothing" on macOS and Linux, so it must be logged explicitly and fail soft.

### D6 — The output stream pulls mixed audio; the game thread never feeds it

Mixing happens inside the audio output callback, which asks for the next mixed frame.

- **Why:** if the game thread pushed frames, a loading screen, autosave or frame spike would starve playback and produce the classic robotic stutter.
- **Alternative rejected:** game-thread push into a queue — simpler, but ties audio continuity to frame rate.

### D7 — One channel per speaker, each with its own decoder and jitter buffer

- **Why:** every speaker has an independent sequence and loss pattern. A per-speaker buffer lets one peer's jitter be absorbed without affecting anyone else, and lets Opus conceal that speaker's single lost frame rather than falling silent.
- **Consequence:** channels are created lazily on first packet and released on disconnect or after a stale timeout, so a session with churn does not accumulate decoders.

### D8 — Proximity is computed by the receiver, from game state

- The speaker's position is read from the game's own farmer state, **never** from the packet.
- Two gates, in order: same game location, then distance in tiles within the declared radius.
- Volume falls off linearly: full at zero distance, silent at the radius. Linear is predictable and easy to tune; a perceptual curve is a refinement, not a requirement.
- The declared radius is validated against the closed set the mod offers, so a modified client cannot widen its own reach.

**Why:** this is the trust boundary. A player who edits the mod can change what they *send*, but the position that decides whether they are heard is computed on the listener's machine.

### D9 — Rate limits and size caps at the network boundary

A per-sender packet budget (voice legitimately produces ~50 packets/s), a cap on encoded audio bytes, and a cap on the location-name length. Anything over budget is dropped before it reaches decoding.

**Why:** without this, one malicious or broken peer can saturate every listener's audio path. Rejecting at the boundary keeps the cost of a flood proportional to dropping bytes.

### D10 — Threading model

| Stage | Thread | Work |
| --- | --- | --- |
| Capture | audio capture callback | accumulate into frames, apply activation rule, encode, hand off to transport |
| Receive | game thread (message event) | parse, validate, rate-limit, gate by location and distance, decode, enqueue into the speaker's buffer |
| Mix | audio output callback | mix the closest N in-range speakers into the outgoing frame |

Shared state is a concurrent map of speaker channels; each channel guards its own buffer. The mix path performs no allocation and no LINQ, because it must fit inside one frame's time budget.

### D11 — Settings: one config object, panel optional

- Config is read and written through the game's config helper; GMCM is resolved by ID at runtime and skipped when absent.
- **Devices are stored by name, not index**, because device indices shift between sessions and machines.
- **The proximity radius is a value from a fixed set**, so the receiving side has a closed set to validate against (D8).
- Changes to volume, radius, activation mode and devices are applied live; the audio streams are restarted only when a device actually changes.

### D12 — Privacy defaults

- The microphone starts **disabled** every session and the player enables it deliberately.
- Nothing is written to disk except settings; audio lives only in memory for the lifetime of a packet.
- The on-screen indicator always reflects whether the local player is transmitting.

## Risks / Trade-offs

- **[Latency over the game's message channel is higher than a dedicated voice protocol]** → Accepted for this change; the transport is behind an interface and a low-latency path is already tracked as future work. The README should set the expectation rather than let players discover it.
- **[The host relays every voice packet, so load grows with the square of the player count]** → The 16 kbps bitrate, the per-sender rate limit and the concurrent-talker cap keep it modest. Document the practical player ceiling.
- **[Native library loading is the most fragile part on macOS and Linux]** → Resolve the platform-specific file name explicitly, log which file was loaded, and fail soft (mod loads, voice disabled, clear error) instead of crashing the game. Verify on each platform before release.
- **[A stale device name would otherwise break startup]** → Always fall back to the system default device and report the substitution.
- **[A player without the mod is silently inaudible and hears nobody]** → State it in the README; the indicator shows who is actually in range rather than implying everyone is.
- **[Voice traffic shares the game's own channel and could affect game latency]** → Keep packets small and rate-limited, and measure before release.
- **[The audio thread must finish inside one frame budget]** → Bounded mixing, pre-allocated reusable buffers, no allocation or LINQ on the mix path.
- **[Wire-format changes silently break cross-version play]** → Version byte, discard on mismatch, and a release note whenever it is bumped.
- **[Opening the microphone is a privacy-sensitive action]** → Off by default each session, never persisted as enabled, and always visible in the indicator.

## Migration Plan

- **Nothing to migrate.** This is a new mod in a new repository; `LVC-StardewValleyMod` is untouched and stays usable.
- **Coexistence:** running PVC and LVC together would mean hearing a voice twice, once per transport. The README must tell players to use one or the other.
- **Rollback:** delete the mod folder; delete the config file to also drop saved settings.
- **Compatibility:** all players in a session must run the same major version; a wire-format bump is a breaking release and must be called out.

## Open Questions

- The default proximity radius and the exact set of offered values. Deferrable: it is a constant, and changing it touches neither the specs nor the task breakdown.
- Whether the talking indicator is drawn through the game's HUD or as a custom overlay. Deferrable: both satisfy the specs; decide while implementing.
- The precise player-count ceiling at which host relaying becomes noticeable. Deferrable: it needs measurement with real sessions, and the answer tunes documentation, not the design.
