using System;
using System.Linq;
using BeyondTheBeat.Interaction;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.UI;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyondTheBeat.Editor
{
    internal static class Phase1ExitValidator
    {
        private const string ScenePath = Phase1WorldBuilder.Phase1ScenePath;
        private const string VehicleName = "PrototypeVehicle";
        private const string CanvasName = "MobileDrivingCanvas";
        private const string ParkingRootName = "ParkingPrototype";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string PersistenceRootName = "Phase1Persistence";
        private const string MissionHudName = "Phase1MissionHUD";

        [MenuItem("Beyond The Beat/Phase 1/Validate MVP Exit Gate")]
        public static void ValidateMvpExitGate()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Beyond The Beat] Phase 1 exit validation FAIL: scene missing at '{ScenePath}'.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene validationScene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                GameObject vehicle = FindRootObject(validationScene, VehicleName);
                GameObject canvasObject = FindRootObject(validationScene, CanvasName);
                GameObject parkingRoot = FindRootObject(validationScene, ParkingRootName);
                GameObject missionRoot = FindRootObject(validationScene, MissionRootName);
                GameObject persistenceRoot = FindRootObject(validationScene, PersistenceRootName);

                InteractionController interactionController =
                    vehicle != null ? vehicle.GetComponent<InteractionController>() : null;
                MobileDrivingInput mobileInput =
                    canvasObject != null ? canvasObject.GetComponent<MobileDrivingInput>() : null;
                ParkingZone parkingZone = parkingRoot != null
                    ? parkingRoot.GetComponentInChildren<ParkingZone>(true)
                    : null;
                MissionManager missionManager =
                    missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                SaveManager saveManager =
                    persistenceRoot != null ? persistenceRoot.GetComponent<SaveManager>() : null;
                Phase1SaveCoordinator saveCoordinator =
                    persistenceRoot != null ? persistenceRoot.GetComponent<Phase1SaveCoordinator>() : null;
                Transform missionHudTransform =
                    canvasObject != null ? canvasObject.transform.Find(MissionHudName) : null;
                MissionHud missionHud =
                    missionHudTransform != null ? missionHudTransform.GetComponent<MissionHud>() : null;

                bool mobileDrivingPass =
                    canvasObject != null &&
                    canvasObject.TryGetComponent<Canvas>(out Canvas canvas) &&
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    canvasObject.TryGetComponent<GraphicRaycaster>(out _) &&
                    mobileInput != null &&
                    mobileInput.DirectTouchFallbackEnabled &&
                    canvasObject.GetComponentsInChildren<TouchHoldButton>(true).Length == 5;

                bool mobileMappingPass = false;
                if (mobileInput != null)
                {
                    mobileInput.EvaluateButtonStatesForValidation(
                        leftPressed: true,
                        rightPressed: false,
                        acceleratePressed: true,
                        brakeReversePressed: false,
                        interact: true,
                        out float steering,
                        out float throttle,
                        out float brake,
                        out bool interact);

                    mobileMappingPass =
                        Mathf.Approximately(steering, -1f) &&
                        Mathf.Approximately(throttle, 1f) &&
                        Mathf.Approximately(brake, 0f) &&
                        interact;
                }

                bool parkingPass =
                    interactionController != null &&
                    parkingZone != null &&
                    parkingZone.TryGetComponent<InteractionTrigger>(out _) &&
                    Mathf.Approximately(parkingZone.StopThresholdKph, 2f) &&
                    !string.IsNullOrWhiteSpace(parkingZone.SuccessMessage);

                bool persistencePass =
                    saveManager != null &&
                    saveCoordinator != null &&
                    missionManager != null &&
                    vehicle != null &&
                    saveCoordinator.SaveManager == saveManager &&
                    saveCoordinator.MissionManager == missionManager &&
                    saveCoordinator.VehicleTransform == vehicle.transform &&
                    saveCoordinator.LoadOnStart;

                bool hudPass =
                    missionHud != null &&
                    missionHud.MissionManager == missionManager &&
                    missionHud.PanelRoot != null &&
                    missionHud.TitleText != null &&
                    missionHud.ObjectiveText != null &&
                    missionHud.StatusText != null &&
                    missionHud.ProgressRoot != null &&
                    missionHud.ProgressFill != null;

                MissionDefinition mission = missionManager != null ? missionManager.StartingMission : null;
                ZoneContext targetZone = mission != null
                    ? FindZoneById(validationScene, mission.TargetZoneId)
                    : null;
                ZoneContext broadOffRoadZone = FindZoneById(validationScene, "off-road");

                bool missionFlowPass = false;
                bool freeRoamPass = false;
                bool hudStatePass = false;
                bool saveResumePass = false;

                if (missionManager != null && mission != null && vehicle != null && targetZone != null)
                {
                    missionManager.ClearMission();
                    bool preMissionFreeRoam =
                        missionManager.State == MissionState.Inactive &&
                        !missionManager.HasActiveMission;

                    bool started = missionManager.StartMission(mission);
                    bool wrongZoneRejected = broadOffRoadZone == null ||
                                             (!missionManager.TryProcessZoneEntry(broadOffRoadZone, vehicle) &&
                                              missionManager.State == MissionState.Active);

                    bool completedByTarget =
                        missionManager.TryProcessZoneEntry(targetZone, vehicle) &&
                        missionManager.State == MissionState.Completed &&
                        !missionManager.HasActiveMission;

                    missionFlowPass = started && wrongZoneRejected && completedByTarget;
                    freeRoamPass = preMissionFreeRoam && completedByTarget && mobileDrivingPass && parkingPass;

                    MissionHudSnapshot inactiveView = MissionHud.CreateSnapshot(null, MissionState.Inactive);
                    MissionHudSnapshot activeView = MissionHud.CreateSnapshot(mission, MissionState.Active);
                    MissionHudSnapshot completedView = MissionHud.CreateSnapshot(mission, MissionState.Completed);
                    hudStatePass =
                        inactiveView.Title == "FREE ROAM" &&
                        activeView.Title == mission.DisplayName &&
                        activeView.Status == "MISSION ACTIVE" &&
                        completedView.Title == "MISSION COMPLETE" &&
                        completedView.Status.IndexOf("COMPLETE", StringComparison.Ordinal) >= 0;

                    GameSaveData savedState = new GameSaveData
                    {
                        Version = SaveManager.CurrentVersion,
                        SceneId = validationScene.name,
                        VehicleTransform = SavedTransform.Capture(vehicle.transform),
                        MissionId = mission.MissionId,
                        MissionState = MissionState.Completed
                    };

                    string json = SaveManager.SerializeForStorage(savedState);
                    SaveLoadResult deserializeResult = SaveManager.DeserializeForStorage(json, out GameSaveData loadedState);

                    missionManager.ClearMission();
                    bool restored = deserializeResult == SaveLoadResult.Success &&
                                    loadedState != null &&
                                    missionManager.RestoreMissionState(loadedState.MissionId, loadedState.MissionState);

                    saveResumePass =
                        restored &&
                        missionManager.State == MissionState.Completed &&
                        missionManager.CurrentMissionId == mission.MissionId &&
                        !missionManager.HasActiveMission;

                    missionManager.ClearMission();
                }

                bool allPass =
                    mobileDrivingPass &&
                    mobileMappingPass &&
                    parkingPass &&
                    persistencePass &&
                    hudPass &&
                    missionFlowPass &&
                    freeRoamPass &&
                    hudStatePass &&
                    saveResumePass;

                string message =
                    "[Beyond The Beat] Phase 1 MVP exit-gate repository validation\n" +
                    $"Mobile driving/direct-touch structure: {PassFail(mobileDrivingPass)}\n" +
                    $"Deterministic mobile input mapping: {PassFail(mobileMappingPass)}\n" +
                    $"Parking/interaction regression structure: {PassFail(parkingPass)}\n" +
                    $"Central save/resume integration: {PassFail(persistencePass)}\n" +
                    $"Mission HUD integration: {PassFail(hudPass)}\n" +
                    $"Start -> target-zone -> completion lifecycle: {PassFail(missionFlowPass)}\n" +
                    $"Free roam before/after mission: {PassFail(freeRoamPass)}\n" +
                    $"HUD active/completed/free-roam states: {PassFail(hudStatePass)}\n" +
                    $"Integrated mission save/resume round-trip: {PassFail(saveResumePass)}\n" +
                    "Physical Android install/input/presentation validation: REQUIRED OUTSIDE CI";

                if (allPass)
                {
                    Debug.Log(message);
                }
                else
                {
                    Debug.LogError(message);
                }
            }
            finally
            {
                if (openedForValidation && validationScene.IsValid())
                {
                    EditorSceneManager.CloseScene(validationScene, true);
                }
            }
        }

        private static ZoneContext FindZoneById(Scene scene, string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return null;
            }

            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ZoneContext>(true))
                .FirstOrDefault(zone => string.Equals(zone.ZoneId, zoneId, StringComparison.Ordinal));
        }

        private static GameObject FindRootObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static string PassFail(bool value) => value ? "PASS" : "FAIL";
    }
}
