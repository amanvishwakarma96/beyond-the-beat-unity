using System;
using BeyondTheBeat.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase4RepairBuilder
    {
        private const string ScenePath = Phase4CookingBuilder.Phase4ScenePath;
        private const string ActivitiesRootName = "Phase4FreeRoamActivities";
        private const string RepairBayName = "VehicleRepairBay";
        private const string VehicleName = "PrototypeVehicle";
        private const string CookingStationName = "CookingStation";
        private const string VehicleRepairableId = "prototype-vehicle";

        [MenuItem("Beyond The Beat/Phase 4/Build Vehicle Repair Interaction")]
        public static void BuildVehicleRepairInteraction()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException(
                    $"Phase 4 repair build requires the cooking milestone scene at '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject activitiesRoot = FindRootObject(scene, ActivitiesRootName);
            GameObject vehicle = FindRootObject(scene, VehicleName);
            if (activitiesRoot == null || vehicle == null)
            {
                throw new InvalidOperationException(
                    "Phase 4 repair build requires the inherited Phase4FreeRoamActivities root and PrototypeVehicle.");
            }

            Transform existing = activitiesRoot.transform.Find(RepairBayName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            RepairableState repairable = vehicle.GetComponent<RepairableState>() ?? vehicle.AddComponent<RepairableState>();
            ConfigureRepairable(repairable, VehicleRepairableId, 0.65f);
            RepairStation station = CreateRepairBay(activitiesRoot.transform, repairable);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save Phase 4 vehicle repair integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = station.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log(
                "[Beyond The Beat] Phase 4 vehicle repair interaction created. Vehicle condition is isolated in RepairableState and the repair bay reuses the shared timed ACTION interaction path.");
        }

        [MenuItem("Beyond The Beat/Phase 4/Validate Vehicle Repair Interaction")]
        public static void ValidateVehicleRepairInteraction()
        {
            if (!ValidateVehicleRepairInteractionInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateVehicleRepairInteractionOrThrow()
        {
            if (ValidateVehicleRepairInteractionInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateVehicleRepairInteractionInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                message = $"[Beyond The Beat] Phase 4 repair validation FAIL: scene not found at '{ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject activitiesRoot = FindRootObject(validationScene, ActivitiesRootName);
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                Transform repairBay = activitiesRoot != null ? activitiesRoot.transform.Find(RepairBayName) : null;
                Transform cookingTransform = activitiesRoot != null ? activitiesRoot.transform.Find(CookingStationName) : null;

                RepairableState repairable = vehicle != null ? vehicle.GetComponent<RepairableState>() : null;
                RepairStation station = repairBay != null ? repairBay.GetComponent<RepairStation>() : null;
                InteractionTrigger trigger = repairBay != null ? repairBay.GetComponent<InteractionTrigger>() : null;
                BoxCollider triggerCollider = repairBay != null ? repairBay.GetComponent<BoxCollider>() : null;
                CookingStation cooking = cookingTransform != null ? cookingTransform.GetComponent<CookingStation>() : null;

                bool structurePass =
                    activitiesRoot != null &&
                    vehicle != null &&
                    repairable != null &&
                    string.Equals(repairable.RepairableId, VehicleRepairableId, StringComparison.Ordinal) &&
                    repairable.NeedsRepair &&
                    station != null &&
                    station is TimedActivityInteractable &&
                    station is InteractableObject &&
                    station.Target == repairable &&
                    string.Equals(station.PromptText, "REPAIR", StringComparison.Ordinal) &&
                    trigger != null &&
                    triggerCollider != null &&
                    triggerCollider.isTrigger;

                bool triggerWiringPass = false;
                if (trigger != null && station != null)
                {
                    SerializedObject serializedTrigger = new SerializedObject(trigger);
                    SerializedProperty interactableProperty = serializedTrigger.FindProperty("interactable");
                    triggerWiringPass = interactableProperty != null && interactableProperty.objectReferenceValue == station;
                }

                bool inheritedPass =
                    cooking != null &&
                    FindRootObject(validationScene, "ParkingPrototype") != null &&
                    FindRootObject(validationScene, "Phase1MissionSystem") != null &&
                    FindRootObject(validationScene, "Phase3RestrictedArea") != null;

                bool behaviorPass = ValidateRepairBehavior(out string behaviorDetail);
                bool pass = structurePass && triggerWiringPass && inheritedPass && behaviorPass;

                message = pass
                    ? "[Beyond The Beat] Phase 4 vehicle repair validation PASS: repairable state, ACTION/timed repair, cancel/no-change, full-repair rejection, re-damage/repeat, Cook, parking, mission and restricted-area regressions are intact. Physical Android validation remains required."
                    : "[Beyond The Beat] Phase 4 vehicle repair validation FAIL: " +
                      $"structure={structurePass}, triggerWiring={triggerWiringPass}, inherited={inheritedPass}, behavior={behaviorPass} ({behaviorDetail}).";
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

        private static bool ValidateRepairBehavior(out string detail)
        {
            GameObject targetObject = null;
            GameObject stationObject = null;
            GameObject actor = null;

            try
            {
                targetObject = new GameObject("Phase4RepairValidationTarget");
                RepairableState target = targetObject.AddComponent<RepairableState>();
                ConfigureRepairable(target, "validation-target", 0.6f);

                stationObject = new GameObject("Phase4RepairValidationStation");
                BoxCollider collider = stationObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                RepairStation station = stationObject.AddComponent<RepairStation>();
                ConfigureRepairStation(station, target);

                actor = new GameObject("Phase4RepairValidationActor");

                int initialRepairs = station.RepairsCompleted;
                float initialDamage = target.Damage01;
                bool damagedEligible = station.IsEligible(actor);
                bool started = station.RequestInteraction(actor);
                station.AdvanceActivity(station.DurationSeconds * 0.4f);
                bool progressed = station.IsInteracting && station.Progress01 > 0f && station.Progress01 < 1f;
                bool cancelled = station.CancelInteraction(actor) &&
                                 !station.IsInteracting &&
                                 Mathf.Approximately(station.Progress01, 0f) &&
                                 Mathf.Approximately(target.Damage01, initialDamage) &&
                                 station.RepairsCompleted == initialRepairs;

                bool restarted = station.RequestInteraction(actor);
                bool completed = station.AdvanceActivity(station.DurationSeconds + 0.1f) &&
                                 !target.NeedsRepair &&
                                 Mathf.Approximately(target.Damage01, 0f) &&
                                 station.RepairsCompleted == initialRepairs + 1;

                int afterCompletion = station.RepairsCompleted;
                station.AdvanceActivity(station.DurationSeconds + 1f);
                bool completesOnce = station.RepairsCompleted == afterCompletion;
                bool fullyRepairedRejected = !station.RequestInteraction(actor);

                bool damagedAgain = target.SetDamage01(0.35f) && target.NeedsRepair;
                bool repeatStarted = station.RequestInteraction(actor);
                bool repeatCompleted = station.AdvanceActivity(station.DurationSeconds + 0.1f) &&
                                       !target.NeedsRepair &&
                                       station.RepairsCompleted == initialRepairs + 2;

                bool pass = damagedEligible && started && progressed && cancelled && restarted && completed &&
                            completesOnce && fullyRepairedRejected && damagedAgain && repeatStarted && repeatCompleted;
                detail =
                    $"damagedEligible={damagedEligible}, started={started}, progressed={progressed}, cancelled={cancelled}, " +
                    $"restarted={restarted}, completed={completed}, completesOnce={completesOnce}, " +
                    $"fullyRepairedRejected={fullyRepairedRejected}, damagedAgain={damagedAgain}, " +
                    $"repeatStarted={repeatStarted}, repeatCompleted={repeatCompleted}";
                return pass;
            }
            finally
            {
                if (actor != null)
                {
                    UnityEngine.Object.DestroyImmediate(actor);
                }

                if (stationObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(stationObject);
                }

                if (targetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(targetObject);
                }
            }
        }

        private static RepairStation CreateRepairBay(Transform parent, RepairableState target)
        {
            GameObject stationObject = new GameObject(RepairBayName);
            stationObject.transform.SetParent(parent, false);
            stationObject.transform.position = new Vector3(24f, 0.75f, -16f);

            BoxCollider triggerCollider = stationObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(10f, 3.5f, 11f);

            RepairStation station = stationObject.AddComponent<RepairStation>();
            ConfigureRepairStation(station, target);

            InteractionTrigger trigger = stationObject.GetComponent<InteractionTrigger>();
            if (trigger == null)
            {
                throw new InvalidOperationException("RepairStation did not receive its required InteractionTrigger component.");
            }

            SerializedObject serializedTrigger = new SerializedObject(trigger);
            SerializedProperty interactableProperty = serializedTrigger.FindProperty("interactable");
            if (interactableProperty == null)
            {
                throw new InvalidOperationException("Unable to configure VehicleRepairBay InteractionTrigger.interactable reference.");
            }

            interactableProperty.objectReferenceValue = station;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

            CreateVisualPrimitive("ServicePad", PrimitiveType.Cube, stationObject.transform, new Vector3(0f, -0.62f, 0f), new Vector3(8f, 0.25f, 9f));
            CreateVisualPrimitive("LiftLeft", PrimitiveType.Cube, stationObject.transform, new Vector3(-2.1f, -0.35f, 0f), new Vector3(0.45f, 0.3f, 7f));
            CreateVisualPrimitive("LiftRight", PrimitiveType.Cube, stationObject.transform, new Vector3(2.1f, -0.35f, 0f), new Vector3(0.45f, 0.3f, 7f));
            CreateVisualPrimitive("ToolCabinet", PrimitiveType.Cube, stationObject.transform, new Vector3(4f, 0.3f, 2.8f), new Vector3(1.1f, 1.8f, 1.2f));

            return station;
        }

        private static void ConfigureRepairable(RepairableState target, string id, float damage01)
        {
            SerializedObject serialized = new SerializedObject(target);
            SetString(serialized, "repairableId", id);
            SetFloat(serialized, "damage01", Mathf.Clamp01(damage01));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureRepairStation(RepairStation station, RepairableState target)
        {
            SerializedObject serialized = new SerializedObject(station);
            SetString(serialized, "promptText", "REPAIR");
            SetBool(serialized, "allowRepeatInteraction", true);
            SetFloat(serialized, "durationSeconds", 3f);
            SetBool(serialized, "resetProgressOnCancel", true);
            SetObjectReference(serialized, "target", target);
            SetString(serialized, "activityLabel", "Repair vehicle");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(station);
        }

        private static void CreateVisualPrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
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

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized string property '{propertyName}'.");
            }

            property.stringValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized bool property '{propertyName}'.");
            }

            property.boolValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized float property '{propertyName}'.");
            }

            property.floatValue = value;
        }

        private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Unable to find serialized object property '{propertyName}'.");
            }

            property.objectReferenceValue = value;
        }
    }
}
