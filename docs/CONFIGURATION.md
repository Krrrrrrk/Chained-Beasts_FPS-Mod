# Configuration

The mouse and keyboard settings live in:

```text
BepInEx/config/local.chainedbeasts.firstperson.cfg
```

The file shows up after the first launch with the mod installed. Close the game before editing it, then restart to apply your changes. You don't need to delete the config when updating the DLL.

The section name goes in square brackets in the file. For example, these lines keep F9 as the toggle and use the default mouse sensitivity:

```ini
[Controls]
ToggleKey = F9
MouseSensitivity = 0.12
```

## General

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | `true` | Enable manual FPS switching when using this mod alone. TC can override the camera mode when both are installed. |
| `StartInFirstPerson` | `false` | Enter first person automatically when arena play is ready. Otherwise, press the toggle to turn it on. |

## Controls

| Setting | Default | What it does |
|---|---|---|
| `ToggleKey` | `F9` | Keyboard key for switching first/third person. Use a Unity Input System key name, such as `F8` or `F9`. `None` disables the keyboard toggle. |
| `MouseSensitivity` | `0.12` | Degrees of camera turn per mouse pixel. Range: `0.01` to `2`. |
| `InvertY` | `false` | Invert vertical mouse look. |

Keep a key assigned to `ToggleKey` if you want to switch back manually. `StartInFirstPerson` can turn the camera on automatically when arena play begins.

## Camera

| Setting | Default | What it does |
|---|---|---|
| `FieldOfView` | `85` | Vertical first-person field of view, in degrees. Range: `50` to `110`. |
| `NearClip` | `0.04` | How close something can get to the camera before it's clipped away, in game world units. Range: `0.02` to `0.2`. |
| `EyeHeightOffset` | `0.06` | Height above the animated head anchor. Range: `-0.5` to `0.5` world units. |
| `EyeForwardOffset` | `0.08` | Distance in front of the head, along your horizontal look direction. Range: `0` to `0.3` world units. |
| `HideOwnHead` | `true` | Hide your head, hair, beard, brows, mask, crown, and eye patch in first person. |
| `HideOwnBody` | `true` | Hide your torso, limbs, and body cosmetics. Standard hand, bow, and back equipment attachments are kept visible where the model layout allows it. |
| `ShowCrosshair` | `true` | Show a small crosshair in the middle of the screen when you have control. It disappears during action locks; the camera can still stay in first person. |

Body hiding also hides the head. To see your whole character, set both `HideOwnBody` and `HideOwnHead` to `false`. The model may get in the way again.

The camera follows the head's position but keeps a level horizon instead of copying the head's animated rotation. Vertical look is limited to 80 degrees up or down. When prone, the camera stays at least `0.2` world units above the character's ground origin. That minimum doesn't detect walls or uneven ground.

## Updating from 0.1.0

The original body-hiding default was `false`. A new 0.1.1 config defaults to `true`, but an existing config keeps its old setting. Change this line under `[Camera]` if your own body is blocking the view:

```ini
HideOwnBody = true
```

## If a setting looks wrong

Numeric values outside the listed ranges are clamped when the mod loads. NaN and infinity use the defaults. Check `BepInEx/LogOutput.log` if a config entry can't be read or the mod won't start.

If the camera stops working after a game update, changing these settings won't bypass the game-build check. I don't plan to update it for new game versions.

[Back to the README](../README.md)

## When TC is installed

Choose **F9 SWITCHING** or **POTION ONLY** in TC Content and save before starting or joining. F9 switching keeps Eye Level out of new offers; potion-only disables F9 and makes Eye Level eligible. TC's mode controls switching even if an older FPS config has `Enabled = false`. Your camera sensitivity, FOV and body-visibility preferences still come from this file. Clients temporarily use the host's mode, then recover their saved choice after leaving.
