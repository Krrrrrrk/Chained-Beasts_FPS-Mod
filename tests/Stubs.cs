// Inert managed substitutes. These tests do NOT load Unity, IL2CPP, BepInEx, or the game.
namespace UnityEngine
{
    public class Object
    {
        private static int _next;
        public IntPtr Pointer { get; set; } = (IntPtr)(++_next);
        public bool Destroyed;
        public static bool operator ==(Object a, Object b) =>
            ReferenceEquals(a, b) || ((ReferenceEquals(a, null) || a.Destroyed) && (ReferenceEquals(b, null) || b.Destroyed));
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => Pointer.GetHashCode();
    }
    public class Transform : Object
    {
        public Vector3 position;
        public Quaternion rotation;
        public Transform parent;
        public bool ThrowOnMove;
        public List<Renderer> Renderers = new();
        public void SetPositionAndRotation(Vector3 p, Quaternion q)
        { if (ThrowOnMove) throw new InvalidOperationException("Destroyed transform"); position = p; rotation = q; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Renderers.OfType<T>().ToArray();
        public bool IsChildOf(Transform ancestor) => ReferenceEquals(this, ancestor) || (parent != null && parent.IsChildOf(ancestor));
    }
    public class Component : Object
    {
        public Transform transform = new();
        public GameObject gameObject = new();
        public bool enabled = true, isActiveAndEnabled = true;
        public T GetComponent<T>() where T : class => gameObject.Brain as T;
        public T[] GetComponentsInChildren<T>(bool includeInactive) => transform.GetComponentsInChildren<T>(includeInactive);
    }
    public class MonoBehaviour : Component
    { public MonoBehaviour() { } public MonoBehaviour(IntPtr pointer) { Pointer = pointer; } }
    public class GameObject : Object
    {
        public bool activeInHierarchy = true;
        public Cinemachine.CinemachineBrain Brain;
        public List<Renderer> Renderers = new();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Renderers.OfType<T>().ToArray();
    }
    public class Renderer : Component
    {
        private bool _hidden;
        public bool ThrowOnHide;
        public bool forceRenderingOff
        { get => _hidden; set { if (value && ThrowOnHide) throw new InvalidOperationException("Renderer mutation failed"); _hidden = value; } }
    }
    public class Camera : Component
    {
        public static Camera main;
        public float fieldOfView = 53, nearClipPlane = .3f;
        public bool orthographic = true;
        public object targetTexture;
        public int targetDisplay;
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new(0, 0);
        public static Vector2 operator *(Vector2 v, float factor) => new(v.x * factor, v.y * factor);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator *(Vector3 v, float factor) => new(v.x * factor, v.y * factor, v.z * factor);
    }
    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion Euler(float x, float y, float z) => new(x, y, z, 1);
    }
    public static class Mathf
    { public const float Rad2Deg = 180f / MathF.PI; public static float Atan2(float y, float x) => MathF.Atan2(y, x); public static float Min(float a, float b) => Math.Min(a, b); public static float Max(float a, float b) => Math.Max(a, b); }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState; public static bool visible = true; }
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static float unscaledTime, unscaledDeltaTime = 1f / 60, timeScale = 1; }
    public enum EventType { Repaint }
    public class Event { public static Event current = new(); public EventType type; }
    public struct Color { public static Color white => new(); }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public class Texture2D { public static Texture2D whiteTexture = new(); }
    public static class Screen { public static int width = 1920, height = 1080; }
    public static class GUI
    { public static int depth; public static Color color; public static void DrawTexture(Rect r, Texture2D t) { } public static void Label(Rect r, string s) { } }
}
namespace UnityEngine.InputSystem
{
    public enum Key { None, F9 }
    public class ButtonControl { public bool wasPressedThisFrame; }
    public class VectorControl { public UnityEngine.Vector2 Value; public UnityEngine.Vector2 ReadValue() => Value; }
    public class InputDevice { public T TryCast<T>() where T : class => this as T; }
    public class Keyboard : InputDevice
    { public static Keyboard current; public ButtonControl Button = new(); public ButtonControl this[Key key] => Button; }
    public class Mouse : InputDevice { public static Mouse current; public VectorControl delta = new(); }
    public class Gamepad : InputDevice
    {
        public static List<Gamepad> all = new();
        public Gamepad() => all.Add(this);
        public VectorControl rightStick = new(); public ButtonControl rightStickButton = new();
    }
}
namespace Il2CppInterop.Runtime { public class Placeholder { } }
namespace Il2CppInterop.Runtime.Attributes { public class HideFromIl2CppAttribute : Attribute { } }
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public class Il2CppReferenceArray<T> : List<T> { }
}
namespace HarmonyLib { public class Harmony { } }
namespace BepInEx.Logging
{
    public class ManualLogSource
    {
        public List<string> Errors = new(), Infos = new(), Warnings = new();
        public void LogError(object text) => Errors.Add(text.ToString());
        public void LogInfo(object text) => Infos.Add(text.ToString());
        public void LogWarning(object text) => Warnings.Add(text.ToString());
    }
}
namespace Cinemachine { public class CinemachineBrain : UnityEngine.Component { } }
namespace ChainedBeasts.FirstPerson
{
    internal class Settings
    {
        public bool StartFirstPerson, InvertY = false, HideHead = true, HideBody = true, Crosshair = true, PadToggle;
        public UnityEngine.InputSystem.Key ToggleKey = UnityEngine.InputSystem.Key.F9;
        public float Fov = 85, NearClip = .04f, MouseSensitivity = .12f, StickSpeed = 150, HeightOffset = .06f, ForwardOffset = .08f;
    }
    internal static class NativeSafety
    {
        public static bool ThrowOnAudit;
        public static void Audit() { if (ThrowOnAudit) throw new InvalidOperationException("Native alias"); }
    }
}
public class ClientIdentifier { public ulong NetworkID = 10, LocalID = 20, PlayerIndex = 0; }
public static class GameInput
{
    public static Dictionary<UnityEngine.InputSystem.InputDevice, ClientIdentifier> Assignments = new();
    public static ClientIdentifier GetDeviceClient(UnityEngine.InputSystem.InputDevice device) =>
        Assignments.TryGetValue(device, out var client) ? client : null;
}
public class Variable<T> { public T Value; }
public class UserInput { public UnityEngine.InputSystem.InputDevice Device; }
public class NetworkAgent : UnityEngine.MonoBehaviour
{
    public bool IsSpawned = true, IsDead, IsIncapacitated;
    public UnityEngine.Vector3 ModelForward = new(0, 0, 1);
}
public class Player : NetworkAgent
{
    public bool IsExecuting, IsInputBlocked, AutoMoveActive, UsingKeyboard = true;
    public AgentModel agentModel = new();
    public Variable<ClientIdentifier> clientID = new() { Value = new() };
    public UserInput InputUser = new() { Device = new UnityEngine.InputSystem.Keyboard() };
}
public class AgentModel : UnityEngine.Component
{
    public UnityEngine.Transform headTransform = new() { position = new(10, 1.7f, 20) }, eyePatch;
    public UnityEngine.Transform mainHandTransform, offHandTransform, bothHandsTransform, bowTransform, backTransform;
    public UnityEngine.Renderer headMaleRenderer = new(), headFemaleRenderer = new();
    public UnityEngine.GameObject crownInstance;
    public AgentModelMask selectedMask;
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.Renderer>
        mainRenderers = new(), browRenderers = new(), hairRenderers_M_act0 = new(), hairRenderers_M_act1 = new(), hairRenderers_M_act2 = new(),
        hairRenderers_F_act0 = new(), hairRenderers_F_act1 = new(), hairRenderers_F_act2 = new(),
        beardRenderers_Act0 = new(), beardRenderers_Act1 = new(), beardRenderers_Act2 = new();
}
public class AgentModelMask { public UnityEngine.Renderer Renderer = new(); }
public class GameLevel
{ public enum eGameState { RUNNING, LEVEL_COMPLETE, LEVEL_FAILED } public bool IsReady = true, BossCinematicPlaying; public eGameState ActiveGameState; }
public class NetworkSession
{
    public static NetworkSession Instance;
    public static bool InstanceExists => Instance != null;
    public bool IsSpawned = true, IsPaused, TransitionPending;
    public GameLevel Level = new();
    public HashSet<ClientIdentifier> LocalClients = new();
    public bool IsLocalClient(ClientIdentifier id) => LocalClients.Contains(id);
}
public class PlayerManager
{
    public static PlayerManager Instance;
    public static bool InstanceExists => Instance != null;
    public List<Player> localPlayers = new();
}
public static class UIManager { public static bool GameInputBlocked; }
