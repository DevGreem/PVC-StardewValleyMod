# Proximity Voice Chat (PVC)

Proximity voice chat for **Stardew Valley multiplayer**, built as a SMAPI mod. It captures
the local microphone, sends Opus frames through the game's own multiplayer network, and plays
other farmers' voices back with volume based on how far away they stand. There is no Discord
bot, no token and no external voice service: install the mod and talk.

This is the successor to [LVC](https://github.com/DevGreem/LVC-StardewValleyMod), which delegates
voice to Discord. PVC is a separate mod with its own settings file; do not run both at once.

## Requirements

- Stardew Valley **1.6** (the current 1.6.x builds).
- **SMAPI 4.0 or later**.
- Windows, macOS or Linux, on x64 (Apple Silicon and Linux arm64 are also packaged).
- A microphone and an output device.
- **Every player in the session must install the mod.** A player who does not have it cannot be
  heard and hears nobody.

## Installation

1. Install SMAPI.
2. Build the mod (see below) or unzip the release archive.
3. Copy the resulting `ProximityVoiceChat` folder into `Stardew Valley/Mods`.
4. Start the game through SMAPI and check the SMAPI console for the mod's startup lines.

All portable native audio libraries are shipped in `ProximityVoiceChat/lib`:

| Platform | File |
| --- | --- |
| Windows x64 | `lib/win-x64/portaudio.dll` |
| Linux x64 | `lib/linux-x64/libportaudio.so` |
| Linux arm64 | `lib/linux-arm64/libportaudio.so` |
| macOS Intel | `lib/osx-x64/libportaudio.dylib` |
| macOS Apple Silicon | `lib/osx-arm64/libportaudio.dylib` |

Keep the `lib` folder next to the DLL. If the native library cannot be loaded, the mod reports
the expected path and disables voice chat instead of crashing the game.

## Controls

The defaults are deliberately outside Stardew Valley's usual bindings. All four keys can be
changed in the in-game settings panel, and any key the game can bind is accepted.

| Action | Default key |
| --- | --- |
| Push to talk (hold) | `V` |
| Toggle microphone | `F7` |
| Cycle voice reach | `F8` |
| Start or stop microphone test | `F9` |

`MicrophoneTestKey` defaults to `F9`. If that key is already used by another PVC action (for
example, a custom microphone toggle), the mod moves the test key to the next free function key
when the settings load, so existing binds keep working. The settings panel always shows the
effective key.

The microphone starts according to the persisted `StartMicrophoneEnabled` setting, which is
**disabled by default**. When it is disabled, press `F7` (or use the settings panel) to enable it
after loading a multiplayer save. During a session, `F7` and the panel's microphone control change
only the current state; they never rewrite the saved start preference.

Push-to-talk transmits only while `V` is held. Voice activity transmits only frames louder than
the configured sensitivity threshold. The two modes are selected in the settings panel.

### Microphone test

Press the configured microphone-test key (default `F9`; see the collision note above), or enable
**Microphone test active** in the settings panel, to test the selected microphone without another
player. The test works in single-player and multiplayer, but only while a world is loaded. It
stops automatically when you return to the title screen.

While the test is active, normal voice chat is paused and the HUD shows the input level in dBFS.
Test audio is **never transmitted and never recorded**: the mod opens the input only for this
local test and does not send voice packets.

Enable **Monitor microphone during test** to hear your own microphone through the selected output.
Monitoring is off by default to prevent feedback. The monitor uses `PlaybackVolume`, the same
master volume as incoming voice, so you can lower that setting and confirm the change locally.
Headphones are strongly recommended before enabling monitoring.

## Settings

Settings live in `Mods/ProximityVoiceChat/config.json` and are created with these defaults on
first run. Generic Mod Config Menu is optional; when installed it exposes every option below.
Most changes apply live, while `StartMicrophoneEnabled` applies on the next multiplayer load.

| Setting | Default | Description |
| --- | --- | --- |
| `InputDeviceName` | `""` (system default) | Microphone used for capture. |
| `OutputDeviceName` | `""` (system default) | Device that plays incoming voice. |
| `PlaybackVolume` | `100` | Master volume for incoming voice and the microphone-test monitor, 0–100. |
| `ProximityRadius` | `10` tiles | How far your voice carries: one of `3`, `10`, `20`. It does not filter the voice you hear. |
| `ActivationMode` | `VoiceActivity` | `PushToTalk` or `VoiceActivity`. |
| `VoiceSensitivity` | `50` | Voice-activity threshold, 0 (loud speech only) to 100 (very quiet speech). |
| `StartMicrophoneEnabled` | `false` | When true, the next multiplayer world starts with the microphone enabled. The in-game toggle does not change this setting. |
| `PushToTalkKey` | `V` | Key held to transmit in push-to-talk mode. |
| `MicrophoneToggleKey` | `F7` | Toggles the microphone. |
| `MicrophoneTestKey` | `F9` | Starts and stops the local microphone test. |
| `MicrophoneTestMonitor` | `false` | Plays the microphone test through the selected output at `PlaybackVolume`. |
| `CycleRadiusKey` | `F8` | Cycles through the offered radii. |
| `ShowIndicator` | `true` | Draws the on-screen voice indicator. |

Changing volume, voice reach or activation mode takes effect immediately. Changing an input or
output device restarts only the affected audio stream. Changing `StartMicrophoneEnabled` applies on
the next multiplayer world load and does not change the current session's microphone state. Every
setting can be restored to its default in the settings panel.

The radius is the local player's **outgoing voice reach**: it controls how far other players can
hear you, not how far you hear them. Each speaker's own radius decides what you hear from them.
Choose `3` for a private conversation, `10` for normal conversation or `20` for long range.
Config files from older builds containing `6` or `15` are normalized to `10` on load.

Config files created by older builds do not contain `StartMicrophoneEnabled`, `MicrophoneTestKey`
or `MicrophoneTestMonitor`; they load as `false`, `F9` and `false`, so the microphone starts muted
and the test remains opt-in until the player changes those settings.

If the settings file is missing or cannot be parsed, the mod starts with the defaults, logs the
problem and keeps a copy of the unreadable file as `config.json.broken`.

## On-screen indicator

While a multiplayer world is loaded, the indicator shows:

- whether your microphone is muted, on, or currently transmitting;
- the active voice reach;
- the names of nearby players who are speaking.

It is drawn in the HUD, does not intercept clicks or keystrokes, and is hidden in single-player.
Clear **Show voice indicator** in the settings to hide it.

While the microphone test is active, a separate test overlay always shows the input level and
whether monitoring is on, even in single-player and even if the normal indicator is hidden. It
also shows the key that stops the test.

## How proximity works

Two gates are applied to every incoming packet:

1. **Same location.** The speaker's current game location must match the local player's. No voice
   crosses between the farm, the town, a mine level or a building interior.
2. **Distance.** The speaker declares their voice reach in each packet. Inside that radius, volume
   falls off linearly: full volume next to the player, silence at the radius. The closest speakers
   are mixed first when more than eight are in range at once.

The listener's own configured radius does not gate incoming voice. It is the local player's
outgoing reach, so changing it changes how far others can hear you, not how far you hear them.

The sender's position is never read from the packet. The receiver looks it up in the game's own
farmer state, so editing the mod cannot make a speaker sound closer than they are.

## Version compatibility

The wire format carries a version byte. This build declares **wire-format version 1**. A packet
with a different version is discarded without producing audio. This build also accepts only the
`3`, `10` and `20` reach values: a packet that declares a removed value such as `6` or `15` is
discarded, so all players must run the same version and update together. A wire-format change is a
breaking release and is called out in the release notes.

## Multiplayer and latency

Voice travels over SMAPI mod messages, which are reliable and relayed by the host. This needs no
port forwarding, NAT traversal or relay server, and works in vanilla multiplayer sessions. The
trade-off is latency: it is higher than a dedicated voice protocol. Packets are kept small
(16 kbps Opus, 20 ms frames, at most 256 bytes of audio) so voice stays a rounding error next to
the game's own traffic. The host relays every packet, so very large sessions cost the host more.

Outside the explicit microphone test, voice has **no effect in single-player**: the microphone is
not opened, nothing is transmitted and no indicator is drawn. The microphone test is the only
feature that opens the microphone outside multiplayer, and it never sends audio.

## Privacy

- The microphone starts disabled by default. It opens when joining a multiplayer world only if
  `StartMicrophoneEnabled` is true and the selected input device can be opened.
- The in-game microphone toggle changes the current session only; it never rewrites the saved
  start preference.
- Voice lives in memory only for the lifetime of a packet. Nothing is recorded or written to disk.
- The microphone test opens the input only while you explicitly enable it, and its audio never
  leaves the machine. Test monitoring is off by default.
- Only the encoded frames needed for playback are sent, and only to players in the current
  multiplayer session.
- The indicator always shows whether your microphone is transmitting.

## Building and testing

The mod targets `net6.0`. The tests target a newer runtime so they can run without the game's
.NET 6 runtime; the mod itself still builds against `net6.0`.

```bash
dotnet build ProximityVoiceChat.csproj
dotnet test tests/ProximityVoiceChat.Tests/ProximityVoiceChat.Tests.csproj
```

The build uses `Pathoschild.Stardew.ModBuildConfig`, which needs the game folder. On Linux/macOS,
put the path in `~/stardewvalley.targets`:

```xml
<Project>
  <PropertyGroup>
    <GamePath>/path/to/Stardew Valley</GamePath>
  </PropertyGroup>
</Project>
```

`dotnet build` deploys the mod to the game's `Mods` folder and produces a release zip under
`bin/Debug/net6.0/`.

## Troubleshooting

- **"Audio backend unavailable"** — PortAudio's native library could not be loaded. Check that the
  `lib/<platform>` folder shipped with the mod is present and that the game can read it.
- **"Could not open microphone"** — the selected device is busy or gone. Pick another device in
  the settings panel; the mod also falls back to the system default and logs the substitution.
- **No voices are heard** — confirm every player installed the same major version, everyone is in
  the same game location, and the speakers are inside their own declared voice reach. Players
  without the mod are silently inaudible.
- **Feedback during the microphone test** — turn off **Monitor microphone during test**, lower
  `PlaybackVolume`, or use headphones before enabling monitoring.
- **Generic Mod Config Menu missing** — the mod still works and reads `config.json`; install GMCM
  if you want the in-game panel.
