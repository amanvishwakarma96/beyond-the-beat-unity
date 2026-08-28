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
        [SerializeField] private bool enableLegacyTouchFallback = true;

        [Header("Editor Fallback")]
        [SerializeField] private bool enableKeyboardFallback = true;

        private readonly List<Vector2> activeScreenTouches = new List<Vector2>(10);
        private bool previousInteractPressed;
        private bool interactionRequested;
        private GUIStyle diagnosticStyle;

        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public bool DirectTouchFallbackEnabled => enableDirectTouchFallback;
        public bool LegacyTouchFallbackEnabled => enableLegacyTouchFallback;
        public bool VehicleBound => vehicleController != null;
        public int LastNewInputTouchCount { get; private set; }
        public int LastLegacyTouchCount { get; private set; }
        public int LastCombinedTouchCount { get; private set; }
        public bool NewTouchscreenAvailable => Touchscreen.current != null;

        public string DiagnosticSummary =>
            $"TOUCH NEW:{(NewTouchscreenAvailable ? "ON" : "OFF")}({LastNewInputTouchCount}) " +
            $"LEG:{LastLegacyTouchCount} ALL:{LastCombinedTouchCount} VEH:{(VehicleBound ? "OK" : "MISS")} " +
            $"S:{Steering:0.0} T:{Throttle:0.0} B:{Brake:0.0} A:{(previousInteractPressed ? 1 : 0)} " +
            $"SPD:{(vehicleController != null ? vehicleController.CurrentSpeedKph : 0f):0}kph";

        public event Action InteractionPressed;

        private void Update()
        {
            ReadTouchInput(out float steering, out float throttle, out float brake, out bool interactPressed);

            if (enableKeyboardFallback)
            {
                ApplyKeyboardFallback(ref steering, ref throttle, ref brake, ref interactPressed);
            }

            ApplyResolvedInput(steering, throttle, brake, interactPressed);
        }

        private void OnGUI()
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }

            if (diagnosticStyle == null)
            {
                diagnosticStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(18, Mathf.RoundToInt(Screen.height * 0.026f)),
                    fontStyle = FontStyle.Bold
                };
                diagnosticStyle.normal.textColor = Color.white;
            }

            float width = Mathf.Min(Screen.width - 24f, 1220f);
            Rect background = new Rect((Screen.width - width) * 0.5f, 10f, width, 48f);
            Color previous = GUI.color;
            GUI.color = new Color(0.01f, 0.02f, 0.03f, 0.82f);
            GUI.Box(background, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(background, DiagnosticSummary, diagnosticStyle);
            GUI.color = previous;
        }

        private void OnDisable()
        {
            Steering = 0f;
            Throttle = 0f;
            Brake = 0f;
            previousInteractPressed = false;
            interactionRequested = false;
            LastNewInputTouchCount = 0;
            LastLegacyTouchCount = 0;
            LastCombinedTouchCount = 0;
            activeScreenTouches.Clear();
            SetControlVisuals(false, false, false, false, false);
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

        public void EvaluateButtonStatesForValidation(
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

        public void EvaluateScreenTouchesForValidation(
            IReadOnlyList<Vector2> screenTouches,
            out float steering,
            out float throttle,
            out float brake,
            out bool interactPressed)
        {
            bool leftPressed = IsPressedByAnySource(steerLeftButton, screenTouches);
            bool rightPressed = IsPressedByAnySource(steerRightButton, screenTouches);
            bool acceleratePressed = IsPressedByAnySource(accelerateButton, screenTouches);
            bool brakeReversePressed = IsPressedByAnySource(brakeReverseButton, screenTouches);
            bool interact = IsPressedByAnySource(interactButton, screenTouches);

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

            if (vehicleController != null)
            {
                vehicleController.SetInput(Steering, Throttle, Brake);
            }

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
            CollectActiveScreenTouches();

            bool leftPressed = IsPressedByAnySource(steerLeftButton, activeScreenTouches);
            bool rightPressed = IsPressedByAnySource(steerRightButton, activeScreenTouches);
            bool acceleratePressed = IsPressedByAnySource(accelerateButton, activeScreenTouches);
            bool brakeReversePressed = IsPressedByAnySource(brakeReverseButton, activeScreenTouches);
            bool interact = IsPressedByAnySource(interactButton, activeScreenTouches);

            SetControlVisuals(leftPressed, rightPressed, acceleratePressed, brakeReversePressed, interact);

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

        private void CollectActiveScreenTouches()
        {
            activeScreenTouches.Clear();
            LastNewInputTouchCount = 0;
            LastLegacyTouchCount = 0;

            if (enableDirectTouchFallback)
            {
                Touchscreen touchscreen = Touchscreen.current;
                if (touchscreen != null)
                {
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var touch = touchscreen.touches[i];
                        if (!touch.press.isPressed)
                        {
                            continue;
                        }

                        activeScreenTouches.Add(touch.position.ReadValue());
                        LastNewInputTouchCount++;
                    }
                }
            }

            if (enableLegacyTouchFallback)
            {
                try
                {
                    int touchCount = UnityEngine.Input.touchCount;
                    for (int i = 0; i < touchCount; i++)
                    {
                        Touch touch = UnityEngine.Input.GetTouch(i);
                        if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                        {
                            continue;
                        }

                        activeScreenTouches.Add(touch.position);
                        LastLegacyTouchCount++;
                    }
                }
                catch (InvalidOperationException)
                {
                    LastLegacyTouchCount = 0;
                }
            }

            LastCombinedTouchCount = activeScreenTouches.Count;
        }

        private static bool IsPressedByAnySource(TouchHoldButton button, IReadOnlyList<Vector2> screenTouches)
        {
            if (button == null)
            {
                return false;
            }

            if (button.IsPressed)
            {
                return true;
            }

            return ContainsAnyTouch(button, screenTouches);
        }

        private void SetControlVisuals(
            bool leftPressed,
            bool rightPressed,
            bool acceleratePressed,
            bool brakeReversePressed,
            bool interactPressed)
        {
            steerLeftButton?.SetVisualPressed(leftPressed);
            steerRightButton?.SetVisualPressed(rightPressed);
            accelerateButton?.SetVisualPressed(acceleratePressed);
            brakeReverseButton?.SetVisualPressed(brakeReversePressed);
            interactButton?.SetVisualPressed(interactPressed);
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
