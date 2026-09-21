using System;
using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Centralized input manager supporting mobile touch virtual joystick,
    /// auto-run toggle, touch screen orbit look (Free Fire / PUBG Mobile style),
    /// and tool hit/strike actions with keyboard fallbacks for PC/Editor testing.
    /// </summary>
    public static class MobileInputManager
    {
        // ── Movement & Auto-Run ──
        public static Vector2 JoystickInput { get; set; } = Vector2.zero;
        public static bool IsAutoRunning { get; private set; } = false;

        public static event Action<bool> AutoRunStateChanged;
        public static event Action OnHitActionTriggered;

        // ── Touch Look Orbit ──
        public static Vector2 LookDelta { get; set; } = Vector2.zero;

        /// <summary>
        /// Reads effective movement input combining virtual joystick, auto-run,
        /// and keyboard WASD/Arrow keys for editor testing.
        /// </summary>
        public static Vector2 GetMoveInput()
        {
            Vector2 input = JoystickInput;

            // Keyboard fallback for editor/PC (supporting both new Input System & legacy)
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    float kH = 0f;
                    float kV = 0f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kH -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kH += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) kV -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) kV += 1f;

                    if (Mathf.Abs(kH) > 0.05f || Mathf.Abs(kV) > 0.05f) input = new Vector2(kH, kV);

                    if (kb.rKey.wasPressedThisFrame) ToggleAutoRun();
                    if (kb.spaceKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) TriggerHitAction();
                }
            }
            catch (System.Exception) { }
#endif
#if !ENABLE_INPUT_SYSTEM
            try
            {
                float keyH = Input.GetAxisRaw("Horizontal");
                float keyV = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(keyH) > 0.05f || Mathf.Abs(keyV) > 0.05f)
                {
                    input = new Vector2(keyH, keyV);
                }

                if (Input.GetKeyDown(KeyCode.R)) ToggleAutoRun();
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F)) TriggerHitAction();
            }
            catch (System.Exception) { }
#endif

            // If auto-running and player pulls joystick back, cancel auto-run
            if (IsAutoRunning)
            {
                if (input.y < -0.3f)
                {
                    SetAutoRun(false);
                }
                else
                {
                    // Force forward motion while preserving steering
                    input.y = 1f;
                }
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        public static void ToggleAutoRun()
        {
            SetAutoRun(!IsAutoRunning);
        }

        public static void SetAutoRun(bool state)
        {
            if (IsAutoRunning != state)
            {
                IsAutoRunning = state;
                AutoRunStateChanged?.Invoke(IsAutoRunning);
            }
        }

        public static void TriggerHitAction()
        {
            OnHitActionTriggered?.Invoke();
        }

        public static void ConsumeLookDelta(out float deltaX, out float deltaY)
        {
            deltaX = LookDelta.x;
            deltaY = LookDelta.y;
            LookDelta = Vector2.zero;
        }
    }
}
