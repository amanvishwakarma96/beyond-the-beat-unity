using System;
using System.Collections.Generic;
using System.IO;
using BeyondTheBeat.Economy;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.Jobs;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase4ExitBuilder
    {
        private const string ScenePath = Phase4CookingBuilder.Phase4ScenePath;
        private const string ActivitiesRootName = "Phase4FreeRoamActivities";
        private const string ParkingRootName = "ParkingPrototype";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string RestrictedRootName = "Phase3RestrictedArea";
        private const string MechanicSystemRootName = "Phase4MechanicJobSystem";
        private const string CanvasName = "MobileDrivingCanvas";
        private const string MechanicHudName = "Phase4MechanicJobHUD";
        private const string VehicleName = "PrototypeVehicle";
        private const string CookingStationName = "CookingStation";
        private const string RepairBayName = "VehicleRepairBay";
        private const string ValidationDocPath = "Docs/Validation/PHASE_4_VALIDATION.md";
        private const string CurrentWorkflowPath = ".github/workflows/phase2-forest-foundation.yml";

        [MenuItem("Beyond The Beat/Phase 4/Validate Final Exit Integration")]
        public static void ValidateFinalExitIntegration()
        {
            if (!ValidateFinalExitIntegrationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateFinalExitIntegrationOrThrow()
        {
            // Keep the final gate composed from the already-proven milestone validators.
            Phase4CookingBuilder.ValidateCookingInteractionOrThrow();
            Phase4RepairBuilder.ValidateVehicleRepairInteractionOrThrow();
            Phase4MechanicJobBuilder.ValidateMechanicJobOrThrow();

            if (ValidateFinalExitIntegrationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateFinalExitIntegrationInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                message = $"[Beyond The Beat] Phase 4 FINAL exit validation FAIL: scene not found at '{ScenePath}'.";
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
                GameObject parkingRoot = FindRootObject(validationScene, ParkingRootName);
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                GameObject restrictedRoot = FindRootObject(validationScene, RestrictedRootName);
                GameObject mechanicSystemRoot = FindRootObject(validationScene, MechanicSystemRootName);
                GameObject canvas = FindRootObject(validationScene, CanvasName);
                GameObject vehicle = FindRootObject(validationScene, VehicleName);

                Transform cookingTransform = activitiesRoot != null ? activitiesRoot.transform.Find(CookingStationName) : null;
                Transform repairTransform = activitiesRoot != null ? activitiesRoot.transform.Find(RepairBayName) : null;
                Transform mechanicHudTransform = canvas != null ? canvas.transform.Find(MechanicHudName) : null;

                ParkingZone parking = parkingRoot != null ? parkingRoot.GetComponentInChildren<ParkingZone>(true) : null;
                CookingStation cooking = cookingTransform != null ? cookingTransform.GetComponent<CookingStation>() : null;
                RepairStation repair = repairTransform != null ? repairTransform.GetComponent<RepairStation>() : null;
                MechanicJobManager mechanicManager = mechanicSystemRoot != null ? mechanicSystemRoot.GetComponent<MechanicJobManager>() : null;
                CreditWallet wallet = mechanicSystemRoot != null ? mechanicSystemRoot.GetComponent<CreditWallet>() : null;
                MechanicJobHud mechanicHud = mechanicHudTransform != null ? mechanicHudTransform.GetComponent<MechanicJobHud>() : null;

                InteractionTrigger parkingTrigger = parking != null ? parking.GetComponent<InteractionTrigger>() : null;
                InteractionTrigger cookingTrigger = cooking != null ? cooking.GetComponent<InteractionTrigger>() : null;
                InteractionTrigger repairTrigger = repair != null ? repair.GetComponent<InteractionTrigger>() : null;

                List<InteractionController> controllers = GetSceneComponents<InteractionController>(validationScene);
                List<MobileDrivingInput> mobileInputs = GetSceneComponents<MobileDrivingInput>(validationScene);
                List<EventSystem> eventSystems = GetSceneComponents<EventSystem>(validationScene);

                InteractionController controller = controllers.Count == 1 ? controllers[0] : null;
                MobileDrivingInput mobileInput = mobileInputs.Count == 1 ? mobileInputs[0] : null;

                bool activityStructurePass =
                    parking != null &&
                    cooking != null &&
                    repair != null &&
                    mechanicManager != null &&
                    wallet != null &&
                    mechanicHud != null &&
                    parking is InteractableObject &&
                    cooking is TimedActivityInteractable &&
                    repair is TimedActivityInteractable;

                bool sharedTriggerPass =
                    IsTriggerWired(parkingTrigger, parking) &&
                    IsTriggerWired(cookingTrigger, cooking) &&
                    IsTriggerWired(repairTrigger, repair) &&
                    mechanicManager.RepairStation == repair &&
                    mechanicManager.Target == repair.Target &&
                    mechanicManager.Wallet == wallet;

                bool singleInputPathPass =
                    controller != null &&
                    mobileInput != null &&
                    eventSystems.Count == 1 &&
                    vehicle != null &&
                    mobileInput.VehicleBound &&
                    mobileInput.DirectTouchFallbackEnabled &&
                    mobileInput.LegacyTouchFallbackEnabled &&
                    ValidateInteractionControllerWiring(controller, mobileInput, vehicle) &&
                    ValidateMobileControlReferences(mobileInput);

                bool hudPass = ValidateMechanicHud(mechanicHud, mechanicManager, wallet);

                bool inheritedWorldPass =
                    activitiesRoot != null &&
                    parkingRoot != null &&
                    missionRoot != null &&
                    restrictedRoot != null &&
                    vehicle != null &&
                    canvas != null;

                bool buildSettingsPass =
                    EditorBuildSettings.scenes.Length == 1 &&
                    EditorBuildSettings.scenes[0].enabled &&
                    string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);

                bool workflowPass = ValidateSingleCurrentAndroidWorkflowContract(out string workflowDetail);
                bool validationDocPass = ValidatePhase4ValidationDocument(out string documentDetail);

                bool pass =
                    activityStructurePass &&
                    sharedTriggerPass &&
                    singleInputPathPass &&
                    hudPass &&
                    inheritedWorldPass &&
                    buildSettingsPass &&
                    workflowPass &&
                    validationDocPass;

                message = pass
                    ? "[Beyond The Beat] Phase 4 FINAL exit validation PASS: Parking, Cook, Repair and Mechanic Job share the existing interaction/input foundation; economy/HUD, inherited mission/puzzle world, single-scene build, single current Android artifact contract and validation documentation are intact. Physical Android sign-off remains required."
                    : "[Beyond The Beat] Phase 4 FINAL exit validation FAIL: " +
                      $"activities={activityStructurePass}, sharedTriggers={sharedTriggerPass}, singleInput={singleInputPathPass}, " +
                      $"hud={hudPass}, inheritedWorld={inheritedWorldPass}, buildSettings={buildSettingsPass}, " +
                      $"workflow={workflowPass} ({workflowDetail}), validationDoc={validationDocPass} ({documentDetail}).";
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

        private static bool IsTriggerWired(InteractionTrigger trigger, InteractableObject expected)
        {
            if (trigger == null || expected == null)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(trigger);
            SerializedProperty interactable = serialized.FindProperty("interactable");
            return interactable != null && interactable.objectReferenceValue == expected;
        }

        private static bool ValidateInteractionControllerWiring(
            InteractionController controller,
            MobileDrivingInput input,
            GameObject vehicle)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty inputSource = serialized.FindProperty("inputSource");
            SerializedProperty actor = serialized.FindProperty("actor");
            return inputSource != null &&
                   actor != null &&
                   inputSource.objectReferenceValue == input &&
                   actor.objectReferenceValue == vehicle;
        }

        private static bool ValidateMobileControlReferences(MobileDrivingInput input)
        {
            if (input == null)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(input);
            string[] names =
            {
                "steerLeftButton",
                "steerRightButton",
                "accelerateButton",
                "brakeReverseButton",
                "interactButton"
            };

            for (int i = 0; i < names.Length; i++)
            {
                SerializedProperty property = serialized.FindProperty(names[i]);
                if (property == null || property.objectReferenceValue == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateMechanicHud(
            MechanicJobHud hud,
            MechanicJobManager manager,
            CreditWallet wallet)
        {
            if (hud == null || manager == null || wallet == null ||
                hud.JobManager != manager || hud.Wallet != wallet ||
                hud.PanelRoot == null || hud.JobText == null || hud.CreditsText == null ||
                hud.JobText.raycastTarget || hud.CreditsText.raycastTarget)
            {
                return false;
            }

            Image[] images = hud.PanelRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].raycastTarget)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateSingleCurrentAndroidWorkflowContract(out string detail)
        {
            string path = GetProjectPath(CurrentWorkflowPath);
            if (!File.Exists(path))
            {
                detail = "workflow-missing";
                return false;
            }

            string text = File.ReadAllText(path);
            string[] lines = text.Replace("\r", string.Empty).Split('\n');
            int buildMethodCount = 0;
            string buildMethodLine = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (!trimmed.StartsWith("buildMethod:", StringComparison.Ordinal))
                {
                    continue;
                }

                buildMethodCount++;
                buildMethodLine = trimmed;
            }

            bool buildMethodValid =
                buildMethodCount == 1 &&
                buildMethodLine.Contains("BeyondTheBeat.Editor.Phase", StringComparison.Ordinal) &&
                buildMethodLine.EndsWith("BuildAutomation.BuildAndroid", StringComparison.Ordinal);

            bool pass =
                text.Contains("name: Current Android Test Build", StringComparison.Ordinal) &&
                buildMethodValid &&
                text.Contains("TEST-THIS-BUILD-${GITHUB_RUN_NUMBER}", StringComparison.Ordinal) &&
                text.Contains("This is the ONLY APK intended for current device testing.", StringComparison.Ordinal);

            detail = $"buildMethods={buildMethodCount}, active='{buildMethodLine}'";
            return pass;
        }

        private static bool ValidatePhase4ValidationDocument(out string detail)
        {
            string path = GetProjectPath(ValidationDocPath);
            if (!File.Exists(path))
            {
                detail = "document-missing";
                return false;
            }

            string text = File.ReadAllText(path);
            bool pass =
                text.Contains("# Phase 4 Validation", StringComparison.Ordinal) &&
                text.Contains("Parking", StringComparison.Ordinal) &&
                text.Contains("Cook", StringComparison.Ordinal) &&
                text.Contains("Repair", StringComparison.Ordinal) &&
                text.Contains("Mechanic Job", StringComparison.Ordinal) &&
                text.Contains("PENDING DEVICE VALIDATION", StringComparison.Ordinal) &&
                text.Contains("CI GREEN IS NOT DEVICE SIGN-OFF", StringComparison.Ordinal);

            detail = pass ? "required-sections-present" : "required-section-missing";
            return pass;
        }

        private static string GetProjectPath(string relativePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return relativePath;
            }

            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components;
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
    }
}
