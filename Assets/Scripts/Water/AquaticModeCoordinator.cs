using System;
using BeyondTheBeat.CameraSystem;
using BeyondTheBeat.UI;
using UnityEngine;

namespace BeyondTheBeat.Water
{
    [DisallowMultipleComponent]
    public sealed class AquaticModeCoordinator : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private MobileDrivingInput drivingInput;
        [SerializeField] private MobileSwimInput swimInput;
        [SerializeField] private SwimController swimController;

        [Header("Camera")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Transform vehicleCameraTarget;
        [SerializeField] private Transform swimCameraTarget;

        [Header("HUD")]
        [SerializeField] private GameObject drivingControlsRoot;
        [SerializeField] private GameObject swimControlsRoot;
        [SerializeField] private GameObject enterSwimControl;
        [SerializeField] private GameObject exitSwimControl;

        [Header("Startup")]
        [SerializeField] private bool startInSwimMode;

        public bool IsSwimMode { get; private set; }
        public MobileDrivingInput DrivingInput => drivingInput;
        public MobileSwimInput SwimInput => swimInput;
        public SwimController SwimController => swimController;
        public CameraFollow CameraFollow => cameraFollow;
        public Transform VehicleCameraTarget => vehicleCameraTarget;
        public Transform SwimCameraTarget => swimCameraTarget;

        public event Action<bool> ModeChanged;

        private void Start()
        {
            SetSwimMode(startInSwimMode, true, true);
        }

        private void OnDisable()
        {
            swimInput?.SetInputEnabled(false);
            swimController?.ClearInput();
        }

        public void EnterSwimMode()
        {
            SetSwimMode(true, true, true);
        }

        public void ExitSwimMode()
        {
            SetSwimMode(false, true, true);
        }

        public bool SetSwimMode(bool swimMode, bool snapCamera = true, bool notify = true)
        {
            ValidateRequiredReferences();

            bool changed = IsSwimMode != swimMode;
            IsSwimMode = swimMode;

            if (IsSwimMode)
            {
                drivingInput.enabled = false;
                if (drivingControlsRoot != null)
                {
                    drivingControlsRoot.SetActive(false);
                }

                if (swimControlsRoot != null)
                {
                    swimControlsRoot.SetActive(true);
                }

                swimInput.SetInputEnabled(true);
                SetOptionalControlState(enterSwimControl, false);
                SetOptionalControlState(exitSwimControl, true);
                cameraFollow.SetTarget(swimCameraTarget, snapCamera);
            }
            else
            {
                swimInput.SetInputEnabled(false);
                swimController.ClearInput();

                if (swimControlsRoot != null)
                {
                    swimControlsRoot.SetActive(false);
                }

                if (drivingControlsRoot != null)
                {
                    drivingControlsRoot.SetActive(true);
                }

                drivingInput.enabled = true;
                SetOptionalControlState(enterSwimControl, true);
                SetOptionalControlState(exitSwimControl, false);
                cameraFollow.SetTarget(vehicleCameraTarget, snapCamera);
            }

            if (changed && notify)
            {
                ModeChanged?.Invoke(IsSwimMode);
            }

            return changed;
        }

        private void ValidateRequiredReferences()
        {
            if (drivingInput == null || swimInput == null || swimController == null || cameraFollow == null ||
                vehicleCameraTarget == null || swimCameraTarget == null)
            {
                throw new InvalidOperationException(
                    "AquaticModeCoordinator is missing required input, swimmer, camera, or camera-target references.");
            }
        }

        private static void SetOptionalControlState(GameObject control, bool active)
        {
            if (control != null && control.activeSelf != active)
            {
                control.SetActive(active);
            }
        }
    }
}
