using System;
using System.Collections.Generic;
using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Missions
{
    public enum MissionState
    {
        Inactive = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    [DisallowMultipleComponent]
    public sealed class MissionManager : MonoBehaviour
    {
        [Header("Mission")]
        [SerializeField] private MissionDefinition startingMission;
        [SerializeField] private bool startOnPlay = true;

        [Header("World")]
        [SerializeField] private GameObject playerActor;
        [SerializeField] private ZoneContext[] observedZones = Array.Empty<ZoneContext>();

        private readonly HashSet<ZoneContext> subscribedZones = new HashSet<ZoneContext>();
        private MissionDefinition currentMission;
        private MissionState state = MissionState.Inactive;

        public MissionDefinition StartingMission => startingMission;
        public MissionDefinition CurrentMission => currentMission;
        public string CurrentMissionId => currentMission != null ? currentMission.MissionId : string.Empty;
        public MissionState State => state;
        public GameObject PlayerActor => playerActor;
        public int ObservedZoneCount => observedZones != null ? observedZones.Length : 0;
        public bool HasActiveMission => currentMission != null && state == MissionState.Active;

        public event Action<MissionDefinition> MissionStarted;
        public event Action<MissionDefinition> MissionCompleted;
        public event Action<MissionDefinition> MissionFailed;
        public event Action<MissionDefinition, MissionState> MissionStateChanged;

        private void OnEnable()
        {
            SubscribeToZones();
        }

        private void Start()
        {
            if (startOnPlay && startingMission != null && currentMission == null)
            {
                StartMission(startingMission);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromZones();
        }

        public bool StartMission(MissionDefinition mission)
        {
            if (mission == null || !mission.IsConfigured)
            {
                Debug.LogError("[Beyond The Beat] MissionManager cannot start an unconfigured mission definition.");
                return false;
            }

            currentMission = mission;
            SetState(MissionState.Active);

            Debug.Log(
                $"[Beyond The Beat] Mission STARTED: id='{mission.MissionId}', " +
                $"objective={mission.ObjectiveType}, targetZone='{mission.TargetZoneId}'.");

            MissionStarted?.Invoke(mission);
            return true;
        }

        public bool RestoreMissionState(string missionId, MissionState restoredState)
        {
            if (restoredState == MissionState.Inactive || string.IsNullOrWhiteSpace(missionId))
            {
                ClearMission();
                return restoredState == MissionState.Inactive;
            }

            if (restoredState != MissionState.Active &&
                restoredState != MissionState.Completed &&
                restoredState != MissionState.Failed)
            {
                Debug.LogWarning($"[Beyond The Beat] Mission restore rejected unsupported state {restoredState}.");
                return false;
            }

            MissionDefinition mission = ResolveMissionById(missionId);
            if (mission == null)
            {
                Debug.LogWarning($"[Beyond The Beat] Mission restore could not resolve id '{missionId}'.");
                return false;
            }

            currentMission = mission;
            SetState(restoredState);

            Debug.Log(
                $"[Beyond The Beat] Mission state RESTORED: id='{mission.MissionId}', state={restoredState}.");
            return true;
        }

        public bool FailActiveMission()
        {
            if (!HasActiveMission)
            {
                return false;
            }

            MissionDefinition failedMission = currentMission;
            SetState(MissionState.Failed);
            MissionFailed?.Invoke(failedMission);

            Debug.Log($"[Beyond The Beat] Mission FAILED: id='{failedMission.MissionId}'.");
            return true;
        }

        public void ClearMission()
        {
            currentMission = null;
            SetState(MissionState.Inactive);
        }

        public bool TryProcessZoneEntry(ZoneContext zone, GameObject actor)
        {
            if (!HasActiveMission ||
                !MissionObjectiveEvaluator.IsSatisfied(currentMission, zone, actor, playerActor))
            {
                return false;
            }

            return CompleteActiveMission();
        }

        private void HandleZoneEntered(ZoneContext zone, GameObject actor)
        {
            TryProcessZoneEntry(zone, actor);
        }

        private bool CompleteActiveMission()
        {
            if (!HasActiveMission)
            {
                return false;
            }

            MissionDefinition completedMission = currentMission;
            SetState(MissionState.Completed);
            MissionCompleted?.Invoke(completedMission);

            Debug.Log(
                $"[Beyond The Beat] Mission COMPLETED: id='{completedMission.MissionId}'. " +
                "Free roam remains available.");
            return true;
        }

        private MissionDefinition ResolveMissionById(string missionId)
        {
            if (currentMission != null &&
                string.Equals(currentMission.MissionId, missionId, StringComparison.Ordinal))
            {
                return currentMission;
            }

            if (startingMission != null &&
                string.Equals(startingMission.MissionId, missionId, StringComparison.Ordinal))
            {
                return startingMission;
            }

            return null;
        }

        private void SetState(MissionState newState)
        {
            if (state == newState)
            {
                return;
            }

            state = newState;
            MissionStateChanged?.Invoke(currentMission, state);
        }

        private void SubscribeToZones()
        {
            if (observedZones == null)
            {
                return;
            }

            for (int i = 0; i < observedZones.Length; i++)
            {
                ZoneContext zone = observedZones[i];
                if (zone == null || !subscribedZones.Add(zone))
                {
                    continue;
                }

                zone.ActorEntered += HandleZoneEntered;
            }
        }

        private void UnsubscribeFromZones()
        {
            foreach (ZoneContext zone in subscribedZones)
            {
                if (zone != null)
                {
                    zone.ActorEntered -= HandleZoneEntered;
                }
            }

            subscribedZones.Clear();
        }
    }
}
