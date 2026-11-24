// WWInput.cs
// ---------------------------------------------------------------------------
// One small place that reads the keyboard and mouse.
//
// Unity has two ways to read input: the old "Input Manager" and the newer
// "Input System" package. Depending on your project settings, only one of
// them may be switched on. This file works with either, so the game runs no
// matter which one Unity picked. Everything else in the game just asks
// WWInput "is W held?" and doesn't care how.
// ---------------------------------------------------------------------------

using UnityEngine;
#if ENABLE_INPUT_SYSTEM && WW_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterWars
{
    public enum WWKey
    {
        W, A, S, D, Space, LeftCtrl, C, LeftShift, R, E, Q, G, B, Tab, Escape, Enter,
        Alpha1, Alpha2, LeftBracket, RightBracket, X, F, V
    }

    public static class WWInput
    {
#if ENABLE_INPUT_SYSTEM && WW_INPUT_SYSTEM
        // ---- New Input System -------------------------------------------
        static Key ToKey(WWKey k)
        {
            switch (k)
            {
                case WWKey.W: return Key.W;
                case WWKey.A: return Key.A;
                case WWKey.S: return Key.S;
                case WWKey.D: return Key.D;
                case WWKey.Space: return Key.Space;
                case WWKey.LeftCtrl: return Key.LeftCtrl;
                case WWKey.C: return Key.C;
                case WWKey.LeftShift: return Key.LeftShift;
                case WWKey.R: return Key.R;
                case WWKey.E: return Key.E;
                case WWKey.Q: return Key.Q;
                case WWKey.G: return Key.G;
                case WWKey.B: return Key.B;
                case WWKey.Tab: return Key.Tab;
                case WWKey.Escape: return Key.Escape;
                case WWKey.Enter: return Key.Enter;
                case WWKey.Alpha1: return Key.Digit1;
                case WWKey.Alpha2: return Key.Digit2;
                case WWKey.LeftBracket: return Key.LeftBracket;
                case WWKey.X: return Key.X;
                case WWKey.F: return Key.F;
                case WWKey.V: return Key.V;
                default: return Key.RightBracket;
            }
        }

        public static bool Held(WWKey k) { return Keyboard.current != null && Keyboard.current[ToKey(k)].isPressed; }
        public static bool Pressed(WWKey k) { return Keyboard.current != null && Keyboard.current[ToKey(k)].wasPressedThisFrame; }
        public static bool FireHeld() { return Mouse.current != null && Mouse.current.leftButton.isPressed; }
        public static bool FirePressed() { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; }
        public static bool AimHeld() { return Mouse.current != null && Mouse.current.rightButton.isPressed; }
        public static bool AimPressed() { return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame; }
        // The new system reports mouse movement in pixels; scale it to feel like the old system.
        public static Vector2 MouseDelta() { return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.05f : Vector2.zero; }
#else
        // ---- Old Input Manager ------------------------------------------
        static KeyCode ToKey(WWKey k)
        {
            switch (k)
            {
                case WWKey.W: return KeyCode.W;
                case WWKey.A: return KeyCode.A;
                case WWKey.S: return KeyCode.S;
                case WWKey.D: return KeyCode.D;
                case WWKey.Space: return KeyCode.Space;
                case WWKey.LeftCtrl: return KeyCode.LeftControl;
                case WWKey.C: return KeyCode.C;
                case WWKey.LeftShift: return KeyCode.LeftShift;
                case WWKey.R: return KeyCode.R;
                case WWKey.E: return KeyCode.E;
                case WWKey.Q: return KeyCode.Q;
                case WWKey.G: return KeyCode.G;
                case WWKey.B: return KeyCode.B;
                case WWKey.Tab: return KeyCode.Tab;
                case WWKey.Escape: return KeyCode.Escape;
                case WWKey.Enter: return KeyCode.Return;
                case WWKey.Alpha1: return KeyCode.Alpha1;
                case WWKey.Alpha2: return KeyCode.Alpha2;
                case WWKey.LeftBracket: return KeyCode.LeftBracket;
                case WWKey.X: return KeyCode.X;
                case WWKey.F: return KeyCode.F;
                case WWKey.V: return KeyCode.V;
                default: return KeyCode.RightBracket;
            }
        }

        public static bool Held(WWKey k) { return Input.GetKey(ToKey(k)); }
        public static bool Pressed(WWKey k) { return Input.GetKeyDown(ToKey(k)); }
        public static bool FireHeld() { return Input.GetMouseButton(0); }
        public static bool FirePressed() { return Input.GetMouseButtonDown(0); }
        public static bool AimHeld() { return Input.GetMouseButton(1); }
        public static bool AimPressed() { return Input.GetMouseButtonDown(1); }
        public static Vector2 MouseDelta() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")); }
#endif
    }
}
