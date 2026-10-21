# Tasks

## 1. Overflow rule

- [ ] 1.1 Add the seven-player visible limit and a pure `IndicatorRules` formatter for the hidden count; verify tests cover zero hidden, one hidden (`+1 player nearby`) and several hidden (`+2 players nearby`, `+5 players nearby`)
- [ ] 1.2 Verify the formatter never returns a summary for zero or negative hidden counts and uses the correct singular/plural wording

## 2. HUD integration

- [ ] 2.1 Cap the HUD list at seven visible players, compute `total - 7` hidden players and pass both values to `VoiceIndicator`; verify the displayed names are the seven nearest and the count covers every remaining eligible player
- [ ] 2.2 Render the summary row after the seven player rows without changing the eight-row maximum, mute icons or click-through behavior; verify the panel width/height includes the summary text
- [ ] 2.3 Update the README HUD section with the seven-player cap and `+N players nearby` behavior; verify the documented threshold matches the implementation

## 3. Verification

- [ ] 3.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [ ] 3.2 Verify the HUD visually in headless mode with seven, eight and nine nearby players: no summary at seven, seven + `+1 player nearby` at eight, and seven + `+2 players nearby` at nine
- [ ] 3.3 Verify the player-mute menu's `Nearby` and `All` views still show their existing eight-row behavior and were not changed by this change

## Workflow follow-up

- Archive the change after review and manual verification.
- Promote the change through `features -> tests -> main` using the repository branch policy.
