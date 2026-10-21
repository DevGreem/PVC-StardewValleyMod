# Tasks

## 1. Radius set and configuration migration

- [x] 1.1 Change `ModConstants.ProximityRadii` to `{3, 15, 30}` and `DefaultProximityRadius` to `15`; verify the radius-set test, cycle order `3 -> 15 -> 30 -> 3`, and default/reset values
- [x] 1.2 Update `ModConfig.Normalize` to map `6 -> 3`, `10 -> 15`, `20 -> 30`, preserve `3/15`, and fall back to `15` for arbitrary values; verify unit tests cover every mapping and the normalization log
- [x] 1.3 Update the README settings table, reach explanation, legacy-config note and examples to `3/15/30` with default `15`; verify the documented values match the code

## 2. Packet compatibility

- [x] 2.1 Extend packet validation to accept the current `3/15/30` set plus the legacy `6/10/20` values while the sender continues to advertise only current values; verify tests accept `3/15/30/6/10/20` and reject an unsupported value such as `25`
- [x] 2.2 Verify the wire format and outgoing packet path remain unchanged, so updated clients can still receive legacy speakers; document the mixed-version limitation for the new `30` value

## 3. HUD and menu fallback

- [x] 3.1 Verify the HUD and player-mute menu derive their unknown-reach fallback from the maximum offered value and now use `30`; add or update tests that assert the fallback and the menu's per-player reach display
- [ ] 3.2 Verify the new range values appear in GMCM and the HUD/mute menu without hardcoded `10/20` text; run a headless visual check with a config containing `15` and `30`

## 4. Verification

- [x] 4.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [ ] 4.2 In a two-player world, set the speaker to `30` and verify a listener around 25 tiles hears them, then set `3` and verify the listener stops hearing at 3 tiles; verify the listener's own reach does not affect incoming voice
- [x] 4.3 Verify a config containing legacy `6/10/20` loads as `3/15/30` without errors, and a config containing `30` is preserved on reload

## Workflow follow-up

- Archive the change after review and manual verification.
- Promote the change through `features -> tests -> main` using the repository branch policy; coordinate the release note that all players should update together to use 30 tiles.
