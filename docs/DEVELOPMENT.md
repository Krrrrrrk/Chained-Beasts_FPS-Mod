# Building and working on the mod

This is a shitpost mod for mouse and keyboard, and I don't plan to fix issues or maintain it. Multiplayer is untested, and I have no plans to test it. The source is here if you want to tinker with your own copy.

It's a standalone BepInEx 6 IL2CPP plugin targeting `net6.0`. It borrows the game's camera and animations, then adapts the local player's movement and aiming while first person is active.

## What you'll need

- A Windows x64 copy of Chained Beasts 1.0.2, Steam build 26231777.
- The BepInEx 6 IL2CPP x64 setup linked in the [README](../README.md#installation), with generated assemblies in `BepInEx/interop/`.
- A .NET SDK that can build `net6.0`. The simulated checks also need a .NET 6 runtime.
- Python 3 if you want to run the native-entry audit or package the mod.

The project references the installed game and loader assemblies without copying them into the build output. Game files, loader binaries, saves, build output, and local audit artifacts aren't source files to upload with the mod.

## Build the mod

Run commands from the root of the FPS mod folder. The default game location is:

```text
C:\Program Files (x86)\Steam\steamapps\common\Chained Beasts
```

If yours is somewhere else, set this first in PowerShell:

```powershell
$env:CHAINED_BEASTS_GAME_DIR = 'D:\SteamLibrary\steamapps\common\Chained Beasts'
```

Then build:

```powershell
dotnet build src/ChainedBeasts.FirstPerson/ChainedBeasts.FirstPerson.csproj -c Release
```

The DLL is written to:

```text
src/ChainedBeasts.FirstPerson/bin/Release/net6.0/ChainedBeasts.FirstPerson.dll
```

Building doesn't install anything. With the game closed, copy that DLL into `BepInEx/plugins/` if you want to try your changes.

## Simulated checks

```powershell
dotnet run --project tests/FirstPerson.Tests.csproj -c Release
```

The harness compiles the actual camera code, input hooks, view math, and cleanup code against small managed substitutes for game objects. It covers things like toggling, state restoration, local/remote player routing, camera persistence through action locks, equipment visibility, and cleanup after a partial failure.

It doesn't load Unity, IL2CPP, BepInEx, or the game. Passing it doesn't tell you whether a native detour, animation, or camera collision behaves correctly in-game.

If you're modifying your own copy, useful Solo checks include:

- Repeated F9 toggles, including during a knockdown.
- Forward/back movement, strafing, mouse look, attacks, blocking, dashing, throwing, and pickups.
- Pickups and knockdowns keeping the camera and look angles in first person while native action locks still apply.
- Pause, menus, tabbing out, executions, death/revive, round results, shops, and the next arena restoring or resuming the right camera.
- Body/head visibility, different beasts and cosmetics, held equipment, and nearby walls.
- Injuries that modify movement, especially ones that force a world direction.

`BepInEx/LogOutput.log` is useful if you're debugging your own changes.

## How the camera and controls work

`src/ChainedBeasts.FirstPerson/FirstPersonController.cs` takes over the existing main camera during eligible arena play. It temporarily disables that camera's Cinemachine brain and stores its position, rotation, FOV, near clip, and projection mode. The camera follows the animated head position, with a level horizon and a minimum prone height of `0.2` world units above the character origin.

`Views` and `Controls` are separate checks. A pickup, knockdown, or scripted movement can keep the view active while the movement, aim, and facing hooks leave the game's blocked controls alone. Death, executions, global UI blocks, pause, cinematics, scene changes, and lost focus release the camera.

Body hiding uses `Renderer.forceRenderingOff`, preserving its previous value for each renderer. It doesn't disable the character, animator, or colliders. Descendants of the model's hand, bow, and back attachment transforms are excluded from the body scan, while the model's explicit body/head renderer lists are hidden too. Unusual model layouts may still clip or hide equipment.

`src/ChainedBeasts.FirstPerson/InputHooks.cs` changes only the viewed local actor:

| Game method | Change in first person |
|---|---|
| `Player.GetModifiedPlayerInput(Vector2, bool)` | Rotates the native movement result around camera yaw, retaining its magnitude and vertical component. |
| `Player.GetMouseGroundPos()` | Supplies a ground point 12 units ahead of the player along camera yaw for native mouse aiming and target selection. |
| `NetworkAgent.UpdateModelFacing(Vector3, bool)` | Supplies horizontal view direction and ignores target-facing assistance, then lets the native rotation logic run. |

Looking up and down doesn't add vertical attacks or projectiles. The input hooks don't run while the player is incapacitated, input-blocked, or being moved by the game.

Only one local player can own the shared camera. Two or more players on the same PC retain third person.

`src/ChainedBeasts.FirstPerson/RestoreActions.cs` runs cleanup in reverse order and continues if one destroyed object can't be restored. Turning first person off restores camera state, cursor state, and renderer flags. An exception disables first person for the session and attempts the same cleanup. The mod doesn't add RPCs or save writes.

## Game-build and native-entry checks

`src/ChainedBeasts.FirstPerson/NativeSafety.cs` accepts this `GameAssembly.dll` SHA-256:

```text
ba3ef02fa87da4c41bb27d7610539c9578132204f0fb5713833e2ef93415ce29
```

The three inspected native entries have these RVAs:

| Method | RVA |
|---|---|
| `Player.GetModifiedPlayerInput` | `0x70e530` |
| `Player.GetMouseGroundPos` | `0x70f1d0` |
| `NetworkAgent.UpdateModelFacing` | `0x6d2010` |

Startup checks verify the binary hash, expected entry addresses, and patched aliases. Alias checks run again before camera activation and every five seconds while it's active. An unrecognized binary leaves the mod disabled.

After a game update, inspect the new binary and bindings before changing these values. Don't just replace the hash to get past the check.

### Offline native audit

```powershell
python scripts/audit-native.py
```

The current audit script expects `ilspycmd.exe` at `../tools/ilspy/ilspycmd.exe`, relative to the FPS mod folder. That tool isn't bundled in this repository. Put your local ILSpy command-line tool there, or adjust the script's `ILSPY` path for your workspace.

The script reads the selected game install, checks the pinned hash, decompiles the current `Player` and `NetworkAgent` bindings, and checks that each selected native address is unique in the game assembly's method table. It writes `artifacts/native-audit.json`. It doesn't start the game.

## Packaging

Build the Release DLL and run the native audit first, then:

```powershell
python scripts/package.py
```

The current output is `artifacts/ChainedBeasts-FirstPerson-0.1.1-experimental.zip`. The script includes an explicit list of files: this mod's DLL, README, configuration/developer guides, changelog, MIT license, and package manifest. It then reopens the archive and checks its entries and payload bytes.

It doesn't package game or loader binaries, install the DLL, create a release, or upload anything. Keep `Plugin.Version`, the project version, and `scripts/package.py`'s version together when making a new build.

## License and dependencies

The mod uses the [MIT license](../LICENSE). The existing game camera and animations belong to Chained Beasts. Runtime dependencies are supplied by the installed [BepInEx](https://github.com/BepInEx/BepInEx) setup, including [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop) and [Harmony](https://github.com/pardeike/Harmony). The native audit uses [ILSpy](https://github.com/icsharpcode/ILSpy).

[Back to the README](../README.md)
