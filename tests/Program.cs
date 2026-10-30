using BepInEx.Logging;
using ChainedBeasts.FirstPerson;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

static class Program
{
    private static int _checks, _scenarios;
    static void Check(bool value, string name)
    { _checks++; if (!value) throw new Exception("FAILED: " + name); }
    static bool Near(float a, float b) => Math.Abs(a - b) < .0001f;
    static void Scenario(string name, Action test)
    { test(); _scenarios++; Console.WriteLine("PASS " + name); }
    static (FirstPersonController Controller, Player Player, Camera Camera, CinemachineBrain Brain, ManualLogSource Log) Scene(Settings settings = null)
    {
        FirstPersonController.Instance?.Shutdown();
        NativeSafety.ThrowOnAudit = false;
        Application.isFocused = true; Time.timeScale = 1; Time.unscaledTime = 1;
        UIManager.GameInputBlocked = false;
        Cursor.lockState = CursorLockMode.Confined; Cursor.visible = true;
        Keyboard.current = new(); Mouse.current = new();
        Gamepad.all.Clear(); GameInput.Assignments.Clear();
        var player = new Player();
        player.transform.position = new(10, 0, 20);
        NetworkSession.Instance = new(); NetworkSession.Instance.LocalClients.Add(player.clientID.Value);
        PlayerManager.Instance = new(); PlayerManager.Instance.localPlayers.Add(player);
        var brain = new CinemachineBrain();
        Camera.main = new(); Camera.main.gameObject.Brain = brain;
        Camera.main.transform.position = new(5, 15, -5);
        Camera.main.transform.rotation = new(1, 2, 3, 4);
        var log = new ManualLogSource();
        var controller = new FirstPersonController((IntPtr)99999);
        controller.Initialize(settings ?? new Settings(), log);
        return (controller, player, Camera.main, brain, log);
    }
    static void Toggle(FirstPersonController controller)
    { Keyboard.current.Button.wasPressedThisFrame = true; controller.Update(); Keyboard.current.Button.wasPressedThisFrame = false; }
    static void Restored((FirstPersonController Controller, Player Player, Camera Camera, CinemachineBrain Brain, ManualLogSource Log) s)
    {
        Check(s.Brain.enabled, "brain restored");
        Check(Near(s.Camera.fieldOfView, 53) && Near(s.Camera.nearClipPlane, .3f) && s.Camera.orthographic, "lens restored");
        Check(Near(s.Camera.transform.position.x, 5) && Near(s.Camera.transform.position.y, 15), "position restored");
        Check(Near(s.Camera.transform.rotation.w, 4), "rotation restored");
        Check(Cursor.lockState == CursorLockMode.Confined && Cursor.visible, "cursor restored");
        Check(!s.Player.agentModel.headMaleRenderer.forceRenderingOff, "head restored");
        Check(!s.Controller.Controls(s.Player), "input released");
    }
    static void Main()
    {
        Scenario("toggle restores camera, lens, cursor and original renderer flags", () =>
        {
            var s = Scene();
            s.Player.agentModel.headFemaleRenderer.forceRenderingOff = true;
            s.Controller.Update(); Check(s.Brain.enabled, "defaults to third person");
            Toggle(s.Controller); s.Controller.LateUpdate();
            Check(!s.Brain.enabled && s.Controller.Controls(s.Player), "first person active");
            Check(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked, "cursor captured");
            Check(s.Player.agentModel.headMaleRenderer.forceRenderingOff, "head hidden");
            Check(Near(s.Camera.transform.position.y, 1.76f) && Near(s.Camera.transform.position.z, 20.08f), "eye anchor");
            Toggle(s.Controller); Restored(s);
            Check(s.Player.agentModel.headFemaleRenderer.forceRenderingOff, "already hidden renderer stays hidden");
            s.Controller.Shutdown(); Restored(s);
        });
        Scenario("view-relative movement, ground aim and facing affect only the local actor", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true });
            s.Player.ModelForward = new(1, 0, 0); s.Controller.Update();
            var move = new Vector3(0, .25f, .4f);
            InputHooks.Movement(s.Player, ref move);
            Check(Near(move.x, .4f) && Near(move.y, .25f) && Near(move.z, 0), "native movement magnitude preserved");
            var point = new Vector3();
            Check(!InputHooks.MousePoint(s.Player, ref point) && Near(point.x, 22) && Near(point.z, 20) && Near(point.y, 0), "ground aim ahead");
            var facing = new Vector3(0, 0, -1); var ignore = false;
            InputHooks.Facing(s.Player, ref facing, ref ignore);
            Check(Near(facing.x, 1) && ignore, "strafe facing follows view");
            var remote = new Player(); var original = new Vector3(1, 2, 3);
            InputHooks.Movement(remote, ref original); Check(Near(original.x, 1) && Near(original.z, 3), "remote movement untouched");
            Check(InputHooks.MousePoint(remote, ref original), "remote aim uses native code");
            ignore = false; InputHooks.Facing(remote, ref original, ref ignore); Check(!ignore && Near(original.y, 2), "remote facing untouched");
            Toggle(s.Controller); InputHooks.Movement(s.Player, ref original); Check(Near(original.z, 3), "third-person movement untouched");
        });
        var suspensions = new (string Name, Action<Player> Change)[]
        {
            ("pause", p => NetworkSession.Instance.IsPaused = true),
            ("menu input block", p => UIManager.GameInputBlocked = true),
            ("death", p => p.IsDead = true),
            ("execution", p => p.IsExecuting = true),
            ("boss cinematic", p => NetworkSession.Instance.Level.BossCinematicPlaying = true),
            ("round end", p => NetworkSession.Instance.Level.ActiveGameState = GameLevel.eGameState.LEVEL_COMPLETE),
            ("scene transition", p => NetworkSession.Instance.TransitionPending = true),
            ("focus loss", p => Application.isFocused = false),
            ("two local players", p => PlayerManager.Instance.localPlayers.Add(new Player())),
            ("despawn", p => p.IsSpawned = false),
            ("disconnected", p => NetworkSession.Instance = null),
            ("level destroyed", p => NetworkSession.Instance.Level = null),
            ("head destroyed", p => p.agentModel.headTransform.Destroyed = true),
        };
        foreach (var suspension in suspensions)
            Scenario("restore on " + suspension.Name + " including a late-frame state change", () =>
            {
                var s = Scene(new Settings { StartFirstPerson = true });
                s.Controller.Update(); s.Controller.LateUpdate();
                suspension.Change(s.Player);
                Check(!s.Controller.Controls(s.Player), "hooks stop immediately");
                s.Controller.LateUpdate(); Restored(s);
            });
        var retainedViews = new (string Name, Action<Player, bool> Change)[]
        {
            ("pickup input lock", (p, value) => p.IsInputBlocked = value),
            ("knockdown", (p, value) => p.IsIncapacitated = value),
            ("scripted movement", (p, value) => p.AutoMoveActive = value),
        };
        foreach (var retained in retainedViews)
            Scenario("retain first person through " + retained.Name + " while preserving native locks", () =>
            {
                var s = Scene(new Settings { StartFirstPerson = true });
                var body = new Renderer(); s.Player.agentModel.mainRenderers.Add(body);
                Mouse.current.delta.Value = new(750, 100); s.Controller.Update(); s.Controller.LateUpdate();
                Mouse.current.delta.Value = Vector2.zero;
                var originalYaw = s.Controller.Yaw;
                retained.Change(s.Player, true);
                Check(s.Controller.Views(s.Player) && !s.Controller.Controls(s.Player), "view retained and controls gated immediately");
                s.Player.agentModel.headTransform.position = new(10, .05f, 20);
                s.Controller.Update(); s.Controller.LateUpdate();
                Check(!s.Brain.enabled && s.Controller.Views(s.Player), "camera stays in first person");
                Check(s.Player.agentModel.headMaleRenderer.forceRenderingOff && body.forceRenderingOff, "head and body stay hidden");
                Check(Near(s.Controller.Yaw, originalYaw) && Near(s.Camera.transform.rotation.x, -12), "look angles retained");
                Check(Near(s.Camera.transform.position.y, .2f), "prone eye stays above ground origin");
                var move = new Vector3(0, .25f, .4f); InputHooks.Movement(s.Player, ref move);
                Check(Near(move.x, 0) && Near(move.z, .4f), "native movement result untouched while locked");
                var facing = new Vector3(0, 0, -1); var ignore = false;
                InputHooks.Facing(s.Player, ref facing, ref ignore);
                Check(!ignore && Near(facing.z, -1), "native knockdown/pickup facing untouched");
                Check(InputHooks.MousePoint(s.Player, ref move), "native aim untouched while locked");
                retained.Change(s.Player, false); s.Controller.Update(); s.Controller.LateUpdate();
                Check(s.Controller.Controls(s.Player) && Near(s.Controller.Yaw, originalYaw), "controls resume without restarting view");
                Toggle(s.Controller); Restored(s);
                Check(!body.forceRenderingOff, "body returns on third-person toggle");
            });
        Scenario("F9 exits first person during a knockdown", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true }); s.Controller.Update(); s.Controller.LateUpdate();
            s.Player.IsIncapacitated = true; s.Controller.Update(); s.Controller.LateUpdate();
            Check(s.Controller.Views(s.Player), "still first person while knocked down");
            Toggle(s.Controller); Restored(s);
        });
        Scenario("resume after pause and toggle off while paused", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true }); s.Controller.Update();
            NetworkSession.Instance.IsPaused = true; s.Controller.Update(); Restored(s);
            NetworkSession.Instance.IsPaused = false; s.Controller.Update(); Check(s.Controller.Controls(s.Player), "resumes requested view");
            NetworkSession.Instance.IsPaused = true; s.Controller.Update(); Toggle(s.Controller);
            NetworkSession.Instance.IsPaused = false; s.Controller.Update(); Restored(s);
        });
        Scenario("reject remote actor incorrectly listed as local and couch co-op", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true });
            NetworkSession.Instance.LocalClients.Clear(); s.Controller.Update(); Restored(s);
            NetworkSession.Instance.LocalClients.Add(s.Player.clientID.Value);
            PlayerManager.Instance.localPlayers.Add(new Player()); s.Controller.Update(); Restored(s);
        });
        Scenario("partial activation failure restores all acquired state", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true });
            s.Player.agentModel.headFemaleRenderer.ThrowOnHide = true;
            s.Controller.Update(); Restored(s);
            Check(s.Log.Errors.Count == 1, "single failure log");
            s.Controller.Update(); Check(s.Log.Errors.Count == 1, "failure latched");
        });
        Scenario("cleanup continues when an individual object cannot restore", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true }); s.Controller.Update(); s.Controller.LateUpdate();
            s.Camera.transform.ThrowOnMove = true; Toggle(s.Controller);
            Check(s.Brain.enabled && Cursor.visible && !s.Player.agentModel.headMaleRenderer.forceRenderingOff, "other state restored");
            Check(s.Log.Warnings.Count == 1, "cleanup failure reported");
            s.Controller.Shutdown(); Check(s.Log.Warnings.Count == 1, "cleanup idempotent");
        });
        Scenario("periodic native-alias failure returns safely to native controls", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true }); s.Controller.Update(); s.Controller.LateUpdate();
            NativeSafety.ThrowOnAudit = true; Time.unscaledTime = 10; s.Controller.Update(); Restored(s);
            var result = new Vector3(9, 0, 1); Check(InputHooks.MousePoint(s.Player, ref result), "native aim restored after failure");
        });
        Scenario("camera replacement and player model replacement restore the old view", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true }); s.Controller.Update(); s.Controller.LateUpdate();
            Camera.main = new(); Check(!s.Controller.Controls(s.Player), "camera change stops hooks immediately");
            s.Controller.LateUpdate(); Restored(s);
            Camera.main = s.Camera; s.Controller.Update();
            var oldHead = s.Player.agentModel.headMaleRenderer;
            s.Player.agentModel = new(); Check(!s.Controller.Controls(s.Player), "model change stops hooks immediately");
            s.Controller.LateUpdate(); Restored(s);
            Check(!oldHead.forceRenderingOff, "old model renderers restored");
        });
        Scenario("paired controller look and optional toggle without global pad selection", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true, PadToggle = true });
            var pad = new Gamepad(); s.Player.InputUser.Device = pad; s.Player.UsingKeyboard = false;
            GameInput.Assignments[pad] = s.Player.clientID.Value;
            pad.rightStick.Value = new(1, 0); s.Controller.Update();
            Check(Near(s.Controller.Yaw, 2.5f), "paired stick integrates elapsed time");
            pad.rightStickButton.wasPressedThisFrame = true; s.Controller.Update(); Restored(s);
        });
        Scenario("Solo controller fallback uses native client identity and ignores other pads", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true });
            s.Player.InputUser.Device = null; s.Player.UsingKeyboard = false;
            var other = new Gamepad(); other.rightStick.Value = new(-1, 0);
            GameInput.Assignments[other] = new ClientIdentifier { LocalID = 999 };
            var owned = new Gamepad(); owned.rightStick.Value = new(1, 0);
            // A fresh wrapper with equal native identity must also match.
            GameInput.Assignments[owned] = new ClientIdentifier();
            s.Controller.Update(); Check(Near(s.Controller.Yaw, 2.5f), "Solo fallback maps only the owning pad");
        });
        Scenario("mouse look wraps yaw, clamps pitch and does not rotate the camera's roll", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true });
            Mouse.current.delta.Value = new(750, 750); s.Controller.Update(); s.Controller.LateUpdate();
            Check(Near(s.Controller.Yaw, 90) && Near(s.Camera.transform.rotation.x, -80), "mouse look and pitch clamp");
            Check(Near(s.Camera.transform.rotation.z, 0), "no animated roll");
            var look = ViewMath.Look(359, 79, 5, 10, true);
            Check(Near(look.Yaw, 4) && Near(look.Pitch, 80), "wrap and inverted clamp");
            Check(Near(ViewMath.Bounded(float.NaN, 85, 50, 110), 85), "nonfinite config fallback");
            var zero = ViewMath.Rotate(0, 0, 120); Check(Near(zero.X, 0) && Near(zero.Z, 0), "native immobilization remains zero");
        });
        Scenario("new cosmetics and body hiding restore while equipment and remote models stay visible", () =>
        {
            var s = Scene(new Settings { StartFirstPerson = true }); s.Controller.Update();
            s.Player.agentModel.selectedMask = new(); s.Controller.LateUpdate();
            var mask = s.Player.agentModel.selectedMask.Renderer;
            Check(mask.forceRenderingOff, "late-spawned mask hidden");
            Toggle(s.Controller); Check(!mask.forceRenderingOff, "mask restored");
            s = Scene(new Settings { StartFirstPerson = true, HideBody = true });
            var body = new Renderer(); s.Player.agentModel.transform.Renderers.Add(body);
            var listedBody = new Renderer(); s.Player.agentModel.mainRenderers.Add(listedBody);
            s.Player.agentModel.mainHandTransform = new();
            var heldWeapon = new Renderer(); heldWeapon.transform.parent = s.Player.agentModel.mainHandTransform;
            s.Player.agentModel.transform.Renderers.Add(heldWeapon);
            s.Player.agentModel.backTransform = new();
            var backEquipment = new Renderer(); backEquipment.transform.parent = s.Player.agentModel.backTransform;
            s.Player.agentModel.transform.Renderers.Add(backEquipment);
            var remote = new Player();
            s.Controller.Update();
            Check(body.forceRenderingOff && listedBody.forceRenderingOff, "body hierarchy and explicit renderer list hidden");
            Check(!heldWeapon.forceRenderingOff && !backEquipment.forceRenderingOff, "held and back equipment remain visible");
            Check(!remote.agentModel.headMaleRenderer.forceRenderingOff, "remote avatar stays visible");
            body.enabled = false; // Native animation changes are independent of our forceRenderingOff flag.
            s.Controller.Shutdown();
            Check(!body.forceRenderingOff && !listedBody.forceRenderingOff, "body restored");
            Check(!body.enabled, "native renderer-enabled changes preserved");
        });
        FirstPersonController.Instance?.Shutdown();
        Console.WriteLine($"{_scenarios} simulated scenarios, {_checks} assertions passed. No game/runtime testing performed.");
    }
}
