# Design

## Context

See `proposal.md` for motivation. The current implementation keeps the microphone state entirely in `ModEntry._microphoneEnabled`:

- `ModConfig` has no microphone-state field.
- `ModEntry.OnSaveLoaded` forces `_microphoneEnabled = false` and stops capture before starting playback.
- `SetMicrophoneEnabled` opens or closes the capture stream and updates the indicator, but never touches the settings file.
- Generic Mod Config Menu already exposes a runtime `Microphone enabled` boolean wired to `IsMicrophoneEnabled`/`SetMicrophoneEnabled`.
- The base change `add-proximity-voice-chat` is still in progress. Its `voice-capture` and `voice-chat-settings` specs were synced into `openspec/specs/` on 2026-10-20, so this delta has main specs to modify; the base change itself is not archived yet.

The design has to preserve the existing privacy and single-player rules while adding one persisted preference that is read only at the start of a multiplayer session.

## Goals / Non-Goals

**Goals:**

- Add a persisted boolean that means “start the microphone enabled when entering a multiplayer world”.
- Apply it exactly once per world load, after the game knows whether the world is multiplayer.
- Keep the in-game toggle as a session-only action that never rewrites the persisted preference.
- Keep single-player completely silent and keep the indicator truthful when the preference is enabled.
- Fail soft if the preference is enabled but the microphone cannot be opened.

**Non-Goals:**

- No change to packet format, Opus settings, transport, proximity gating or playback.
- No change to config-file location, serialization library or GMCM dependency.
- No migration of an existing value: there is no previous microphone-state field to migrate.
- No attempt to keep the current session in sync when the persisted preference is edited; the preference is explicitly a next-session setting.
- No change to the rule that the microphone is never opened on the title screen or in single-player.

## Decisions

### D1 — Persist `StartMicrophoneEnabled`, default `false`

Add `bool StartMicrophoneEnabled { get; set; } = false` to `ModConfig`, include it in `ResetToDefaults`, and leave it untouched by `Normalize` beyond the default.

- **Why a new name instead of `MicrophoneEnabled`:** the existing runtime concept is already called “microphone enabled” in the panel and logs. A separate name makes the contract explicit: one value is a saved preference, the other is the current session state.
- **Why default `false`:** it preserves the current privacy-safe behavior for existing users and for a first run. Enabling voice on join must be an explicit opt-in.
- **Alternative rejected:** persisting the runtime `MicrophoneEnabled` value. That makes an ordinary mute/unmute rewrite the preference and reintroduces the ambiguity the proposal is resolving.

### D2 — Resolve the initial state in `SaveLoaded`

`ModEntry.OnSaveLoaded` remains the single place that initializes a world session. It will:

1. Force the session state off and stop any previous capture, as it does today.
2. Set `_capture.SessionActive` from `IsMultiplayerWorld()`.
3. If the world is multiplayer and `StartMicrophoneEnabled` is true, call `SetMicrophoneEnabled(true)` after the session state is ready.
4. If that call fails, leave the session muted, log the failure and continue loading the world normally.

- **Why `SaveLoaded`:** it is the first point where `Context.IsWorldReady`, `Context.IsMultiplayer` and `Game1.player` are all valid. `GameLaunched` is too early and `OnUpdateTicked` would open the microphone a frame later for no benefit.
- **Why reuse `SetMicrophoneEnabled`:** it already contains the multiplayer-world check, the audio-backend check, the device fallback, the error handling and the indicator update. A second open path would duplicate those rules.
- **Alternative rejected:** opening the stream directly in `OnSaveLoaded`. It would bypass the existing failure handling and make it easy to leave `_microphoneEnabled` and `_capture.MicrophoneEnabled` out of sync.

### D3 — Runtime toggles never write the preference

`SetMicrophoneEnabled` keeps changing only the live state. It will not assign `_config.StartMicrophoneEnabled` and will not call `SaveConfig`.

