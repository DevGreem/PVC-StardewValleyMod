# Tasks

## 1. Voice recipient filtering

- [x] 1.1 Add a pure recipient-selection rule for same-location/radius filtering and use it in `VoiceTransport` before `SendMessage`; verify tests include in-range, out-of-range, different-location and no-recipient cases
- [x] 1.2 Update the README to state that sender-side filtering removes the honest-client broadcast but is not confidentiality against modified clients or the host

## 2. Incoming message bounds

- [x] 2.1 Reject voice messages whose `FromPlayerID` is not a current farmer before parsing or creating a rate limiter; verify a spoofed ID creates no limiter state
- [x] 2.2 Add a maximum Base64 length derived from the packet limit and reject oversized `Data` before `Convert.FromBase64String`; verify valid boundary sizes pass and larger values are dropped

## 3. Wall-occlusion and input hardening

- [x] 3.1 Add a global rate limit for wall-occlusion settings messages before parsing; verify a request flood is bounded and a legitimate join/change still works
- [x] 3.2 Ignore PVC keybinds and force push-to-talk off while the chat box is active; verify typing `V` in chat does not transmit and F7–F10 do not fire

## 4. Name sanitization

- [x] 4.1 Add a pure player-name sanitizer (trim, strip control characters, cap length, `Player <id>` fallback) and apply it to menu candidates, speaker channels, HUD fallback and mute logs; verify tests cover newlines, overlong names and empty names
- [x] 4.2 Normalize persisted mute names with the same sanitizer and verify a legacy config with a control-character or overlong name loads cleanly

## 5. Documentation and verification

- [x] 5.1 Correct README/spec claims about proximity privacy, sender authentication and allocation limits; verify the documented behavior matches the code
- [x] 5.2 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [ ] 5.3 Verify the security behavior in a focused test or headless run: no broadcast for an out-of-range peer, no limiter growth for an unknown sender, no PTT transmission from chat, and sanitized names in the HUD/menu

## Workflow follow-up

- Archive the change after review and verification.
- Promote through `features -> tests -> main` using the repository branch policy.
- Keep SEC-002 (`FromPlayerID` authentication) as an accepted, documented risk; revisit only if the threat model changes.
