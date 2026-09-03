using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeyondTheBeat.CameraSystem;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.UI;
using BeyondTheBeat.Water;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase5ExitBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string ValidationDocPath = "Docs/Validation/PHASE_5_VALIDATION.md";
        private const string FullWorkflowPath = ".github/workflows/phase2-forest-foundation.yml";
        private const string FastWorkflowPath = ".github/workflows/fast-current-milestone-validation.yml";

        private static readonly string[] ExplorationIds =
        {
            "ocean-cove",
            "ocean-reef",
            "ocean-wreck"
        };

        [MenuItem("Beyond The Beat/Phase 5/Validate Final Exit Integration")]
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
            // Compose the already-proven milestone validators first. This keeps the final gate
            // an integration boundary rather than a second implementation of the same gameplay rules.
            Phase5OceanBuilder.ValidateOceanFoundationOrThrow();
            Phase5SwimBuilder.ValidateSwimDiveFoundationOrThrow();
            Phase5MobileSwimBuilder.ValidateMobileSwimCameraIntegrationOrThrow();
            Phase5ExplorationMissionBuilder.ValidateExplorationMissionOrThrow();

            if (ValidateFinalExitIntegrationInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        public static bool ValidateRepositoryContractsOrThrow()
        {
            bool workflowPass = ValidateWorkflowContracts(out string workflowDetail);
            bool documentationPass = ValidatePhysicalGateDocumentation(out string documentationDetail);
            if (workflowPass && documentationPass)
            {
                return true;
            }

            throw new InvalidOperationException(
                "Phase 5 repository exit contract failed: " +
                $"workflow={workflowPass} ({workflowDetail}), documentation={documentationPass} ({documentationDetail}).");
        }

        private static bool ValidateFinalExitIntegrationInternal(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = $"[Beyond The Beat] Phase 5 FINAL exit validation FAIL: scene missing at '{ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene scene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject canvas = FindRoot(scene, "MobileDrivingCanvas");
                GameObject cameraObject = FindRoot(scene, "GameplayCamera");
                GameObject vehicle = FindRoot(scene, "PrototypeVehicle");
                GameObject swimRoot = FindRoot(scene, "Phase5SwimPrototype");
                Transform swimmer = swimRoot != null ? swimRoot.transform.Find("SwimPrototypeActor") : null;
                GameObject missionRoot = FindRoot(scene, "Phase1MissionSystem");
                GameObject persistenceRoot = FindRoot(scene, "Phase1Persistence");
                GameObject explorationRoot = FindRoot(scene, "Phase5ExplorationCheckpoints");

                AquaticModeCoordinator aquatic = canvas != null ? canvas.GetComponent<AquaticModeCoordinator>() : null;
                MobileDrivingInput drivingInput = canvas != null ? canvas.GetComponent<MobileDrivingInput>() : null;
                Transform swimControls = canvas != null ? canvas.transform.Find("SwimControls") : null;
                MobileSwimInput swimInput = swimControls != null ? swimControls.GetComponent<MobileSwimInput>() : null;
                CameraFollow cameraFollow = cameraObject != null ? cameraObject.GetComponent<CameraFollow>() : null;
                SwimController swimController = swimmer != null ? swimmer.GetComponent<SwimController>() : null;
                MissionManager missionManager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                Phase1SaveCoordinator saveCoordinator = persistenceRoot != null
                    ? persistenceRoot.GetComponent<Phase1SaveCoordinator>()
                    : null;
                MissionHud missionHud = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MissionHud>(true))
                    .FirstOrDefault();

                ZoneContext[] explorationZones = explorationRoot != null
                    ? explorationRoot.GetComponentsInChildren<ZoneContext>(true)
                    : Array.Empty<ZoneContext>();
                WaterVolume[] waters = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<WaterVolume>(true))
                    .ToArray();

                bool structurePass =
                    canvas != null && cameraObject != null && vehicle != null && swimmer != null &&
                    missionRoot != null && persistenceRoot != null && explorationRoot != null &&
                    aquatic != null && drivingInput != null && swimInput != null && cameraFollow != null &&
                    swimController != null && missionManager != null && saveCoordinator != null && missionHud != null;

                bool waterPass =
                    waters.Length == 1 &&
                    waters[0] != null &&
                    waters[0].IsConfigured &&
                    waters[0].ZoneContext != null &&
                    waters[0].ZoneContext.ZoneType == WorldZoneType.Ocean &&
                    string.Equals(waters[0].ZoneContext.ZoneId, "ocean", StringComparison.Ordinal);

                HashSet<string> explorationIds = new HashSet<string>(StringComparer.Ordinal);
                bool explorationPass = explorationZones.Length == ExplorationIds.Length;
                for (int i = 0; i < explorationZones.Length && explorationPass; i++)
                {
                    ZoneContext zone = explorationZones[i];
                    explorationPass = zone != null &&
                                      zone.ZoneType == WorldZoneType.Exploration &&
                                      explorationIds.Add(zone.ZoneId) &&
                                      ExplorationIds.Contains(zone.ZoneId, StringComparer.Ordinal);
                }
                explorationPass &= ExplorationIds.All(explorationIds.Contains);

                MissionDefinition mission = missionManager != null ? missionManager.StartingMission : null;
                bool missionPass = mission != null &&
                                   mission.ObjectiveType == MissionObjectiveType.ExploreLocations &&
                                   mission.ExplorationZoneCount == ExplorationIds.Length &&
                                   swimmer != null && missionManager.PlayerActor == swimmer.gameObject &&
                                   ExplorationIds.All(mission.IsExplorationZone) &&
                                   missionHud.MissionManager == missionManager;

                bool persistencePass = saveCoordinator != null &&
                                       saveCoordinator.MissionManager == missionManager &&
                                       saveCoordinator.PersistentZoneCount >= ExplorationIds.Length;

                bool inputCameraPass = structurePass &&
                                       aquatic.DrivingInput == drivingInput &&
                                       aquatic.SwimInput == swimInput &&
                                       aquatic.SwimController == swimController &&
                                       aquatic.CameraFollow == cameraFollow &&
                                       aquatic.VehicleCameraTarget == vehicle.transform &&
                                       aquatic.SwimCameraTarget == swimmer &&
                                       swimInput.HasRequiredControls &&
                                       swimInput.DirectTouchFallbackEnabled &&
                                       swimInput.LegacyTouchFallbackEnabled &&
                                       swimControls.GetComponentsInChildren<TouchHoldButton>(true).Length == 6 &&
                                       ValidateModeRoundTrip(aquatic, drivingInput, swimInput, cameraFollow, vehicle.transform, swimmer);

                int enabledCameraCount = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Count(candidate => candidate.enabled);
                bool singleCameraPass = enabledCameraCount == 1;

                bool inheritedPass =
                    FindRoot(scene, "Phase4FreeRoamActivities") != null &&
                    FindRoot(scene, "ParkingPrototype") != null &&
                    FindRoot(scene, "Phase4MechanicJobSystem") != null &&
                    FindRoot(scene, "Phase3RestrictedArea") != null &&
                    FindRoot(scene, "Phase1MissionSystem") != null;

                bool buildSettingsPass = EditorBuildSettings.scenes.Length == 1 &&
                                         EditorBuildSettings.scenes[0].enabled &&
                                         string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);

                bool workflowPass = ValidateWorkflowContracts(out string workflowDetail);
                bool documentationPass = ValidatePhysicalGateDocumentation(out string documentationDetail);

                bool pass = structurePass && waterPass && explorationPass && missionPass && persistencePass &&
                            inputCameraPass && singleCameraPass && inheritedPass && buildSettingsPass &&
                            workflowPass && documentationPass;

                message = pass
                    ? "[Beyond The Beat] Phase 5 FINAL exit validation PASS: Ocean, Swim/Dive, mobile swim/camera, Exploration mission/persistence, inherited gameplay, single-camera, single-scene, fast-PR and post-merge single-APK contracts are intact. CI GREEN IS NOT DEVICE SIGN-OFF."
                    : "[Beyond The Beat] Phase 5 FINAL exit validation FAIL: " +
                      $"structure={structurePass}, water={waterPass}, exploration={explorationPass}, mission={missionPass}, " +
                      $"persistence={persistencePass}, inputCamera={inputCameraPass}, singleCamera={singleCameraPass}, " +
                      $"inherited={inheritedPass}, buildSettings={buildSettingsPass}, workflow={workflowPass} ({workflowDetail}), " +
                      $"documentation={documentationPass} ({documentationDetail}).";
                return pass;
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool ValidateModeRoundTrip(
            AquaticModeCoordinator aquatic,
            MobileDrivingInput drivingInput,
            MobileSwimInput swimInput,
            CameraFollow cameraFollow,
            Transform vehicle,
            Transform swimmer)
        {
            if (aquatic == null || drivingInput == null || swimInput == null || cameraFollow == null ||
                vehicle == null || swimmer == null)
            {
                return false;
            }

            aquatic.SetSwimMode(false, false, false);
            bool driving = drivingInput.enabled && !swimInput.InputEnabled && cameraFollow.Target == vehicle;

            aquatic.SetSwimMode(true, false, false);
            bool swimming = !drivingInput.enabled && swimInput.InputEnabled && cameraFollow.Target == swimmer;

            aquatic.SetSwimMode(false, false, false);
            bool restored = drivingInput.enabled && !swimInput.InputEnabled && cameraFollow.Target == vehicle;
            return driving && swimming && restored;
        }

        private static bool ValidateWorkflowContracts(out string detail)
        {
            string projectRoot = GetProjectRoot();
            string fullPath = Path.Combine(projectRoot, FullWorkflowPath.Replace('/', Path.DirectorySeparatorChar));
            string fastPath = Path.Combine(projectRoot, FastWorkflowPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath) || !File.Exists(fastPath))
            {
                detail = $"fullExists={File.Exists(fullPath)}, fastExists={File.Exists(fastPath)}";
                return false;
            }

            string full = File.ReadAllText(fullPath);
            string fast = File.ReadAllText(fastPath);

            string[] fullBuildMethods = full
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("buildMethod:", StringComparison.Ordinal))
                .ToArray();
            string[] fastBuildMethods = fast
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("buildMethod:", StringComparison.Ordinal))
                .ToArray();

            bool fullPass =
                full.Contains("name: Current Android Test Build", StringComparison.Ordinal) &&
                full.Contains("workflow_dispatch:", StringComparison.Ordinal) &&
                full.Contains("push:", StringComparison.Ordinal) &&
                full.Contains("- main", StringComparison.Ordinal) &&
                full.Contains("TEST-THIS-BUILD-${GITHUB_RUN_NUMBER}", StringComparison.Ordinal) &&
                full.Contains("This is the ONLY APK intended for current device testing.", StringComparison.Ordinal) &&
                fullBuildMethods.Length == 1 &&
                fullBuildMethods[0].Contains("Phase5ExplorationBuildAutomation.BuildAndroid", StringComparison.Ordinal);

            bool fastPass =
                fast.Contains("name: Fast Current Milestone Validation", StringComparison.Ordinal) &&
                fast.Contains("pull_request:", StringComparison.Ordinal) &&
                fastBuildMethods.Length == 1 &&
                fastBuildMethods[0].Contains("Phase5ExitFastValidation.Validate", StringComparison.Ordinal) &&
                !fast.Contains("BuildPipeline.BuildPlayer", StringComparison.Ordinal) &&
                !fast.Contains("androidExportType: androidPackage", StringComparison.Ordinal);

            detail = $"full={fullPass}, fast={fastPass}, fullMethods={fullBuildMethods.Length}, fastMethods={fastBuildMethods.Length}";
            return fullPass && fastPass;
        }

        private static bool ValidatePhysicalGateDocumentation(out string detail)
        {
            string path = Path.Combine(GetProjectRoot(), ValidationDocPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                detail = "document missing";
                return false;
            }

            string document = File.ReadAllText(path);
            string[] required =
            {
                "CI GREEN IS NOT DEVICE SIGN-OFF",
                "LEFT / RIGHT / GO / REV / ACTION",
                "SWIM TEST",
                "DIVE",
                "SURFACE",
                "ocean-cove",
                "ocean-reef",
                "ocean-wreck",
                "save/relaunch",
                "FPS",
                "thermal",
                "battery"
            };

            string missing = string.Join(", ", required.Where(token => !document.Contains(token, StringComparison.Ordinal)));
            detail = string.IsNullOrEmpty(missing) ? "complete" : "missing: " + missing;
            return string.IsNullOrEmpty(missing);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

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

        private static string GetProjectRoot()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root for Phase 5 exit validation.");
            }

            return projectRoot;
        }
    }
}
