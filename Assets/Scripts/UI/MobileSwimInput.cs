using System;
using System.Collections.Generic;
using BeyondTheBeat.Water;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileSwimInput : MonoBehaviour
    {
        [Header("Swimmer")]
        [SerializeField] private SwimController swimController;

        [Header("Touch Controls")]
        [SerializeField] private TouchHoldButton moveLeftButton;
        [SerializeField] private TouchHoldButton moveRightButton;
        [SerializeField] private TouchHoldButton moveForwardButton;
        [SerializeField] private TouchHoldButton moveBackButton;
        [SerializeField] private TouchHoldButton diveButton;
        [SerializeField] private TouchHoldButton surfaceButton;
        [SerializeField] private bool enableDirectTouchFallback = true;
        [SerializeField] private bool enableLegacyTouchFallback = true;

        [Header("Editor Fallback")]
        [SerializeField] private bool enableKeyboardFallback = true;

        private readonly List<Vector2> activeScreenTouches = new List<Vector2>(10);
        private bool inputEnabled;

        public SwimController SwimController => swimController;
        public Vector2 MoveInput { get; private set; }
        public bool DivePressed { get; private set; }
        public bool SurfacePressed { get; private set; }
        public bool InputEnabled => inputEnabled;
        public bool DirectTouchFallbackEnabled => enableDirectTouchFallback;
        public bool LegacyTouchFallbackEnabled => enableLegacyTouchFallback;
        public bool HasRequiredControls =>
            moveLeftButton != null &&
            moveRightButton != null &&
            moveForwardButton != null &&
            moveBackButton != null &&
            diveButton != null &&
            surfaceButton != null;

        private void Update()
        {
            if (!inputEnabled || swimController == null)
            {
                return;
            }

            ReadTouchInput(out Vector2 move, out bool dive, out bool surface);

            if (enableKeyboardFallback)
            {
                ApplyKeyboardFallback(ref move, ref dive, ref surface);
            }

            ApplyResolvedInput(move, dive, surface);
        }

        private void OnDisable()
        {
            ClearResolvedInput();
        }

        public void SetSwimController(SwimController controller)
        {
            if (swimController == controller)
            {
                return;
            }

            swimController?.ClearInput();
            swimController = controller;

            if (!inputEnabled)
            {
                swimController?.ClearInput();
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            if (inputEnabled == enabled)
            {
                if (!enabled)
                {
                    ClearResolvedInput();
                }
                return;
            }

            inputEnabled = enabled;
            if (!inputEnabled)
            {
                ClearResolvedInput();
            }
        }

        public void EvaluateButtonStatesForValidation(
            bool leftPressed,
            bool rightPressed,
            bool forwardPressed,
            bool backPressed,
            bool divePressed,
            bool surfacePressed,
            out Vector2 move,
            out bool dive,
            out bool surface)
        {
            ResolveButtonStates(
                leftPressed,
                rightPressed,
                forwardPressed,
                backPressed,
                divePressed,
                surfacePressed,
                out move,
                out dive,
                out surface);
        }

        private void ReadTouchInput(out Vector2 move, out bool dive, out bool surface)
        {
            CollectActiveScreenTouches();

            bool leftPressed = IsPressedByAnySource(moveLeftButton, activeScreenTouches);
            bool rightPressed = IsPressedByAnySource(moveRightButton, activeScreenTouches);
            bool forwardPressed = IsPressedByAnySource(moveForwardButton, activeScreenTouches);
            bool backPressed = IsPressedByAnySource(moveBackButton, activeScreenTouches);
            bool divePressed = IsPressedByAnySource(diveButton, activeScreenTouches);
            bool surfacePressed = IsPressedByAnySource(surfaceButton, activeScreenTouches);

            ResolveButtonStates(
                leftPressed,
                rightPressed,
                forwardPressed,
                backPressed,
                divePressed,
                surfacePressed,
                out move,
                out dive,
                out surface);

            SetControlVisuals(leftPressed, rightPressed, forwardPressed, backPressed, divePressed, surfacePressed);
        }

        private void ApplyResolvedInput(Vector2 move, bool dive, bool surface)
        {
            MoveInput = Vector2.ClampMagnitude(move, 1f);
            DivePressed = dive;
            SurfacePressed = surface;

            swimController.SetMoveInput(MoveInput);

            if (SurfacePressed)
            {
                swimController.SetDiveRequested(false);
            }
            else if (DivePressed)
            {
                swimController.SetDiveRequested(true);
            }
        }

        private void ClearResolvedInput()
        {
            MoveInput = Vector2.zero;
            DivePressed = false;
            SurfacePressed = false;
            activeScreenTouches.Clear();
            SetControlVisuals(false, false, false, false, false, false);
            swimController?.ClearInput();
        }

        private void CollectActiveScreenTouches()
        {
            activeScreenTouches.Clear();

            if (enableDirectTouchFallback)
            {
                Touchscreen touchscreen = Touchscreen.current;
                if (touchscreen != null)
                {
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var touch = touchscreen.touches[i];
                        if (touch.press.isPressed)
                        {
                            activeScreenTouches.Add(touch.position.ReadValue());
                        }
                    }
                }
            }

            if (!enableLegacyTouchFallback)
            {
                return;
            }

            try
            {
                int touchCount = UnityEngine.Input.touchCount;
                for (int i = 0; i < touchCount; i++)
                {
                    UnityEngine.Touch touch = UnityEngine.Input.GetTouch(i);
                    if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                    {
                        continue;
                    }

                    activeScreenTouches.Add(touch.position);
                }
            }
            catch (InvalidOperationException)
            {
                // Active Input Handling can disable the legacy API in some editor configurations.
                // The fail-closed Android input build guard and Input System path remain authoritative.
            }
        }

        private static bool IsPressedByAnySource(TouchHoldButton button, IReadOnlyList<Vector2> touches)
        {
            if (button == null)
            {
                return false;
            }

            if (button.IsPressed)
            {
                return true;
            }

            if (touches == null)
            {
                return false;
            }

            for (int i = 0; i < touches.Count; i++)
            {
                if (button.ContainsScreenPoint(touches[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetControlVisuals(
            bool left,
            bool right,
            bool forward,
            bool back,
            bool dive,
            bool surface)
        {
            moveLeftButton?.SetVisualPressed(left);
            moveRightButton?.SetVisualPressed(right);
            moveForwardButton?.SetVisualPressed(forward);
            moveBackButton?.SetVisualPressed(back);
            diveButton?.SetVisualPressed(dive);
            surfaceButton?.SetVisualPressed(surface);
        }

        private static void ResolveButtonStates(
            bool leftPressed,
            bool rightPressed,
            bool forwardPressed,
            bool backPressed,
            bool divePressed,
            bool surfacePressed,
            out Vector2 move,
            out bool dive,
            out bool surface)
        {
            float horizontal = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
            float vertical = (forwardPressed ? 1f : 0f) - (backPressed ? 1f : 0f);
            move = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            // Surface is the safer conflict winner if both vertical-state buttons are held.
            surface = surfacePressed;
            dive = divePressed && !surfacePressed;
        }

        private static void ApplyKeyboardFallback(ref Vector2 move, ref bool dive, ref bool surface)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 keyboardMove = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                keyboardMove.x -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                keyboardMove.x += 1f;
            }
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                keyboardMove.y += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                keyboardMove.y -= 1f;
            }

            if (move.sqrMagnitude < 0.001f)
            {
                move = Vector2.ClampMagnitude(keyboardMove, 1f);
            }

            surface |= keyboard.rKey.isPressed;
            dive |= keyboard.fKey.isPressed && !surface;
        }
    }
}
