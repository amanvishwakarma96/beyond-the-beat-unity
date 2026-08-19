using UnityEngine;
using UnityEngine.InputSystem;

namespace BeyondTheBeat.Vehicle
{
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleDebugInput : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 1f)] private float steeringSensitivity = 1f;
        [SerializeField, Range(0.1f, 1f)] private float throttleSensitivity = 1f;

        private VehicleController controller;

        private void Awake()
        {
            controller = GetComponent<VehicleController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                controller.ClearInput();
                return;
            }

            float steer = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                steer -= steeringSensitivity;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                steer += steeringSensitivity;
            }

            float throttle = 0f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                throttle += throttleSensitivity;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                throttle -= throttleSensitivity;
            }

            float brake = keyboard.spaceKey.isPressed ? 1f : 0f;
            controller.SetInput(steer, throttle, brake);
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.ClearInput();
            }
        }
    }
}
