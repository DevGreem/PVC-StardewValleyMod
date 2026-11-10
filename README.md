# Proximity Voice Chat (PVC)

## [About This Mod]

This mod adds in-game proximity voice chat to **Stardew Valley multiplayer**.
It captures the local microphone, and plays other farmers' voices back with a
volume that depends on how far away they stand.

There is no Discord bot, no token and no external voice service: install the mod and talk.

This is the successor to [LVC](https://github.com/DevGreem/LVC-StardewValleyMod), which delegates
voice to Discord. PVC is a separate mod with its own settings file

## [How it works]

Voice travels over SMAPI mod messages, which are reliable and relayed by the host. That needs no
port forwarding, NAT traversal or relay server, and works in vanilla multiplayer sessions. The
trade-off is latency: it is higher than a dedicated voice protocol.

Every incoming packet passes three gates:

1. **Same location.** The speaker's current game location must match the local player's. No voice
   is played across the farm, the town, a mine level or a building interior.
2. **Distance.** The speaker declares their voice reach in each packet. Inside that radius, volume
   falls off linearly: full volume next to the player, silence at the radius. The closest speakers
   are mixed first when more than eight are in range at once.
3. **Static walls.** The line between the speaker and the listener is traced over the map's
   `Buildings` layer and the footprints of constructed buildings (cabins, sheds, barns and coops).
   One obstacle makes the voice quieter and muffled; two make it much quieter and heavily muffled;
   three or more make it silent. Open or passable doors do not block, while closed doors do.
   Player-placed objects, trees and furniture are not considered yet.

In mines and farm caves, incoming voice gets a short echo/reverb and a mild attenuation, so the
cave sounds enclosed and immersive instead of just quieter. The cave echo is independent of
`WallOcclusion` and applies even when wall occlusion is disabled by the host. The local microphone
test monitor stays clean; the echo is only applied to remote voice playback.

Wall occlusion is controlled by the **host**. The host's choice is sent to every client and
applies immediately; clients follow it and cannot override it locally. If the host does not run
PVC, no host value arrives and wall occlusion stays enabled for every client. The line of sight
itself is computed locally by each listener from its own map and the farmers' game-state
positions; wall data is never sent.

The sender's position is never read from the packet. The receiver looks it up in the game's own
farmer state, so editing the mod cannot make a speaker sound closer than they are.

Packets are kept small (16 kbps Opus, 20 ms frames, at most 256 bytes of audio) so voice stays a
rounding error next to the game's own traffic. The host relays every packet, so very large
sessions cost the host more.

## [Requirements]

- Stardew Valley **1.6** (the current 1.6.x builds).
- **SMAPI 4.0 or later**.
- Windows, macOS or Linux, on x64 (Apple Silicon and Linux arm64 are also packaged).
- A microphone and an output device.
- **Every player who wants voice chat must install the mod.** Players without it keep playing
  normally; they simply cannot be heard and hear nobody.
- **The host needs SMAPI**, even if the host does not install this mod: SMAPI relays the voice
  messages between players.

## [Installation Guide]

1. Install SMAPI.
2. Build the mod (see [Building and testing](#building-and-testing)) or unzip the release archive.
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

## [How Setup Voice Chat]

1. Install the mod on **every** player's machine, as described above.
2. Start the game through SMAPI and load a multiplayer save.
3. The microphone starts according to `StartMicrophoneEnabled`, which is **disabled by default**.
   Press `F7` (or use the settings panel) to enable it. During a session, `F7` and the panel's
   microphone control change only the current state; they never rewrite the saved start
   preference.
4. Pick `InputDeviceName` and `OutputDeviceName` in the settings panel if the system defaults are
   not the devices you want.
5. Choose how you transmit: **push-to-talk** (hold `V`) or **voice activity** (speak above the
   sensitivity threshold). Both laws are selected in the settings panel.
6. Pick your voice reach with `F8` (`3`, `15` or `30` tiles).

Most changes apply live, while `StartMicrophoneEnabled` applies on the next multiplayer load.

### Microphone test

Press the configured microphone-test key (default `F9`; see the collision note in
[Default Controls](#default-controls)), or enable **Microphone test active** in the settings panel,
to test the selected microphone without another player. The test works in single-player and
multiplayer, but only while a world is loaded. It stops automatically when you return to the title
screen.

While the test is active, normal voice chat is paused and the HUD shows the input level in dBFS.
Test audio is **never transmitted and never recorded**: the mod opens the input only for this local
test and does not send voice packets.

Enable **Monitor microphone during test** to hear your own microphone through the selected output.
Monitoring is off by default to prevent feedback. The monitor uses `PlaybackVolume`, the same
master volume as incoming voice, so you can lower that setting and confirm the change locally.
Headphones are strongly recommended before enabling monitoring.

### Player mute

Press `F10` (or the configured player-mute key) to open the local player menu. It has two views:

- **Nearby:** players in the same game location within the voice reach declared by **that player**,
  so someone with a 30-tile reach appears even if your own reach is only 3.
- **All:** every player currently in the server, regardless of location or distance.

Click a player row to mute or unmute that player. Each row shows the player's own voice reach.
Mutes are **local**: they silence that player only on your machine, while everyone else continues
to hear them. The list is saved in your config and persists across sessions. A muted player who
is nearby still appears in the HUD with a muted-microphone icon, even while they are silent, so
you can see who you have silenced. The HUD stays click-through; the menu is the interactive
surface. Resetting the settings clears the mute list.

## [Config Voice Chat]

Settings live in `Mods/ProximityVoiceChat/config.json` and are created with these defaults on
first run. Generic Mod Config Menu is optional; when installed it exposes every option below.

### General

| Setting | Default | Description |
| --- | --- | --- |
| `InputDeviceName` | `""` (system default) | Microphone used for capture. |
| `OutputDeviceName` | `""` (system default) | Device that plays incoming voice. |
| `PlaybackVolume` | `100` | Master volume for incoming voice and the microphone-test monitor, 0–100. |
| `ActivationMode` | `VoiceActivity` | `PushToTalk` or `VoiceActivity`. |
| `VoiceSensitivity` | `50` | Voice-activity threshold, 0 (loud speech only) to 100 (very quiet speech). |
| `StartMicrophoneEnabled` | `false` | When true, the next multiplayer world starts with the microphone enabled. The in-game toggle does not change this setting. |
| `MutedPlayers` | `[]` | Players muted locally; managed from the player menu and saved in the config. |
| `ShowIndicator` | `true` | Draws the on-screen voice indicator. |

### Host

| Setting | Default | Description |
| --- | --- | --- |
| `WallOcclusion` | `true` | Host-controlled: walls and closed doors reduce and muffle incoming voice. Clients follow the host and cannot change it. |

### Keys

| Setting | Default | Description |
| --- | --- | --- |
| `PushToTalkKey` | `V` | Key held to transmit in push-to-talk mode. |
| `MicrophoneToggleKey` | `F7` | Toggles the microphone. |
| `MicrophoneTestKey` | `F9` | Starts and stops the local microphone test. |
| `MicrophoneTestMonitor` | `false` | Plays the microphone test through the selected output at `PlaybackVolume`. |
| `CycleRadiusKey` | `F8` | Cycles through the offered radii. |
| `PlayerMuteKey` | `F10` | Opens the local player-mute menu. |

### Live changes and migration

Changing volume, voice reach, wall occlusion, the player-mute key or activation mode takes effect
immediately. Muting or unmuting a player applies on the next frame. Changing an input or output
device restarts only the affected audio stream. Changing `StartMicrophoneEnabled` applies on the
next multiplayer world load and does not change the current session's microphone state. Every
setting can be restored to its default in the settings panel; resetting also clears the local mute
list.

The radius is the local player's **outgoing voice reach**: it controls how far other players can
hear you, not how far you hear them. Each speaker's own radius decides what you hear from them.
Choose `3` for a quiet conversation, `15` for normal conversation or `30` for long range.
Config files from older builds are migrated as `6 -> 3`, `10 -> 15` and `20 -> 30`; `3` and `15`
remain unchanged.

Config files created by older builds do not contain `StartMicrophoneEnabled`, `MicrophoneTestKey`,
`MicrophoneTestMonitor`, `WallOcclusion`, `PlayerMuteKey` or `MutedPlayers`; they load as `false`,
`F9`, `false`, `true`, `F10` and an empty list, so the microphone starts muted, the test remains
opt-in, wall occlusion is enabled until the host changes it, and the player mute menu works with no
saved mutes. The host's wall-occlusion value is the only one used during a session; a client's
local value is ignored.

If the settings file is missing or cannot be parsed, the mod starts with the defaults, logs the
problem and keeps a copy of the unreadable file as `config.json.broken`.

## [Possible doubts]

- **Do all players need the mod?**

  No. Only the players who want voice chat need it; everyone else keeps playing normally. A player
  without PVC cannot be heard and hears nobody, and nothing in the SMAPI console warns the others
  about it. The host does need SMAPI, because SMAPI relays the voice messages, but the host does
  not need this mod.

- **Can I run LVC and PVC at the same time?**

  Maybe (I don't tested it)

- **Is there a location limit?**

  No. Voice is only played between players in the same game location, and at most eight speakers
  are mixed at once (the closest ones). There is no hard cap on the number of locations.

- **Does it work in single-player?**

  Voice has no effect in single-player: the microphone is not opened, nothing is transmitted and
  no indicator is drawn. The microphone test is the only feature that opens the microphone outside
  multiplayer, and it never sends audio.

- **Does the mod record anything?**

  No. Voice lives in memory only for the lifetime of a packet. Nothing is recorded or written to
  disk, and the microphone test audio never leaves the machine.

- **Can someone cheat the proximity rules?**

  Proximity is a playback rule, not a confidentiality boundary. Honest clients address outgoing
  voice only to peers in the same location and inside the declared reach, but a modified client
  can still send voice to every peer, and the host can inspect session traffic. SMAPI does not
  authenticate the sender ID, so voice attribution and local mute are advisory rather than
  cryptographic guarantees.

## [Default Controls]

The defaults are deliberately outside Stardew Valley's usual bindings. All keys can be changed in
the in-game settings panel, and any key the game can bind is accepted.

| Action | Default key |
| --- | --- |
| Push to talk (hold) | `V` |
| Toggle microphone | `F7` |
| Cycle voice reach | `F8` |
| Start or stop microphone test | `F9` |
| Open player mute menu | `F10` |

`MicrophoneTestKey` defaults to `F9`. If that key is already used by another PVC action (for
example, a custom microphone toggle), the mod moves the test key to the next free function key
when the settings load, so existing binds keep working. The settings panel always shows the
effective key.

Push-to-talk transmits only while `V` is held. Voice activity transmits only frames louder than
the configured sensitivity threshold. The two modes are selected in the settings panel.

## On-screen indicator

While a multiplayer world is loaded, the indicator shows:

- whether your microphone is muted, on, or currently transmitting;
- the active voice reach;
- the names of nearby players who are speaking, plus nearby players you have muted locally (even
  while silent), with a muted-microphone icon next to the muted players;
- up to seven player rows; when more players are nearby, a final row reports the rest as
  `+N players nearby`.

It is drawn in the HUD, does not intercept clicks or keystrokes, and is hidden in single-player.
Clear **Show voice indicator** in the settings to hide it.

While the microphone test is active, a separate test overlay always shows the input level and
whether monitoring is on, even in single-player and even if the normal indicator is hidden. It
also shows the key that stops the test.

## Privacy

- The microphone starts disabled by default. It opens when joining a multiplayer world only if
  `StartMicrophoneEnabled` is true and the selected input device can be opened.
- The in-game microphone toggle changes the current session only; it never rewrites the saved
  start preference.
- Voice lives in memory only for the lifetime of a packet. Nothing is recorded or written to disk.
- The microphone test opens the input only while you explicitly enable it, and its audio never
  leaves the machine. Test monitoring is off by default.
- Only the encoded frames needed for playback are sent, and honest clients address them only to
  players in the same location and inside the declared reach.
- Wall occlusion is computed locally from the map; only the host's on/off preference is transmitted,
  never wall data or voice occlusion values.
- Player mutes are local and stored in your config; they are never transmitted to other players.
- The indicator always shows whether your microphone is transmitting.

## Version compatibility

The wire format carries a version byte. This build declares **wire-format version 1**. A packet
with a different version is discarded without producing audio. This build sends the current
`3`, `15` and `30` reach values and also accepts the legacy `6`, `10` and `20` values from older
speakers. Older clients still reject the new `30` value, so all players should update together to
use the full range. A wire-format change is a breaking release and is called out in the release
notes.

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
- **No voices are heard** — confirm everyone who wants voice installed the same major version,
  everyone is in the same game location, and the speakers are inside their own declared voice
  reach. Players without the mod are silently inaudible.
- **One player is silent although nearby** — open the player mute menu (`F10`) and check whether
  that player is muted locally. Mutes are saved and only affect your playback.
- **Voice sounds muffled or very quiet near a building** — static walls are occluding the speaker.
  Move to the same side of the wall or ask the host to disable `WallOcclusion`.
- **Voice has a short echo in a mine or farm cave** — that is the cave environment effect. It stays
  active even if the host disables `WallOcclusion`; the local microphone test monitor is not echoed.
- **Feedback during the microphone test** — turn off **Monitor microphone during test**, lower
  `PlaybackVolume`, or use headphones before enabling monitoring.
- **Generic Mod Config Menu missing** — the mod still works and reads `config.json`; install GMCM
  if you want the in-game panel.
