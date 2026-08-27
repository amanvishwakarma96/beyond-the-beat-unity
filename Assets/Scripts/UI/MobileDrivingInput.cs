using System;
using System.Collections.Generic;
using BeyondTheBeat.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileDrivingInput : MonoBehaviour
    {
        [Header("Vehicle")]
        [SerializeField] private VehicleController vehicleController;

        [Header("Touch Controls")]
        [SerializeField] private TouchHoldButton steerLeftButton;
        [SerializeField] private TouchHoldButton steerRightButton;
        [SerializeField] private TouchHoldButton accelerateButton;
        [SerializeField] private TouchHoldButton brakeReverseButton;
        [SerializeField] private TouchHoldButton interactButton;
        [SerializeField] private bool enableDirectTouchFallback = true;

        [Header("Editor Fallback")]
        [SerializeField] private bool enableKeyboardFallback = true;

        private bool previousInteractPressed;
        private bool interactionRequested;

        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public bool DirectTouchFallbackEnabled => enableDirectTouchFallback;

        public event Action InteractionPressed;

        private void Update()
        {
            if (vehicleController == null)
            {
                return;
            }

            ReadTouchInput(out float steering, out float throttle, out float brake, out bool interactPressed);

            if (enableKeyboardFallback)
            {
                ApplyKeyboardFallback(ref steering, ref throttle, ref brake, ref interactPressed);
            }

            ApplyResolvedInput(steering, throttle, brake, interactPressed);
        }

        private void OnDisable()
        {
            Steering = 0f;
            Throttle = 0f;
            Brake = 0f;
            previousInteractPressed = false;
            interactionRequested = false;
            vehicleController?.ClearInput();
        }

        public bool ConsumeInteractionRequest()
        {
            if (!interactionRequested)
            {
                return false;
            }

            interactionRequested = false;
            return true;
        }

        public void SetVehicleController(VehicleController controller)
        {
            vehicleController = controller;
        }

        public void EvaluateScreenTouchesForValidation(
            IReadOnlyList<Vector2> screenTouches,
            out float steering,
            out float throttle,
            out float brake,
            out bool interactPressed)
        {
            bool leftPressed = ContainsAnyTouch(steerLeftButton, screenTouches);
            bool rightPressed = ContainsAnyTouch(steerRightButton, screenTouches);
            bool acceleratePressed = ContainsAnyTouch(accelerateButton, screenTouches);
            bool brakeReversePressed = ContainsAnyTouch(brakeReverseButton, screenTouches);
            bool interact = ContainsAnyTouch(interactButton, screenTouches);

            ResolveButtonStates(
                leftPressed,
                rightPressed,
                acceleratePressed,
                brakeReversePressed,
                interact,
                out steering,
                out throttle,
                out brake,
                out interactPressed);
        }

        private void ApplyResolvedInput(float steering, float throttle, float brake, bool interactPressed)
        {
            if (brake > 0.01f)
            {
                throttle = 0f;
            }

            Steering = Mathf.Clamp(steering, -1f, 1f);
            Throttle = Mathf.Clamp(throttle, -1f, 1f);
            Brake = Mathf.Clamp01(brake);

            vehicleController.SetInput(Steering, Throttle, Brake);

            if (interactPressed && !previousInteractPressed)
            {
                interactionRequested = true;
                InteractionPressed?.Invoke();
            }

            previousInteractPressed = interactPressed;
        }

        private void ReadTouchInput(
            out float steering,
            out float throttle,
            out float brake,
            out bool interactPressed)
        {
            bool leftPressed = IsControlPressed(steerLeftButton);
            bool rightPressed = IsControlPressed(steerRightButton);
            bool acceleratePressed = IsControlPressed(accelerateButton);
            bool brakeReversePressed = IsControlPressed(brakeReverseButton);
            bool interact = IsControlPressed(interactButton);

            ResolveButtonStates(
                leftPressed,
                rightPressed,
                acceleratePressed,
                brakeReversePressed,
                interact,
                out steering,
                out throttle,
                out brake,
                out interactPressed);
        }

        private bool IsControlPressed(TouchHoldButton button)
        {
            if (button == null)
            {
                return false;
            }

            if (button.IsPressed)
            {
                return true;
            }

            if (!enableDirectTouchFallback)
            {
                return false;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                Vector2 position = touch.position.ReadValue();
                if (button.ContainsScreenPoint(position))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAnyTouch(TouchHoldButton button, IReadOnlyList<Vector2> screenTouches)
        {
            if (button == null || screenTouches == null)
            {
                return false;
            }

            for (int i = 0; i < screenTouches.Count; i++)
            {
                if (button.ContainsScreenPoint(screenTouches[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResolveButtonStates(
            bool leftPressed,
            bool rightPressed,
            bool acceleratePressed,
            bool brakeReversePressed,
            bool interact,
            out float steering,
            out float throttle,
            out float brake,
            out bool interactPressed)
        {
            steering = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);

            if (acceleratePressed && brakeReversePressed)
            {
                throttle = 0f;
                brake = 1f;
            }
            else
            {
                throttle = (acceleratePressed ? 1f : 0f) - (brakeReversePressed ? 1f : 0f);
                brake = 0f;
            }

            interactPressed = interact;
        }

        private static void ApplyKeyboardFallback(
            ref float steering,
            ref float throttle,
            ref float brake,
            ref bool interactPressed)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            float keyboardSteering = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                keyboardSteering -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                keyboardSteering += 1f;
            }

            float keyboardThrottle = 0f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                keyboardThrottle += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                keyboardThrottle -= 1f;
            }

            if (Mathf.Abs(steering) < 0.01f)
            {
                steering = keyboardSteering;
            }
            if (Mathf.Abs(throttle) < 0.01f)
            {
                throttle = keyboardThrottle;
            }
            if (brake < 0.01f && keyboard.spaceKey.isPressed)
            {
                brake = 1f;
            }

            interactPressed |= keyboard.eKey.isPressed;
        }
    }
}
