using System;
using System.Collections.Generic;
using System.Linq;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.UI;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    internal static class Phase5ExplorationMissionBuilder
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string RootName = "Phase5ExplorationCheckpoints";
        private const string MissionRootName = "Phase1MissionSystem";
        private const string PersistenceRootName = "Phase1Persistence";
        private const string SwimRootName = "Phase5SwimPrototype";
        private const string SwimActorName = "SwimPrototypeActor";
        private const string VehicleName = "PrototypeVehicle";
        private const string OceanRootName = "Phase5OceanArea";
        private const string OceanVolumeName = "OceanVolume";
        private const string MissionAssetPath = "Assets/Settings/Missions/Phase5_ExploreOcean.asset";
        private const string MissionId = "phase5-explore-ocean";
        private const string ValidationDocPath = "Docs/Validation/PHASE_5_EXPLORATION_MISSION.md";

        private static readonly string[] CheckpointIds =
        {
            "ocean-cove",
            "ocean-reef",
            "ocean-wreck"
        };

        private static readonly Vector3[] CheckpointPositions =
        {
            new Vector3(96f, -1.4f, 130f),
            new Vector3(145f, -3.8f, 174f),
            new Vector3(180f, -5.5f, 132f)
        };

        [MenuItem("Beyond The Beat/Phase 5/Build Exploration Mission")]
        public static void BuildExplorationMission()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Phase 5 exploration build requires scene '{ScenePath}'.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject missionRoot = RequireRoot(scene, MissionRootName);
            GameObject persistenceRoot = RequireRoot(scene, PersistenceRootName);
            GameObject swimRoot = RequireRoot(scene, SwimRootName);
            GameObject vehicle = RequireRoot(scene, VehicleName);
            Transform swimmer = swimRoot.transform.Find(SwimActorName) ??
                                throw new InvalidOperationException($"Missing '{SwimActorName}' under '{SwimRootName}'.");

            MissionManager manager = missionRoot.GetComponent<MissionManager>() ??
                                     throw new InvalidOperationException("Phase1MissionSystem is missing MissionManager.");
            Phase1SaveCoordinator coordinator = persistenceRoot.GetComponent<Phase1SaveCoordinator>() ??
                                                throw new InvalidOperationException("Phase1Persistence is missing Phase1SaveCoordinator.");

            RemoveRoot(scene, RootName);
            GameObject root = new GameObject(RootName);
            ZoneContext[] checkpoints = new ZoneContext[CheckpointIds.Length];
            for (int i = 0; i < CheckpointIds.Length; i++)
            {
                checkpoints[i] = CreateCheckpoint(root.transform, CheckpointIds[i], CheckpointPositions[i], i + 1);
            }

            MissionDefinition mission = CreateOrUpdateMissionDefinition();
            ZoneContext[] allZones = GetSceneComponents<ZoneContext>(scene)
                .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
                .ToArray();

            ConfigureMissionManager(manager, mission, swimmer.gameObject, allZones);
            ConfigurePersistence(coordinator, allZones);

            MissionHud hud = GetSceneComponents<MissionHud>(scene).FirstOrDefault();
            if (hud != null)
            {
                hud.SetSource(manager);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Unable to save exploration mission integration into '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            Debug.Log(
                "[Beyond The Beat] Phase 5 exploration mission created with three stable checkpoint ZoneContexts, " +
                "swimmer actor binding, HUD progress and additive save/resume wiring.");
        }

        [MenuItem("Beyond The Beat/Phase 5/Validate Exploration Mission")]
        public static void ValidateExplorationMission()
        {
            if (!ValidateExplorationMissionInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static bool ValidateExplorationMissionOrThrow()
        {
            if (ValidateExplorationMissionInternal(out string message))
            {
                Debug.Log(message);
                return true;
            }

            throw new InvalidOperationException(message);
        }

        private static bool ValidateExplorationMissionInternal(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = $"[Beyond The Beat] Phase 5 exploration validation FAIL: scene missing at '{ScenePath}'.";
                return false;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            bool openedForValidation = originalScene.path != ScenePath;
            Scene scene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : originalScene;

            try
            {
                MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
                GameObject root = FindRoot(scene, RootName);
                GameObject missionRoot = FindRoot(scene, MissionRootName);
                GameObject persistenceRoot = FindRoot(scene, PersistenceRootName);
                GameObject swimRoot = FindRoot(scene, SwimRootName);
                GameObject vehicle = FindRoot(scene, VehicleName);
                Transform swimmer = swimRoot != null ? swimRoot.transform.Find(SwimActorName) : null;
                MissionManager manager = missionRoot != null ? missionRoot.GetComponent<MissionManager>() : null;
                Phase1SaveCoordinator coordinator = persistenceRoot != null
                    ? persistenceRoot.GetComponent<Phase1SaveCoordinator>()
                    : null;
                MissionHud hud = GetSceneComponents<MissionHud>(scene).FirstOrDefault();

                ZoneContext[] checkpoints = root != null
                    ? root.GetComponentsInChildren<ZoneContext>(true)
                        .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
                        .ToArray()
                    : Array.Empty<ZoneContext>();

                bool enumPass = (int)WorldZoneType.Ocean == 4 && (int)WorldZoneType.Exploration == 5;
                bool definitionPass = ValidateDefinition(mission);
                bool checkpointPass = ValidateCheckpoints(checkpoints);
                bool managerPass = manager != null &&
                                   manager.StartingMission == mission &&
                                   swimmer != null &&
                                   manager.PlayerActor == swimmer.gameObject &&
                                   CheckpointIds.All(id => GetSceneComponents<ZoneContext>(scene).Any(zone => zone.ZoneId == id));
                bool persistencePass = coordinator != null &&
                                       coordinator.PersistentZoneCount >= checkpoints.Length &&
                                       ValidateSaveDataRoundTrip();
                bool hudPass = hud != null && hud.MissionManager == manager && ValidateHudSnapshot(mission);
                bool lifecyclePass = ValidateLifecycle(manager, mission, checkpoints, swimmer, vehicle, scene, out string lifecycleDetail);
                bool inheritedPass = FindRoot(scene, OceanRootName) != null &&
                                     FindRoot(scene, "MobileDrivingCanvas") != null &&
                                     FindRoot(scene, "GameplayCamera") != null &&
                                     FindRoot(scene, "Phase4FreeRoamActivities") != null &&
                                     FindRoot(scene, "Phase3RestrictedArea") != null;
                bool buildSettingsPass = EditorBuildSettings.scenes.Length == 1 &&
                                         EditorBuildSettings.scenes[0].enabled &&
                                         string.Equals(EditorBuildSettings.scenes[0].path, ScenePath, StringComparison.Ordinal);
                bool docPass = AssetDatabase.LoadAssetAtPath<TextAsset>(ValidationDocPath) != null ||
                               System.IO.File.Exists(ValidationDocPath);

                bool pass = enumPass && definitionPass && checkpointPass && managerPass && persistencePass &&
                            hudPass && lifecyclePass && inheritedPass && buildSettingsPass && docPass;

                message = pass
                    ? "[Beyond The Beat] Phase 5 exploration mission validation PASS: stable data-driven checkpoints, unique visit progress, any-order completion, wrong-actor/unrelated-zone rejection, HUD progress, additive save data, inherited swim/camera systems and single-scene build contract are intact."
                    : "[Beyond The Beat] Phase 5 exploration mission validation FAIL: " +
                      $"enum={enumPass}, definition={definitionPass}, checkpoints={checkpointPass}, manager={managerPass}, " +
                      $"persistence={persistencePass}, hud={hudPass}, lifecycle={lifecyclePass} ({lifecycleDetail}), " +
                      $"inherited={inheritedPass}, buildSettings={buildSettingsPass}, doc={docPass}.";
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

        private static ZoneContext CreateCheckpoint(Transform parent, string zoneId, Vector3 position, int index)
        {
            GameObject checkpoint = new GameObject($"Checkpoint_{index}_{zoneId}", typeof(BoxCollider), typeof(ZoneContext));
            checkpoint.transform.SetParent(parent, false);
            checkpoint.transform.position = position;

            BoxCollider trigger = checkpoint.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(12f, 7f, 12f);

            ZoneContext context = checkpoint.GetComponent<ZoneContext>();
            SerializedObject serializedContext = new SerializedObject(context);
            SetString(serializedContext, "zoneId", zoneId);
            SetInt(serializedContext, "zoneType", (int)WorldZoneType.Exploration);
            serializedContext.ApplyModifiedPropertiesWithoutUndo();

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            marker.transform.SetParent(checkpoint.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(1.8f, 0.12f, 1.8f);
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(markerCollider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return context;
        }

        private static MissionDefinition CreateOrUpdateMissionDefinition()
        {
            MissionDefinition mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionAssetPath);
            if (mission == null)
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                AssetDatabase.CreateAsset(mission, MissionAssetPath);
            }

            SerializedObject serialized = new SerializedObject(mission);
            SetString(serialized, "missionId", MissionId);
            SetString(serialized, "displayName", "Explore the Northern Ocean");
            SetString(serialized, "description", "Swim through the Cove, Reef and Wreck exploration checkpoints.");
            SetInt(serialized, "objectiveType", (int)MissionObjectiveType.ExploreLocations);
            SetString(serialized, "targetZoneId", string.Empty);
            SetFloat(serialized, "survivalDurationSeconds", 0f);
            SetString(serialized, "targetPuzzleId", string.Empty);
            SerializedProperty zones = serialized.FindProperty("explorationZoneIds") ??
                                       throw new InvalidOperationException("MissionDefinition explorationZoneIds field is missing.");
            zones.arraySize = CheckpointIds.Length;
            for (int i = 0; i < CheckpointIds.Length; i++)
            {
                zones.GetArrayElementAtIndex(i).stringValue = CheckpointIds[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mission);
            return mission;
        }

        private static void ConfigureMissionManager(
            MissionManager manager,
            MissionDefinition mission,
            GameObject swimmer,
            ZoneContext[] zones)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetObjectReference(serialized, "startingMission", mission);
            SetObjectReference(serialized, "playerActor", swimmer);
            SetBool(serialized, "startOnPlay", true);
            SetObjectArray(serialized, "observedZones", zones.Cast<UnityEngine.Object>().ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            manager.RebindZoneSources();
            EditorUtility.SetDirty(manager);
        }

        private static void ConfigurePersistence(Phase1SaveCoordinator coordinator, ZoneContext[] zones)
        {
            SerializedObject serialized = new SerializedObject(coordinator);
            SetObjectArray(serialized, "persistentZones", zones.Cast<UnityEngine.Object>().ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(coordinator);
        }

        private static bool ValidateDefinition(MissionDefinition mission)
        {
            if (mission == null || !mission.IsConfigured ||
                mission.ObjectiveType != MissionObjectiveType.ExploreLocations ||
                !string.Equals(mission.MissionId, MissionId, StringComparison.Ordinal) ||
                mission.ExplorationZoneCount != CheckpointIds.Length)
            {
                return false;
            }

            for (int i = 0; i < CheckpointIds.Length; i++)
            {
                if (!mission.IsExplorationZone(CheckpointIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateCheckpoints(ZoneContext[] checkpoints)
        {
            if (checkpoints == null || checkpoints.Length != CheckpointIds.Length)
            {
                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < checkpoints.Length; i++)
            {
                ZoneContext checkpoint = checkpoints[i];
                BoxCollider trigger = checkpoint != null ? checkpoint.GetComponent<BoxCollider>() : null;
                if (checkpoint == null || trigger == null || !trigger.isTrigger ||
                    checkpoint.ZoneType != WorldZoneType.Exploration ||
                    !ids.Add(checkpoint.ZoneId) ||
                    !CheckpointIds.Contains(checkpoint.ZoneId, StringComparer.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateLifecycle(
            MissionManager manager,
            MissionDefinition mission,
            ZoneContext[] checkpoints,
            Transform swimmer,
            GameObject vehicle,
            Scene scene,
            out string detail)
        {
            if (manager == null || mission == null || checkpoints.Length != CheckpointIds.Length || swimmer == null || vehicle == null)
            {
                detail = "required references missing";
                return false;
            }

            ZoneContext oceanZone = FindRoot(scene, OceanRootName)?.transform.Find(OceanVolumeName)?.GetComponent<ZoneContext>();
            ZoneContext[] ordered = CheckpointIds
                .Select(id => checkpoints.FirstOrDefault(zone => string.Equals(zone.ZoneId, id, StringComparison.Ordinal)))
                .ToArray();
            if (ordered.Any(zone => zone == null) || oceanZone == null)
            {
                detail = "checkpoint/ocean resolution failed";
                return false;
            }

            manager.ClearMission();
            bool started = manager.StartMission(mission);
            bool wrongActorRejected = !manager.TryProcessZoneEntry(ordered[0], vehicle);
            bool unrelatedRejected = !manager.TryProcessZoneEntry(oceanZone, swimmer.gameObject);
            bool firstAccepted = manager.TryProcessZoneEntry(ordered[2], swimmer.gameObject) &&
                                 manager.Progress.ExplorationVisitedCount == 1;
            bool duplicateRejected = !manager.TryProcessZoneEntry(ordered[2], swimmer.gameObject) &&
                                     manager.Progress.ExplorationVisitedCount == 1;
            bool secondAccepted = manager.TryProcessZoneEntry(ordered[0], swimmer.gameObject) &&
                                  manager.Progress.ExplorationVisitedCount == 2 &&
                                  manager.State == MissionState.Active;
            bool finalAccepted = manager.TryProcessZoneEntry(ordered[1], swimmer.gameObject) &&
                                 manager.State == MissionState.Completed &&
                                 manager.Progress.ExplorationVisitedCount == 3 &&
                                 Mathf.Approximately(manager.Progress.NormalizedProgress, 1f);
            bool completionOnce = !manager.TryProcessZoneEntry(ordered[1], swimmer.gameObject) &&
                                  manager.State == MissionState.Completed;

            bool restoreStarted = manager.StartMission(mission);
            bool restorePass = manager.RestoreExplorationProgress(new[] { CheckpointIds[1], CheckpointIds[0] }) &&
                               manager.State == MissionState.Active &&
                               manager.Progress.ExplorationVisitedCount == 2 &&
                               Mathf.Abs(manager.Progress.NormalizedProgress - (2f / 3f)) < 0.001f &&
                               manager.TryProcessZoneEntry(ordered[2], swimmer.gameObject) &&
                               manager.State == MissionState.Completed;

            detail =
                $"started={started}, wrongActorRejected={wrongActorRejected}, unrelatedRejected={unrelatedRejected}, " +
                $"firstAccepted={firstAccepted}, duplicateRejected={duplicateRejected}, secondAccepted={secondAccepted}, " +
                $"finalAccepted={finalAccepted}, completionOnce={completionOnce}, restoreStarted={restoreStarted}, restorePass={restorePass}";

            return started && wrongActorRejected && unrelatedRejected && firstAccepted && duplicateRejected &&
                   secondAccepted && finalAccepted && completionOnce && restoreStarted && restorePass;
        }

        private static bool ValidateHudSnapshot(MissionDefinition mission)
        {
            if (mission == null)
            {
                return false;
            }

            MissionProgressSnapshot progress = new MissionProgressSnapshot(
                MissionObjectiveType.ExploreLocations,
                false,
                0f,
                0f,
                false,
                1,
                3);
            MissionHudSnapshot snapshot = MissionHud.CreateSnapshot(mission, MissionState.Active, progress);
            return snapshot.Objective.Contains("1/3") &&
                   snapshot.Status.Contains("EXPLORING") &&
                   Mathf.Abs(progress.NormalizedProgress - (1f / 3f)) < 0.001f;
        }

        private static bool ValidateSaveDataRoundTrip()
        {
            GameSaveData data = new GameSaveData
            {
                SceneId = "Phase5_Ocean",
                MissionId = MissionId,
                MissionState = MissionState.Active,
                HasPhase5ExplorationState = true,
                MissionVisitedExplorationZoneIds = new[] { CheckpointIds[2], CheckpointIds[0] }
            };

            string json = JsonUtility.ToJson(data);
            GameSaveData restored = JsonUtility.FromJson<GameSaveData>(json);
            bool roundTrip = restored != null && restored.HasPhase5ExplorationState &&
                             restored.MissionVisitedExplorationZoneIds != null &&
                             restored.MissionVisitedExplorationZoneIds.Length == 2 &&
                             restored.MissionVisitedExplorationZoneIds.Contains(CheckpointIds[0]) &&
                             restored.MissionVisitedExplorationZoneIds.Contains(CheckpointIds[2]);

            GameSaveData legacy = JsonUtility.FromJson<GameSaveData>(
                "{\"Version\":1,\"SceneId\":\"Phase3_RestrictedArea\",\"MissionId\":\"legacy\",\"MissionState\":1}");
            bool legacyPass = legacy != null && !legacy.HasPhase5ExplorationState &&
                              (legacy.MissionVisitedExplorationZoneIds == null ||
                               legacy.MissionVisitedExplorationZoneIds.Length == 0);
            return roundTrip && legacyPass;
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> result = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                result.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }
            return result;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return FindRoot(scene, name) ??
                   throw new InvalidOperationException($"Required scene root '{name}' is missing.");
        }

        private static GameObject FindRoot(Scene scene, string name)
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
            GameObject root = FindRoot(scene, name);
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetObjectReference(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized object property '{name}'.");
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedObject serialized, string name, UnityEngine.Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(name) ??
                                          throw new InvalidOperationException($"Missing serialized array property '{name}'.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
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
