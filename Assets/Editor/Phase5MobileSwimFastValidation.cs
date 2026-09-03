using System;
using System.Reflection;
using BeyondTheBeat.CameraSystem;
using BeyondTheBeat.UI;
using BeyondTheBeat.Water;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase5MobileSwimFastValidation
    {
        public static void ValidateOrThrow()
        {
            MethodInfo coordinatorUpdate = typeof(AquaticModeCoordinator).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (coordinatorUpdate != null)
            {
                throw new InvalidOperationException(
                    "Phase 5 fast validation failed: AquaticModeCoordinator must remain event/state-transition driven and must not add an Update polling loop.");
            }

            ValidateInputMappingOrThrow();
            ValidateModeHandoffOrThrow();
        }

        private static void ValidateInputMappingOrThrow()
        {
            GameObject inputObject = new GameObject("FastMobileSwimInput");
            try
            {
                MobileSwimInput input = inputObject.AddComponent<MobileSwimInput>();
                input.EvaluateButtonStatesForValidation(
                    true, false, true, false, true, false,
                    out Vector2 move, out bool dive, out bool surface);

                bool diagonal = move.x < -0.6f && move.y > 0.6f && dive && !surface;

                input.EvaluateButtonStatesForValidation(
                    false, true, false, true, true, true,
                    out move, out dive, out surface);
                bool safeConflict = move.x > 0.6f && move.y < -0.6f && !dive && surface;

                if (!diagonal || !safeConflict)
                {
                    throw new InvalidOperationException(
                        $"Phase 5 fast mobile-input validation failed: diagonal={diagonal}, safeConflict={safeConflict}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inputObject);
            }
        }

        private static void ValidateModeHandoffOrThrow()
        {
            GameObject waterObject = null;
            GameObject swimmerObject = null;
            GameObject vehicleObject = null;
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            GameObject drivingControls = null;
            GameObject swimControls = null;
            GameObject enterControl = null;
            GameObject exitControl = null;

            try
            {
                waterObject = new GameObject("FastMobileWater");
                BoxCollider waterCollider = waterObject.AddComponent<BoxCollider>();
                waterCollider.isTrigger = true;
                waterCollider.size = new Vector3(20f, 8f, 20f);
                waterCollider.center = new Vector3(0f, -4f, 0f);
                ZoneContext context = waterObject.AddComponent<ZoneContext>();
                ConfigureZoneContext(context);
                WaterVolume water = waterObject.AddComponent<WaterVolume>();
                ConfigureWaterVolume(water, context, waterCollider);

                swimmerObject = new GameObject("FastMobileSwimmer");
                swimmerObject.transform.position = new Vector3(0f, -0.55f, 0f);
                Rigidbody body = swimmerObject.AddComponent<Rigidbody>();
                body.useGravity = true;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                SwimController swimController = swimmerObject.AddComponent<SwimController>();
                ConfigureSwimController(swimController, body, water, swimmerObject.transform);
                swimController.BindWaterVolume(water, true);

                vehicleObject = new GameObject("FastVehicleTarget");
                cameraObject = new GameObject("FastGameplayCamera");
                CameraFollow cameraFollow = cameraObject.AddComponent<CameraFollow>();
                cameraFollow.SetTarget(vehicleObject.transform, false);

                canvasObject = new GameObject("FastMobileCanvas");
                MobileDrivingInput drivingInput = canvasObject.AddComponent<MobileDrivingInput>();
                AquaticModeCoordinator coordinator = canvasObject.AddComponent<AquaticModeCoordinator>();

                drivingControls = new GameObject("FastDrivingControls");
                drivingControls.transform.SetParent(canvasObject.transform, false);
                swimControls = new GameObject("FastSwimControls");
                swimControls.transform.SetParent(canvasObject.transform, false);
                MobileSwimInput swimInput = swimControls.AddComponent<MobileSwimInput>();
                ConfigureMobileSwimInput(swimInput, swimController);

                enterControl = new GameObject("FastEnterSwim");
                enterControl.transform.SetParent(canvasObject.transform, false);
                exitControl = new GameObject("FastExitSwim");
                exitControl.transform.SetParent(swimControls.transform, false);

                ConfigureCoordinator(
                    coordinator,
                    drivingInput,
                    swimInput,
                    swimController,
                    cameraFollow,
                    vehicleObject.transform,
                    swimmerObject.transform,
                    drivingControls,
                    swimControls,
                    enterControl,
                    exitControl);

                coordinator.SetSwimMode(false, false, false);
                bool driveBaseline = drivingInput.enabled && !swimInput.InputEnabled &&
                                     drivingControls.activeSelf && !swimControls.activeSelf &&
                                     cameraFollow.Target == vehicleObject.transform;

                coordinator.SetSwimMode(true, false, false);
                bool swimMode = !drivingInput.enabled && swimInput.InputEnabled &&
                                !drivingControls.activeSelf && swimControls.activeSelf &&
                                cameraFollow.Target == swimmerObject.transform;

                coordinator.SetSwimMode(false, false, false);
                bool driveRestored = drivingInput.enabled && !swimInput.InputEnabled &&
                                     drivingControls.activeSelf && !swimControls.activeSelf &&
                                     cameraFollow.Target == vehicleObject.transform;

                if (!driveBaseline || !swimMode || !driveRestored)
                {
                    throw new InvalidOperationException(
                        "Phase 5 fast camera/input handoff validation failed: " +
                        $"driveBaseline={driveBaseline}, swimMode={swimMode}, driveRestored={driveRestored}.");
                }
            }
            finally
            {
                if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (vehicleObject != null) UnityEngine.Object.DestroyImmediate(vehicleObject);
                if (swimmerObject != null) UnityEngine.Object.DestroyImmediate(swimmerObject);
                if (waterObject != null) UnityEngine.Object.DestroyImmediate(waterObject);
            }
        }

        private static void ConfigureZoneContext(ZoneContext context)
        {
            SerializedObject serialized = new SerializedObject(context);
            SetString(serialized, "zoneId", "fast-mobile-ocean");
            SetInt(serialized, "zoneType", (int)WorldZoneType.Ocean);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWaterVolume(WaterVolume water, ZoneContext context, BoxCollider collider)
        {
            SerializedObject serialized = new SerializedObject(water);
            SetObjectReference(serialized, "zoneContext", context);
            SetObjectReference(serialized, "volumeCollider", collider);
            SetFloat(serialized, "surfaceY", 0f);
            SetFloat(serialized, "maxDepth", 8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSwimController(SwimController controller, Rigidbody body, WaterVolume water, Transform reference)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetObjectReference(serialized, "body", body);
            SetObjectReference(serialized, "waterVolume", water);
            SetObjectReference(serialized, "movementReference", reference);
            SetFloat(serialized, "surfaceDepth", 0.55f);
            SetFloat(serialized, "targetDiveDepth", 3f);
            SetFloat(serialized, "bottomClearance", 0.75f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMobileSwimInput(MobileSwimInput input, SwimController controller)
        {
            SerializedObject serialized = new SerializedObject(input);
            SetObjectReference(serialized, "swimController", controller);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            input.SetInputEnabled(false);
        }

        private static void ConfigureCoordinator(
            AquaticModeCoordinator coordinator,
            MobileDrivingInput drivingInput,
            MobileSwimInput swimInput,
            SwimController swimController,
            CameraFollow cameraFollow,
            Transform vehicleTarget,
            Transform swimTarget,
            GameObject drivingControls,
            GameObject swimControls,
            GameObject enterControl,
            GameObject exitControl)
        {
            SerializedObject serialized = new SerializedObject(coordinator);
            SetObjectReference(serialized, "drivingInput", drivingInput);
            SetObjectReference(serialized, "swimInput", swimInput);
            SetObjectReference(serialized, "swimController", swimController);
            SetObjectReference(serialized, "cameraFollow", cameraFollow);
            SetObjectReference(serialized, "vehicleCameraTarget", vehicleTarget);
            SetObjectReference(serialized, "swimCameraTarget", swimTarget);
            SetObjectReference(serialized, "drivingControlsRoot", drivingControls);
            SetObjectReference(serialized, "swimControlsRoot", swimControls);
            SetObjectReference(serialized, "enterSwimControl", enterControl);
            SetObjectReference(serialized, "exitSwimControl", exitControl);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized object property '{name}'.");
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string name, string value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized string property '{name}'.");
            property.stringValue = value;
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized int property '{name}'.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized float property '{name}'.");
            property.floatValue = value;
        }
    }
}
