using System;
using System.IO;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.Puzzles;
using BeyondTheBeat.UI;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase3ExitBuilder
    {
        private const string ScenePath = Phase3RestrictedAreaBuilder.Phase3ScenePath;
        private const string CanvasName = "MobileDrivingCanvas";
        private const string HudRootName = "Phase1MissionHUD";
        private const string PanelName = "MissionPanel";
        private const string StepTextName = "Phase3MissionSteps";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string PersistenceRootName = "Phase1Persistence";
        private const string VehicleName = "PrototypeVehicle";
        private const string RestrictedZoneId = "restricted-yard";
        private const string RestrictedPuzzleId = "restricted-pressure-plate";
        private const string WorkflowRelativePath = ".github/workflows/phase2-forest-foundation.yml";
        private const string ValidationRelativePath = "Docs/Validation/PHASE_3_VALIDATION.md";

        [MenuItem("Beyond The Beat/Phase 3/Build Exit Integration")]
        public static void BuildExitIntegration()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 3 exit integration requires scene '{ScenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvasObject = FindRootObject(scene, CanvasName);
            Transform hudRoot = canvasObject != null ? canvasObject.transform.Find(HudRootName) : null;
            MissionHud hud = hudRoot != null ? hudRoot.GetComponent<MissionHud>() : null;
            Transform panel = hudRoot != null ? hudRoot.Find(PanelName) : null;

            if (hud == null || panel == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] Phase 3 exit integration requires the inherited MissionHud/MissionPanel. " +
                    "Build the preceding Phase 1/2/3 milestones first.");
                return;
            }

            ConfigureReachAndSolveStepStrip(hud, panel);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"[Beyond The Beat] Unable to save Phase 3 exit integration into '{ScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Beyond The Beat] Phase 3 exit integration built. Reach + Solve now has an authored two-condition HUD strip, " +
                "and the Android build is constrained to the integrated Phase 3 scene.");
        }

        [MenuItem("Beyond The Beat/Phase 3/Validate Exit Integration")]
        public static void ValidateExitIntegration()
        {
            if (ValidateExitIntegrationInternal(out string message))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static bool ValidateExitIntegrationOrThrow()
        {
            if (ValidateExitIntegrationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateExitIntegrationInternal(out string message)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                message = $"[Beyond The Beat] Phase 3 exit validation FAIL: scene missing at '{ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject canvasObject = FindRootObject(validationScene, CanvasName);
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                GameObject persistenceRoot = FindRootObject(validationScene, PersistenceRootName);

                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                MissionDefinition mission = manager != null ? manager.StartingMission : null;
                Phase1SaveCoordinator coordinator =
                    persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;
                SaveManager saveManager = persistenceRoot != null ? persistenceRoot.GetComponent<SaveManager>() : null;
                MobileDrivingInput mobileInput =
                    canvasObject != null ? canvasObject.GetComponentInChildren<MobileDrivingInput>(true) : null;
                int touchControlCount = canvasObject != null
                    ? canvasObject.GetComponentsInChildren<TouchHoldButton>(true).Length
                    : 0;

                MissionHud hud = canvasObject != null
                    ? canvasObject.GetComponentInChildren<MissionHud>(true)
                    : null;
                PuzzleStateController puzzle = FindPuzzle(validationScene, RestrictedPuzzleId);
                PuzzleGateBinding binding = validationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<PuzzleGateBinding>(true))
                    .FirstOrDefault(item => item.PuzzleState == puzzle);
                RestrictedGateController gate = binding != null ? binding.Gate : null;
                ZoneContext restrictedZone = FindZone(validationScene, RestrictedZoneId);

                bool inheritedWorldPass =
                    vehicle != null &&
                    FindRootObject(validationScene, "Phase2ForestBiome") != null &&
                    FindRootObject(validationScene, "Phase2SurvivalSystem") != null &&
                    FindRootObject(validationScene, "Phase3RestrictedArea") != null;

                bool mobileControlsPass =
                    mobileInput != null &&
                    mobileInput.VehicleBound &&
                    mobileInput.DirectTouchFallbackEnabled &&
                    mobileInput.LegacyTouchFallbackEnabled &&
                    touchControlCount >= 5;

                bool missionPass =
                    manager != null &&
                    mission != null &&
                    mission.IsConfigured &&
                    mission.ObjectiveType == MissionObjectiveType.ReachAndSolve &&
                    string.Equals(mission.TargetZoneId, RestrictedZoneId, StringComparison.Ordinal) &&
                    string.Equals(mission.TargetPuzzleId, RestrictedPuzzleId, StringComparison.Ordinal) &&
                    manager.PlayerActor == vehicle &&
                    manager.ObservedPuzzleCount >= 1;

                bool puzzleGatePass =
                    puzzle != null &&
                    puzzle.IsConfigured &&
                    string.Equals(puzzle.PuzzleId, RestrictedPuzzleId, StringComparison.Ordinal) &&
                    restrictedZone != null &&
                    restrictedZone.ZoneType == WorldZoneType.Restricted &&
                    binding != null &&
                    binding.PuzzleState == puzzle &&
                    binding.Gate == gate &&
                    binding.RelockWhenPuzzleResets &&
                    gate != null &&
                    gate.GateTransform != null;

                bool persistencePass =
                    coordinator != null &&
                    saveManager != null &&
                    coordinator.SaveManager == saveManager &&
                    coordinator.MissionManager == manager &&
                    coordinator.VehicleTransform == (vehicle != null ? vehicle.transform : null) &&
                    coordinator.PersistentZoneCount >= 5 &&
                    coordinator.PersistentPuzzleCount >= 1;

                bool hudStructurePass =
                    hud != null &&
                    hud.MissionManager == manager &&
                    hud.PanelRoot != null &&
                    hud.TitleText != null &&
                    hud.ObjectiveText != null &&
                    hud.StatusText != null &&
                    hud.PhaseStepText != null &&
                    hud.ProgressRoot != null &&
                    hud.ProgressFill != null &&
                    !hud.PhaseStepText.raycastTarget &&
                    hud.PanelRoot.GetComponentsInChildren<Image>(true).All(image => !image.raycastTarget);

                bool hudStatePass = ValidateHudStates(mission);
                bool lifecyclePass = ValidateReachAndSolveLifecycle(manager, mission, vehicle, restrictedZone, puzzle, binding, gate);

                bool buildSettingsPass =
                    EditorBuildSettings.scenes.Length == 1 &&
                    EditorBuildSettings.scenes[0].enabled &&
                    string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);

                bool workflowPass = ValidateSingleApkWorkflowContract();
                bool validationDocPass = ValidatePhysicalGateDocumentation();

                bool allPass =
                    inheritedWorldPass &&
                    mobileControlsPass &&
                    missionPass &&
                    puzzleGatePass &&
                    persistencePass &&
                    hudStructurePass &&
                    hudStatePass &&
                    lifecyclePass &&
                    buildSettingsPass &&
                    workflowPass &&
                    validationDocPass;

                message =
                    "[Beyond The Beat] Phase 3 FINAL exit integration validation\n" +
                    $"Inherited Phase 2 world + Phase 3 restricted area: {PassFail(inheritedWorldPass)}\n" +
                    $"Mobile controls (5 buttons, vehicle bound, dual fallback): {PassFail(mobileControlsPass)}\n" +
                    $"Reach + Solve mission wiring/configuration: {PassFail(missionPass)}\n" +
                    $"Restricted ZoneContext + puzzle/gate binding: {PassFail(puzzleGatePass)}\n" +
                    $"Central persistence zone/puzzle wiring: {PassFail(persistencePass)}\n" +
                    $"Non-blocking authored HUD + Phase 3 step strip: {PassFail(hudStructurePass)}\n" +
                    $"Reach + Solve HUD state matrix: {PassFail(hudStatePass)}\n" +
                    $"Reach + Solve + gate lifecycle/free-roam transition: {PassFail(lifecyclePass)}\n" +
                    $"Single integrated Phase 3 build scene: {PassFail(buildSettingsPass)}\n" +
                    $"Single TEST-THIS-BUILD workflow contract: {PassFail(workflowPass)}\n" +
                    $"Physical Android acceptance checklist documented: {PassFail(validationDocPass)}\n" +
                    "Automated PASS does not equal physical Android sign-off.";

                return allPass;
            }
            finally
            {
                if (openedForValidation && validationScene.IsValid())
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static void ConfigureReachAndSolveStepStrip(MissionHud hud, Transform panel)
        {
            Transform existing = panel.Find(StepTextName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(580f, 238f);
            }

            Text status = panel.Find("MissionStatus")?.GetComponent<Text>();
            if (status != null)
            {
                SetAnchors(status.rectTransform, new Vector2(0.055f, 0.20f), new Vector2(0.95f, 0.33f));
            }

            Transform progressTransform = panel.Find("MissionProgress");
            if (progressTransform != null && progressTransform.TryGetComponent(out RectTransform progressRect))
            {
                SetAnchors(progressRect, new Vector2(0.055f, 0.045f), new Vector2(0.95f, 0.095f));
            }

            GameObject stepObject = new GameObject(
                StepTextName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            stepObject.transform.SetParent(panel, false);

            Text stepText = stepObject.GetComponent<Text>();
            stepText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stepText.fontSize = 14;
            stepText.fontStyle = FontStyle.Bold;
            stepText.alignment = TextAnchor.MiddleLeft;
            stepText.color = MobileUiTheme.Muted;
            stepText.horizontalOverflow = HorizontalWrapMode.Wrap;
            stepText.verticalOverflow = VerticalWrapMode.Truncate;
            stepText.raycastTarget = false;
            SetAnchors(stepText.rectTransform, new Vector2(0.055f, 0.105f), new Vector2(0.95f, 0.19f));

            SerializedObject serialized = new SerializedObject(hud);
            SerializedProperty stepProperty = serialized.FindProperty("phaseStepText");
            if (stepProperty == null)
            {
                throw new InvalidOperationException("MissionHud phaseStepText field could not be resolved for Phase 3 exit integration.");
            }

            stepProperty.objectReferenceValue = stepText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        private static bool ValidateHudStates(MissionDefinition mission)
        {
            if (mission == null || mission.ObjectiveType != MissionObjectiveType.ReachAndSolve)
            {
                return false;
            }

            MissionProgressSnapshot none = CreateReachAndSolveProgress(false, false);
            MissionProgressSnapshot puzzleFirst = CreateReachAndSolveProgress(false, true);
            MissionProgressSnapshot areaFirst = CreateReachAndSolveProgress(true, false);
            MissionProgressSnapshot both = CreateReachAndSolveProgress(true, true);

            MissionHudSnapshot noneSnapshot = MissionHud.CreateSnapshot(mission, MissionState.Active, none);
            MissionHudSnapshot puzzleSnapshot = MissionHud.CreateSnapshot(mission, MissionState.Active, puzzleFirst);
            MissionHudSnapshot areaSnapshot = MissionHud.CreateSnapshot(mission, MissionState.Active, areaFirst);
            MissionHudSnapshot completeSnapshot = MissionHud.CreateSnapshot(mission, MissionState.Completed, both);

            return
                noneSnapshot.Status == "SOLVE PUZZLE • REACH AREA" &&
                puzzleSnapshot.Status == "PUZZLE SOLVED • ENTER AREA" &&
                areaSnapshot.Status == "AREA REACHED • SOLVE PUZZLE" &&
                completeSnapshot.Status.IndexOf("COMPLETE", StringComparison.Ordinal) >= 0 &&
                MissionHud.CreateReachAndSolveStepLabel(none) == "PUZZLE NEXT   |   AREA NEXT" &&
                MissionHud.CreateReachAndSolveStepLabel(puzzleFirst) == "PUZZLE DONE   |   AREA NEXT" &&
                MissionHud.CreateReachAndSolveStepLabel(areaFirst) == "PUZZLE NEXT   |   AREA DONE" &&
                MissionHud.CreateReachAndSolveStepLabel(both) == "PUZZLE DONE   |   AREA DONE" &&
                Mathf.Approximately(none.NormalizedProgress, 0f) &&
                Mathf.Approximately(puzzleFirst.NormalizedProgress, 0.5f) &&
                Mathf.Approximately(areaFirst.NormalizedProgress, 0.5f) &&
                Mathf.Approximately(both.NormalizedProgress, 1f);
        }

        private static bool ValidateReachAndSolveLifecycle(
            MissionManager manager,
            MissionDefinition mission,
            GameObject vehicle,
            ZoneContext restrictedZone,
            PuzzleStateController puzzle,
            PuzzleGateBinding binding,
            RestrictedGateController gate)
        {
            if (manager == null || mission == null || vehicle == null || restrictedZone == null ||
                puzzle == null || binding == null || gate == null)
            {
                return false;
            }

            manager.ClearMission();
            puzzle.ResetPuzzle();

            // Editor/batch-mode validation does not rely on Play Mode lifecycle callbacks. Rebind the
            // same event sources explicitly so this test exercises the production event-driven path.
            manager.RebindPuzzleSources();
            binding.Rebind();
            binding.Synchronize();

            bool startsLocked = gate.IsLocked && !puzzle.IsSolved;
            bool started = manager.StartMission(mission);
            bool targetAccepted = manager.TryProcessZoneEntry(restrictedZone, vehicle);
            bool waitsForPuzzle = manager.State == MissionState.Active &&
                                  manager.Progress.TargetContextActive &&
                                  !manager.Progress.PuzzleSolved;

            puzzle.SetSolved(true);
            bool completesAndUnlocks =
                manager.State == MissionState.Completed &&
                !manager.HasActiveMission &&
                !gate.IsLocked;

            manager.ClearMission();
            puzzle.ResetPuzzle();
            binding.Synchronize();
            bool resetsForFreeRoam = !puzzle.IsSolved && gate.IsLocked && manager.State == MissionState.Inactive;

            Debug.Log(
                "[Beyond The Beat] Phase 3 Reach + Solve lifecycle detail: " +
                $"startsLocked={PassFail(startsLocked)}, " +
                $"started={PassFail(started)}, " +
                $"targetAccepted={PassFail(targetAccepted)}, " +
                $"waitsForPuzzle={PassFail(waitsForPuzzle)}, " +
                $"completesAndUnlocks={PassFail(completesAndUnlocks)}, " +
                $"resetsForFreeRoam={PassFail(resetsForFreeRoam)}, " +
                $"finalMissionState={manager.State}, finalPuzzleSolved={puzzle.IsSolved}, finalGateLocked={gate.IsLocked}.");

            return startsLocked && started && targetAccepted && waitsForPuzzle && completesAndUnlocks && resetsForFreeRoam;
        }

        private static bool ValidateSingleApkWorkflowContract()
        {
            string projectRoot = GetProjectRoot();
            string path = Path.Combine(projectRoot, WorkflowRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                return false;
            }

            string workflow = File.ReadAllText(path);
            return workflow.Contains("name: Current Android Test Build") &&
                   workflow.Contains("buildMethod: BeyondTheBeat.Editor.Phase3BuildAutomation.BuildAndroid") &&
                   workflow.Contains("TEST-THIS-BUILD-${GITHUB_RUN_NUMBER}") &&
                   workflow.Contains("This is the ONLY APK intended for current device testing.");
        }

        private static bool ValidatePhysicalGateDocumentation()
        {
            string projectRoot = GetProjectRoot();
            string path = Path.Combine(projectRoot, ValidationRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                return false;
            }

            string document = File.ReadAllText(path);
            return document.Contains("LEFT / RIGHT / GO / REV / ACTION") &&
                   document.Contains("crate / pressure plate") &&
                   document.Contains("persistence after relaunch") &&
                   document.Contains("CI GREEN IS NOT DEVICE SIGN-OFF");
        }

        private static MissionProgressSnapshot CreateReachAndSolveProgress(bool targetContextActive, bool puzzleSolved)
        {
            return new MissionProgressSnapshot(
                MissionObjectiveType.ReachAndSolve,
                targetContextActive,
                0f,
                0f,
                puzzleSolved);
        }

        private static PuzzleStateController FindPuzzle(Scene scene, string puzzleId)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleStateController>(true))
                .FirstOrDefault(item => string.Equals(item.PuzzleId, puzzleId, StringComparison.Ordinal));
        }

        private static ZoneContext FindZone(Scene scene, string zoneId)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .FirstOrDefault(item => string.Equals(item.ZoneId, zoneId, StringComparison.Ordinal));
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static string GetProjectRoot()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root for Phase 3 exit validation.");
            }

            return projectRoot;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
