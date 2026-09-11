using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.InputSystem
{
    /// <summary>
    /// Named gameplay actions used by both the existing keyboard controls
    /// and future touch/controller presentation layers.
    /// </summary>
    public enum GameAction
    {
        Jump,
        Sprint,
        Interact,
        SecondaryInteract,
        AssignFarmhand,
        SwitchCharacter,
        Attack,
        Tutorial,
        Wardrobe,
        WardrobeWear,
        WardrobeRemove,
        WardrobeTrial,
        WardrobeNext
    }

    /// <summary>
    /// Small input facade over the project's current Legacy Input Manager.
    /// Gameplay code reads named actions from here instead of binding itself
    /// directly to keys. A future mobile UI can inject a move/look vector and
    /// press/release the same actions without changing the consumers.
    ///
    /// This deliberately does not install Unity's Input System package or
    /// alter InputManager.asset; that migration needs its own approval task.
    /// </summary>
    public static class GameInput
    {
        private struct VirtualButtonState
        {
            public bool held;
            public int pressedFrame;
            public int releasedFrame;
        }

        private static readonly Dictionary<GameAction, VirtualButtonState> VirtualButtons =
            new Dictionary<GameAction, VirtualButtonState>();

        private static Vector2 _virtualMove;
        private static Vector2 _virtualLook;
        private static bool _virtualMoveActive;
        private static bool _virtualLookActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetAtGameStart()
        {
            ResetVirtualInput();
        }

        // MINI-155: movement is WASD only. Unity's default "Horizontal"/
        // "Vertical" axes (still used as-is by vehicle throttle/steer) also
        // bind the arrow keys, which would otherwise both walk the character
        // AND pan the camera from the same press. Reading the letter keys
        // directly here decouples on-foot movement from the arrow keys
        // without touching the shared ProjectSettings/InputManager.asset.
        public static Vector2 Move => _virtualMoveActive
            ? Vector2.ClampMagnitude(_virtualMove, 1f)
            : WasdMove();

        // MINI-155: arrow keys pan/tilt the camera the same way mouse
        // movement does - held Right/Left/Up/Down behaves like a sustained
        // mouse delta on that axis, on top of whatever the mouse itself is
        // doing, so both remain usable together.
        public static Vector2 Look => _virtualLookActive
            ? _virtualLook
            : new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y")) + ArrowKeyLook();

        private static Vector2 WasdMove()
        {
            float x = 0f, y = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) x += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.W)) y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) y -= 1f;
            return new Vector2(x, y);
        }

        private static Vector2 ArrowKeyLook()
        {
            float x = 0f, y = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            return new Vector2(x, y);
        }

        public static bool WasPressed(GameAction action)
        {
            return VirtualButtons.TryGetValue(action, out VirtualButtonState state)
                   && state.pressedFrame == Time.frameCount
                   || LegacyWasPressed(action);
        }

        public static bool IsHeld(GameAction action)
        {
            return VirtualButtons.TryGetValue(action, out VirtualButtonState state) && state.held
                   || LegacyIsHeld(action);
        }

        public static bool WasReleased(GameAction action)
        {
            return VirtualButtons.TryGetValue(action, out VirtualButtonState state)
                   && state.releasedFrame == Time.frameCount
                   || LegacyWasReleased(action);
        }

        /// <summary>Called by a future touch joystick while it is active.</summary>
        public static void SetVirtualMove(Vector2 value)
        {
            _virtualMove = Vector2.ClampMagnitude(value, 1f);
            _virtualMoveActive = true;
        }

        public static void ClearVirtualMove()
        {
            _virtualMove = Vector2.zero;
            _virtualMoveActive = false;
        }

        /// <summary>Called by a future touch-look surface while it is active.</summary>
        public static void SetVirtualLook(Vector2 value)
        {
            _virtualLook = value;
            _virtualLookActive = true;
        }

        public static void ClearVirtualLook()
        {
            _virtualLook = Vector2.zero;
            _virtualLookActive = false;
        }

        /// <summary>
        /// Press or release a named virtual button. Edge queries remain true
        /// for the complete Unity frame, so more than one legitimate consumer
        /// can observe an action without competing to consume it.
        /// </summary>
        public static void SetVirtualButton(GameAction action, bool held)
        {
            VirtualButtons.TryGetValue(action, out VirtualButtonState state);
            if (state.held == held) return;

            state.held = held;
            if (held) state.pressedFrame = Time.frameCount;
            else state.releasedFrame = Time.frameCount;
            VirtualButtons[action] = state;
        }

        /// <summary>Clears injected input when a touch layout closes or a test ends.</summary>
        public static void ResetVirtualInput()
        {
            VirtualButtons.Clear();
            ClearVirtualMove();
            ClearVirtualLook();
        }

        public static KeyCode PrimaryKey(GameAction action)
        {
            switch (action)
            {
                case GameAction.Jump: return KeyCode.Space;
                case GameAction.Sprint: return KeyCode.LeftShift;
                case GameAction.Interact: return KeyCode.E;
                case GameAction.SecondaryInteract: return KeyCode.R;
                case GameAction.AssignFarmhand: return KeyCode.G;
                case GameAction.SwitchCharacter: return KeyCode.Tab;
                case GameAction.Attack: return KeyCode.F;
                case GameAction.Tutorial: return KeyCode.H;
                case GameAction.Wardrobe: return KeyCode.Alpha5;
                case GameAction.WardrobeWear: return KeyCode.Alpha1;
                case GameAction.WardrobeRemove: return KeyCode.Alpha2;
                case GameAction.WardrobeTrial: return KeyCode.Alpha6;
                case GameAction.WardrobeNext: return KeyCode.Alpha3;
                default: return KeyCode.None;
            }
        }

        public static KeyCode SecondaryKey(GameAction action)
        {
            return action == GameAction.Sprint ? KeyCode.RightShift : KeyCode.None;
        }

        public static string BindingLabel(GameAction action)
        {
            KeyCode primary = PrimaryKey(action);
            KeyCode secondary = SecondaryKey(action);
            return secondary == KeyCode.None ? primary.ToString() : $"{primary} / {secondary}";
        }

        private static bool LegacyWasPressed(GameAction action)
        {
            KeyCode primary = PrimaryKey(action);
            KeyCode secondary = SecondaryKey(action);
            return primary != KeyCode.None && UnityEngine.Input.GetKeyDown(primary)
                   || secondary != KeyCode.None && UnityEngine.Input.GetKeyDown(secondary);
        }

        private static bool LegacyIsHeld(GameAction action)
        {
            KeyCode primary = PrimaryKey(action);
            KeyCode secondary = SecondaryKey(action);
            return primary != KeyCode.None && UnityEngine.Input.GetKey(primary)
                   || secondary != KeyCode.None && UnityEngine.Input.GetKey(secondary);
        }

        private static bool LegacyWasReleased(GameAction action)
        {
            KeyCode primary = PrimaryKey(action);
            KeyCode secondary = SecondaryKey(action);
            return primary != KeyCode.None && UnityEngine.Input.GetKeyUp(primary)
                   || secondary != KeyCode.None && UnityEngine.Input.GetKeyUp(secondary);
        }
    }
}