- **Why:** the player asked for the session toggle to be independent from the configuration. Pressing `F7` to mute during a session must not turn off the next session's start preference, and pressing it to talk must not silently opt the next session in.
- **Consequence:** the persisted value changes only through the GMCM start-state option (or a manual edit of `config.json`), and that change applies on the next multiplayer load.
- **Alternative rejected:** making the runtime toggle update the preference. It would make a temporary mute permanent and would surprise players who only wanted silence for one session.

### D4 — Two separate controls in the settings panel

`GmcmIntegration` will keep the existing runtime `Microphone enabled` boolean and add a second boolean labelled as a start-state option, bound to `_config.StartMicrophoneEnabled`.

- The start-state setter updates the config and calls `OnConfigChanged`/save, but does not call `SetMicrophoneEnabled`.
- The runtime control keeps its current getter/setter and tooltip, but the tooltip will explicitly say that it is session-only.
- Reset-to-defaults sets `StartMicrophoneEnabled = false` and calls `SetMicrophoneEnabled(false)`.

- **Why two controls:** collapsing them into one would make it impossible to answer “what happens next session?” separately from “what is happening now?”.
- **Alternative rejected:** a single boolean that means both. It would either persist every mute or make the start preference impossible to change without also changing the current session.

### D5 — Add a pure decision helper for testability

Introduce a small pure policy helper, for example:

```csharp
public static bool ShouldStartMicrophone(
    bool startMicrophoneEnabled,
    bool worldReady,
    bool multiplayer,
    bool audioAvailable)
```

`OnSaveLoaded` uses it before attempting to open the microphone, and unit tests cover the matrix without needing a running game.

- **Why:** the decision has four independent conditions and is the part most likely to regress (single-player opening the microphone, or a missing backend producing a crash). A pure function makes those cases explicit.
- **Alternative rejected:** relying only on integration tests. They need a multiplayer world and audio hardware and are not practical for every condition.

### D6 — Stacked spec and archive order

The `voice-capture` delta is a `RENAMED` + `MODIFIED` of the base change's “Microphone starts disabled” requirement. The `voice-chat-settings` delta modifies existing requirements and adds one new requirement.

- **Why:** this change is a behavior change to capabilities introduced by `add-proximity-voice-chat`, not a new capability. Duplicating the requirements as `ADDED` would leave two contradictory microphone-state rules in the main specs.
- **Consequence:** the base specs have been synced into `openspec/specs/`, so this delta now has targets. If both changes are archived, archive `add-proximity-voice-chat` first; the base change's `ADDED` requirements are already present and its archive treats them as an early sync, while this change's `MODIFIED`/`RENAMED` operations then apply cleanly.
- **Alternative rejected:** creating a new capability only for this setting. It would split microphone behavior across two unrelated capabilities and make future changes harder to follow.

## Risks / Trade-offs

- **[A player sets the preference to true and forgets, so voice starts on join]** → The default is false, the setting is explicit, the README documents it and the HUD indicator shows `Mic: on`/`Mic: speaking` immediately.
- **[The preference is true but the microphone is missing or busy]** → Keep the session muted, log the failure and leave the game running; the preference stays true so the next session retries.
- **[Players confuse the persisted start option with the runtime toggle]** → Use distinct labels and tooltips in GMCM and document both in the README.
- **[Editing the start option mid-session appears to do nothing]** → This is intentional; the tooltip and README say it applies on the next multiplayer world load.
- **[The in-game toggle could accidentally persist]** → Keep `SetMicrophoneEnabled` free of config writes and cover the separation with a test or code-level assertion in the tasks.
- **[Single-player regression]** → The pure decision helper requires both a ready world and multiplayer; integration verification explicitly checks that single-player never opens the microphone.

## Migration Plan

- Add the boolean with a `false` default. Existing `config.json` files remain valid; the field is read as `false` until the player opts in or the settings file is next written.
- No data migration, no config rename and no rollback procedure beyond restoring the previous DLL/config.
- Deploy the new DLL with the base change already present. Rollback is safe because the extra config field is ignored by older builds.
- Archive the base change first, then this change, so the `MODIFIED`/`RENAMED` delta has a main spec to merge into.

## Open Questions

None.
