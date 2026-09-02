using System;
using System.Reflection;
using BeyondTheBeat.Water;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase5SwimBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string PrototypeRootName = "Phase5SwimPrototype";
        private const string PrototypeActorName = "SwimPrototypeActor";
        private const string ValidationDocPath = "Docs/Validation/PHASE_5_SWIM_FOUNDATION.md";

        [MenuItem("Beyond The Beat/Phase 5/Build Swim Dive Foundation")]
        public static void BuildSwimDiveFoundation()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Phase 5 swim build requires the ocean foundation scene at '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WaterVolume waterVolume = FindUniqueWaterVolume(scene);
            if (waterVolume == null)
            {
                throw new InvalidOperationException("Phase 5 swim build requires exactly one configured WaterVolume.");
            }

            RemoveRoot(scene, PrototypeRootName);

            GameObject root = new GameObject(PrototypeRootName);
            SwimController controller = CreatePrototypeActor(root.transform, waterVolume);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 5 swim integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = controller.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 5 swim/dive controller foundation created. The prototype is intentionally controller-only: no exploration mission, oxygen, extra camera, or parallel mobile UI/input stack is introduced in this milestone.");
        }

        [MenuItem("Beyond The Beat/Phase 5/Validate Swim Dive Foundation")]
        public static void ValidateSwimDiveFoundation()
        {
            if (!ValidateSwimDiveFoundationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateSwimDiveFoundationOrThrow()
        {
            Phase5OceanBuilder.ValidateOceanFoundationOrThrow();

            if (ValidateSwimDiveFoundationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateSwimDiveFoundationInternal(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = $"[Beyond The Beat] Phase 5 swim validation FAIL: scene not found at '{ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                WaterVolume waterVolume = FindUniqueWaterVolume(validationScene);
                GameObject root = FindRootObject(validationScene, PrototypeRootName);
                Transform actorTransform = root != null ? root.transform.Find(PrototypeActorName) : null;
                SwimController controller = actorTransform != null ? actorTransform.GetComponent<SwimController>() : null;
                Rigidbody body = actorTransform != null ? actorTransform.GetComponent<Rigidbody>() : null;
                CapsuleCollider capsule = actorTransform != null ? actorTransform.GetComponent<CapsuleCollider>() : null;

                bool structurePass =
                    waterVolume != null &&
                    root != null &&
                    actorTransform != null &&
                    controller != null &&
                    body != null &&
                    capsule != null &&
                    controller.Body == body &&
                    controller.WaterVolume == waterVolume &&
                    controller.MovementReference == actorTransform &&
                    waterVolume.ContainsPoint(actorTransform.position) &&
                    (body.constraints & RigidbodyConstraints.FreezeRotation) == RigidbodyConstraints.FreezeRotation;

                bool architecturePass =
                    typeof(SwimController).GetMethod(
                        "Update",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null &&
                    typeof(SwimController).GetMethod(
                        "FixedUpdate",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null &&
                    root != null &&
                    root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                    root.GetComponentsInChildren<Light>(true).Length == 0;

                bool behaviorPass = ValidateControllerBehavior(out string behaviorDetail);

                bool inheritedPass =
                    FindRootObject(validationScene, "Phase5OceanArea") != null &&
                    FindRootObject(validationScene, "Phase4FreeRoamActivities") != null &&
                    FindRootObject(validationScene, "ParkingPrototype") != null &&
                    FindRootObject(validationScene, "Phase1MissionSystem") != null &&
                    FindRootObject(validationScene, "Phase3RestrictedArea") != null &&
                    FindRootObject(validationScene, "Phase4MechanicJobSystem") != null &&
                    FindRootObject(validationScene, "MobileDrivingCanvas") != null;

                bool buildSettingsPass =
                    EditorBuildSettings.scenes.Length == 1 &&
                    EditorBuildSettings.scenes[0].enabled &&
                    string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);

                bool validationDocPass = AssetDatabase.LoadAssetAtPath<TextAsset>(ValidationDocPath) != null ||
                                         System.IO.File.Exists(ValidationDocPath);

                bool pass = structurePass && architecturePass && behaviorPass && inheritedPass &&
                            buildSettingsPass && validationDocPass;

                message = pass
                    ? "[Beyond The Beat] Phase 5 swim/dive foundation validation PASS: WaterVolume-bound Rigidbody locomotion, Dry/Surface/Underwater transitions, bounded dive depth, gravity restore, no Update-loop controller polling, inherited gameplay roots, single-scene build contract and validation documentation are intact. Physical mobile control/camera integration remains a later milestone."
                    : "[Beyond The Beat] Phase 5 swim/dive foundation validation FAIL: " +
                      $"structure={structurePass}, architecture={architecturePass}, behavior={behaviorPass} ({behaviorDetail}), " +
                      $"inherited={inheritedPass}, buildSettings={buildSettingsPass}, validationDoc={validationDocPass}.";
                return pass;
            }
            finally
            {
                if (openedForValidation && validationScene.IsValid() && validationScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static bool ValidateControllerBehavior(out string detail)
        {
            GameObject waterObject = null;
            GameObject actorObject = null;

            try
            {
                waterObject = new GameObject("SwimValidationWater");
                BoxCollider waterCollider = waterObject.AddComponent<BoxCollider>();
                waterCollider.isTrigger = true;
                waterCollider.size = new Vector3(20f, 8f, 20f);
                waterCollider.center = new Vector3(0f, -4f, 0f);

                ZoneContext context = waterObject.AddComponent<ZoneContext>();
                ConfigureZoneContext(context, "swim-validation-ocean", WorldZoneType.Ocean);

                WaterVolume water = waterObject.AddComponent<WaterVolume>();
                ConfigureWaterVolume(water, context, waterCollider, 0f, 8f);

                actorObject = new GameObject("SwimValidationActor");
                actorObject.transform.position = new Vector3(0f, -0.55f, 0f);
                Rigidbody body = actorObject.AddComponent<Rigidbody>();
                body.useGravity = true;
                body.linearDamping = 0.15f;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                SwimController controller = actorObject.AddComponent<SwimController>();
                ConfigureSwimController(controller, body, water, actorObject.transform);

                bool entered = controller.BindWaterVolume(water, true) &&
                               controller.IsInWater &&
                               controller.State == AquaticState.Surface &&
                               !body.useGravity;

                controller.SetMoveInput(Vector2.up);
                Vector3 forwardVelocity = controller.GetTargetVelocity(actorObject.transform.position);
                bool surfaceMovement = forwardVelocity.z > 0.1f &&
                                       Mathf.Abs(forwardVelocity.x) < 0.01f;

                float clampedDiveDepth = controller.SetTargetDiveDepth(100f);
                bool depthClamped = clampedDiveDepth <= water.MaxDepth &&
                                    Mathf.Approximately(clampedDiveDepth, controller.MaxAllowedDiveDepth);

                bool diveRequested = controller.SetDiveRequested(true) &&
                                     controller.State == AquaticState.Underwater;
                Vector3 diveVelocity = controller.GetTargetVelocity(new Vector3(0f, -0.55f, 0f));
                bool divesDown = diveVelocity.y < -0.01f;

                bool surfaceRequested = controller.SetDiveRequested(false) &&
                                        controller.State == AquaticState.Surface;
                Vector3 surfaceVelocity = controller.GetTargetVelocity(new Vector3(0f, -4f, 0f));
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

                bool pass = entered && surfaceMovement && depthClamped && diveRequested && divesDown &&
                            surfaceRequested && surfacesUp && exited && reentered;

                detail =
                    $"entered={entered}, surfaceMovement={surfaceMovement}, depthClamped={depthClamped}, " +
                    $"diveRequested={diveRequested}, divesDown={divesDown}, surfaceRequested={surfaceRequested}, " +
                    $"surfacesUp={surfacesUp}, exited={exited}, reentered={reentered}";
                return pass;
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

        private static SwimController CreatePrototypeActor(Transform parent, WaterVolume waterVolume)
        {
            GameObject actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            actor.name = PrototypeActorName;
            actor.transform.SetParent(parent, false);

            Bounds waterBounds = waterVolume.VolumeCollider.bounds;
            actor.transform.position = new Vector3(
                waterBounds.center.x,
                waterVolume.SurfaceY - 0.55f,
                waterBounds.center.z);
            actor.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);

            Rigidbody body = actor.AddComponent<Rigidbody>();
            body.mass = 75f;
            body.useGravity = true;
            body.linearDamping = 0.15f;
            body.angularDamping = 4f;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            SwimController controller = actor.AddComponent<SwimController>();
            ConfigureSwimController(controller, body, waterVolume, actor.transform);
            return controller;
        }

        private static void ConfigureSwimController(
            SwimController controller,
            Rigidbody body,
            WaterVolume waterVolume,
            Transform movementReference)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetObjectReference(serialized, "body", body);
            SetObjectReference(serialized, "waterVolume", waterVolume);
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
            EditorUtility.SetDirty(controller);
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

        private static WaterVolume FindUniqueWaterVolume(Scene scene)
        {
            WaterVolume result = null;
            int count = 0;
            WaterVolume[] volumes = UnityEngine.Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None);
            for (int i = 0; i < volumes.Length; i++)
            {
                WaterVolume candidate = volumes[i];
                if (candidate == null || candidate.gameObject.scene != scene)
                {
                    continue;
                }

                count++;
                result = candidate;
            }

            return count == 1 ? result : null;
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject existing = FindRootObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
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
