using System;
using System.Reflection;
using BeyondTheBeat.Water;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Lightweight pull-request gate for the active Phase 5 milestone.
    /// It intentionally avoids rebuilding the historical phase scene chain and avoids BuildPipeline.BuildPlayer.
    /// Full integrated scene generation and Android packaging remain in Phase5BuildAutomation.BuildAndroid.
    /// </summary>
    public static class Phase5FastValidation
    {
        public static void Validate()
        {
            InputBackendBuildGuard.EnsureBothInputBackends();
            ValidateArchitectureOrThrow();
            ValidateBehaviorOrThrow();

            Debug.Log(
                "[Beyond The Beat] FAST PR VALIDATION PASS: scripts compiled, Android input backend guard passed, " +
                "and the Phase 5 swim/dive controller contract passed without rebuilding prior phase scenes or packaging an APK.");
        }

        private static void ValidateArchitectureOrThrow()
        {
            MethodInfo update = typeof(SwimController).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            MethodInfo fixedUpdate = typeof(SwimController).GetMethod(
                "FixedUpdate",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (update != null || fixedUpdate == null)
            {
                throw new InvalidOperationException(
                    "Phase 5 fast validation failed: SwimController must avoid an Update polling loop and keep physics work in FixedUpdate.");
            }
        }

        private static void ValidateBehaviorOrThrow()
        {
            GameObject waterObject = null;
            GameObject actorObject = null;

            try
            {
                waterObject = new GameObject("FastValidationWater");
                BoxCollider waterCollider = waterObject.AddComponent<BoxCollider>();
                waterCollider.isTrigger = true;
                waterCollider.size = new Vector3(20f, 8f, 20f);
                waterCollider.center = new Vector3(0f, -4f, 0f);

                ZoneContext context = waterObject.AddComponent<ZoneContext>();
                ConfigureZoneContext(context, "fast-validation-ocean", WorldZoneType.Ocean);

                WaterVolume water = waterObject.AddComponent<WaterVolume>();
                ConfigureWaterVolume(water, context, waterCollider, 0f, 8f);

                actorObject = new GameObject("FastValidationSwimmer");
                actorObject.transform.position = new Vector3(0f, -0.55f, 0f);

                Rigidbody body = actorObject.AddComponent<Rigidbody>();
                body.mass = 75f;
                body.useGravity = true;
                body.linearDamping = 0.15f;
                body.angularDamping = 4f;
                body.constraints = RigidbodyConstraints.FreezeRotation;

                SwimController controller = actorObject.AddComponent<SwimController>();
                ConfigureSwimController(controller, body, water, actorObject.transform);

                bool entered = controller.BindWaterVolume(water, true) &&
                               controller.IsInWater &&
                               controller.State == AquaticState.Surface &&
                               !body.useGravity;

                controller.SetMoveInput(Vector2.up);
                Vector3 forwardVelocity = controller.GetTargetVelocity(actorObject.transform.position);
                bool surfaceMovement = forwardVelocity.z > 0.1f && Mathf.Abs(forwardVelocity.x) < 0.01f;

                float clampedDepth = controller.SetTargetDiveDepth(100f);
                bool depthClamped = clampedDepth <= water.MaxDepth &&
                                    Mathf.Approximately(clampedDepth, controller.MaxAllowedDiveDepth);

                bool diveRequested = controller.SetDiveRequested(true) && controller.State == AquaticState.Underwater;
                Vector3 diveVelocity = controller.GetTargetVelocity(actorObject.transform.position);
                bool divesDown = diveVelocity.y < -0.01f;

                actorObject.transform.position = new Vector3(0f, -4f, 0f);
                bool surfaceRequested = controller.SetDiveRequested(false) && controller.State == AquaticState.Surface;
                Vector3 surfaceVelocity = controller.GetTargetVelocity(actorObject.transform.position);
                bool surfacesUp = surfaceVelocity.y > 0.01f;

                bool exited = controller.ExitWater() &&
                              !controller.IsInWater &&
                              controller.State == AquaticState.Dry &&
                              body.useGravity &&
                              controller.MoveInput == Vector2.zero;

                actorObject.transform.position = new Vector3(0f, -0.55f, 0f);
                bool reentered = controller.BindWaterVolume(water, true) &&
                                 controller.IsInWater &&
                                 controller.State == AquaticState.Surface &&
                                 !body.useGravity;

                if (!entered || !surfaceMovement || !depthClamped || !diveRequested || !divesDown ||
                    !surfaceRequested || !surfacesUp || !exited || !reentered)
                {
                    throw new InvalidOperationException(
                        "Phase 5 fast validation failed: " +
                        $"entered={entered}, surfaceMovement={surfaceMovement}, depthClamped={depthClamped}, " +
                        $"diveRequested={diveRequested}, divesDown={divesDown}, surfaceRequested={surfaceRequested}, " +
                        $"surfacesUp={surfacesUp}, exited={exited}, reentered={reentered}.");
                }
            }
            finally
            {
                if (actorObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(actorObject);
                }

                if (waterObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(waterObject);
                }
            }
        }

        private static void ConfigureZoneContext(ZoneContext context, string zoneId, WorldZoneType zoneType)
        {
            SerializedObject serialized = new SerializedObject(context);
            SetString(serialized, "zoneId", zoneId);
            SetInt(serialized, "zoneType", (int)zoneType);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWaterVolume(
            WaterVolume water,
            ZoneContext context,
            BoxCollider collider,
            float surfaceY,
            float maxDepth)
        {
            SerializedObject serialized = new SerializedObject(water);
            SetObjectReference(serialized, "zoneContext", context);
            SetObjectReference(serialized, "volumeCollider", collider);
            SetFloat(serialized, "surfaceY", surfaceY);
            SetFloat(serialized, "maxDepth", maxDepth);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSwimController(
            SwimController controller,
            Rigidbody body,
            WaterVolume water,
            Transform movementReference)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetObjectReference(serialized, "body", body);
            SetObjectReference(serialized, "waterVolume", water);
            SetObjectReference(serialized, "movementReference", movementReference);
            SetFloat(serialized, "surfaceSwimSpeed", 4f);
            SetFloat(serialized, "underwaterSwimSpeed", 3.25f);
            SetFloat(serialized, "acceleration", 8f);
            SetFloat(serialized, "surfaceDepth", 0.55f);
            SetFloat(serialized, "targetDiveDepth", 3f);
            SetFloat(serialized, "verticalSpeed", 2.5f);
            SetFloat(serialized, "verticalResponsiveness", 3f);
            SetFloat(serialized, "bottomClearance", 0.75f);
            SetFloat(serialized, "waterLinearDamping", 2f);
            SetBool(serialized, "enterWaterOnEnableWhenInside", true);
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

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized bool property '{name}'.");
            property.boolValue = value;
        }
    }
}
