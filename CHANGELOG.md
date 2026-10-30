# Changelog

Changes between versions of the mod.

## 0.1.1

- Keep first person active during pickups, knockdowns, and other temporary player action locks. Native movement, aim, and facing controls stay blocked until the game releases them.
- Retain your look angles when control returns.
- Hide the torso, limbs, and body cosmetics by default, while excluding the usual hand, bow, and back equipment attachments from the body scan.
- Keep the prone camera at least 0.2 world units above the character's ground origin, with a level horizon.
- Mark the managed-only initialization and error methods so IL2CPP doesn't try to expose their unsupported parameters.

Existing configs keep their values. Set `HideOwnBody = true` if you're updating from 0.1.0 and want the new default. These camera/visibility changes haven't had a full in-game retest.

## 0.1.0

- Add the first-person camera, mouse look, movement relative to the view, and a crosshair.
- Use F9 to switch back to the normal camera. Start in third person by default.
- Hide the local head and head accessories, with optional full-body hiding.
- Restore the normal camera and cursor during menus, death, cinematics, results, and scene changes.
- Add game-build and native-entry checks, plus the simulated camera/input test harness.

[Back to the README](README.md)
