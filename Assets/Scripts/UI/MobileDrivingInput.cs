using System;
using BeyondTheBeat.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeyondTheBeat.UI
{
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

        [Header("Editor Fallback")]
        [SerializeField] private bool enableKeyboardFallback = true;

        private bool previousInteractPressed;
        private bool interactionRequested;

        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }

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

        private void ReadTouchInput(
            out float steering,
            out float throttle,
            out float brake,
            out bool interactPressed)
        {
            bool leftPressed = steerLeftButton != null && steerLeftButton.IsPressed;
            bool rightPressed = steerRightButton != null && steerRightButton.IsPressed;
            bool acceleratePressed = accelerateButton != null && accelerateButton.IsPressed;
            bool brakeReversePressed = brakeReverseButton != null && brakeReverseButton.IsPressed;

            steering = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);

            if (acceleratePressed && brakeReversePressed)
            {
                // Conflicting pedals resolve to a full brake instead of applying motor torque.
                throttle = 0f;
                brake = 1f;
            }
            else
            {
                throttle = (acceleratePressed ? 1f : 0f) - (brakeReversePressed ? 1f : 0f);
                brake = 0f;
            }

            interactPressed = interactButton != null && interactButton.IsPressed;
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
