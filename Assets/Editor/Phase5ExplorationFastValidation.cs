using System;
using BeyondTheBeat.Missions;
using BeyondTheBeat.Persistence;
using BeyondTheBeat.UI;
using BeyondTheBeat.World;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase5ExplorationFastValidation
    {
        private static readonly string[] CheckpointIds =
        {
            "fast-ocean-cove",
            "fast-ocean-reef",
            "fast-ocean-wreck"
        };

        public static void Validate()
        {
            Phase5FastValidation.Validate();
            ValidateExplorationContract();
            ValidateSaveContract();

            Debug.Log(
                "[Beyond The Beat] FAST PR VALIDATION PASS: merged swim/camera contracts plus data-driven exploration mission, " +
                "unique checkpoint progress, HUD normalization and additive save schema passed without scene regeneration or APK packaging.");
        }

        private static void ValidateExplorationContract()
        {
            MissionDefinition mission = null;
            GameObject managerObject = null;
            GameObject actor = null;
            GameObject wrongActor = null;
            GameObject[] checkpointObjects = new GameObject[CheckpointIds.Length];

            try
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                ConfigureMission(mission);

                actor = new GameObject("FastExplorationActor");
                wrongActor = new GameObject("FastExplorationWrongActor");
                managerObject = new GameObject("FastExplorationMissionManager");
                MissionManager manager = managerObject.AddComponent<MissionManager>();

                ZoneContext[] checkpoints = new ZoneContext[CheckpointIds.Length];
                for (int i = 0; i < CheckpointIds.Length; i++)
                {
                    checkpointObjects[i] = new GameObject($"FastCheckpoint{i}", typeof(BoxCollider), typeof(ZoneContext));
                    checkpoints[i] = checkpointObjects[i].GetComponent<ZoneContext>();
                    ConfigureZone(checkpoints[i], CheckpointIds[i]);
                }

                ConfigureManager(manager, mission, actor, checkpoints);

                bool configured = mission.IsConfigured && mission.ExplorationZoneCount == 3;
                bool started = manager.StartMission(mission);
                bool wrongRejected = !manager.TryProcessZoneEntry(checkpoints[0], wrongActor);
                bool first = manager.TryProcessZoneEntry(checkpoints[2], actor) && manager.Progress.ExplorationVisitedCount == 1;
                bool duplicate = !manager.TryProcessZoneEntry(checkpoints[2], actor) && manager.Progress.ExplorationVisitedCount == 1;
                bool second = manager.TryProcessZoneEntry(checkpoints[0], actor) &&
                              manager.Progress.ExplorationVisitedCount == 2 &&
                              Mathf.Abs(manager.Progress.NormalizedProgress - (2f / 3f)) < 0.001f;
                bool final = manager.TryProcessZoneEntry(checkpoints[1], actor) &&
                             manager.State == MissionState.Completed &&
                             manager.Progress.ExplorationVisitedCount == 3;

                bool restoredStart = manager.StartMission(mission);
                bool restored = manager.RestoreExplorationProgress(new[] { CheckpointIds[1], CheckpointIds[0] }) &&
                                manager.Progress.ExplorationVisitedCount == 2 &&
                                manager.State == MissionState.Active;

                MissionHudSnapshot hud = MissionHud.CreateSnapshot(mission, MissionState.Active, manager.Progress);
                bool hudPass = hud.Objective.Contains("2/3") && hud.Status.Contains("EXPLORING");

                if (!configured || !started || !wrongRejected || !first || !duplicate || !second || !final ||
                    !restoredStart || !restored || !hudPass)
                {
                    throw new InvalidOperationException(
                        "Phase 5 exploration fast validation failed: " +
                        $"configured={configured}, started={started}, wrongRejected={wrongRejected}, first={first}, " +
                        $"duplicate={duplicate}, second={second}, final={final}, restoredStart={restoredStart}, " +
                        $"restored={restored}, hud={hudPass}.");
                }
            }
            finally
            {
                if (mission != null)
                {
                    UnityEngine.Object.DestroyImmediate(mission);
                }
                if (managerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(managerObject);
                }
                if (actor != null)
                {
                    UnityEngine.Object.DestroyImmediate(actor);
                }
                if (wrongActor != null)
                {
                    UnityEngine.Object.DestroyImmediate(wrongActor);
                }
                for (int i = 0; i < checkpointObjects.Length; i++)
                {
                    if (checkpointObjects[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(checkpointObjects[i]);
                    }
                }
            }
        }

        private static void ValidateSaveContract()
        {
            GameSaveData data = new GameSaveData
            {
                SceneId = "Phase5_Ocean",
                MissionId = "fast-exploration",
                MissionState = MissionState.Active,
                HasPhase5ExplorationState = true,
                MissionVisitedExplorationZoneIds = new[] { CheckpointIds[0], CheckpointIds[2] }
            };

            GameSaveData restored = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
            if (restored == null || !restored.HasPhase5ExplorationState ||
                restored.MissionVisitedExplorationZoneIds == null ||
                restored.MissionVisitedExplorationZoneIds.Length != 2)
            {
                throw new InvalidOperationException("Phase 5 exploration fast validation failed: additive save round-trip was not preserved.");
            }
        }

        private static void ConfigureMission(MissionDefinition mission)
        {
            SerializedObject serialized = new SerializedObject(mission);
            SetString(serialized, "missionId", "fast-exploration");
            SetString(serialized, "displayName", "Fast Exploration");
            SetString(serialized, "description", "Visit all fast checkpoints.");
            SetInt(serialized, "objectiveType", (int)MissionObjectiveType.ExploreLocations);
            SerializedProperty zones = serialized.FindProperty("explorationZoneIds") ??
                                       throw new InvalidOperationException("MissionDefinition explorationZoneIds field is missing.");
            zones.arraySize = CheckpointIds.Length;
            for (int i = 0; i < CheckpointIds.Length; i++)
            {
                zones.GetArrayElementAtIndex(i).stringValue = CheckpointIds[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureZone(ZoneContext zone, string zoneId)
        {
            SerializedObject serialized = new SerializedObject(zone);
            SetString(serialized, "zoneId", zoneId);
            SetInt(serialized, "zoneType", (int)WorldZoneType.Exploration);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureManager(
            MissionManager manager,
            MissionDefinition mission,
            GameObject actor,
            ZoneContext[] zones)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SetObjectReference(serialized, "startingMission", mission);
            SetObjectReference(serialized, "playerActor", actor);
            SerializedProperty observed = serialized.FindProperty("observedZones") ??
                                          throw new InvalidOperationException("MissionManager observedZones field is missing.");
            observed.arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++)
            {
                observed.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
            }
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
    }
}
