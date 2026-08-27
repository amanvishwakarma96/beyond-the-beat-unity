using System;
using System.Collections.Generic;
using BeyondTheBeat.Survival;
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

    public readonly struct MissionProgressSnapshot
    {
        public MissionProgressSnapshot(
            MissionObjectiveType objectiveType,
            bool targetContextActive,
            float survivalElapsedSeconds,
            float survivalRequiredSeconds)
        {
            ObjectiveType = objectiveType;
            TargetContextActive = targetContextActive;
            SurvivalElapsedSeconds = Mathf.Max(0f, survivalElapsedSeconds);
            SurvivalRequiredSeconds = Mathf.Max(0f, survivalRequiredSeconds);
        }

        public MissionObjectiveType ObjectiveType { get; }
        public bool TargetContextActive { get; }
        public float SurvivalElapsedSeconds { get; }
        public float SurvivalRequiredSeconds { get; }
        public float NormalizedProgress => SurvivalRequiredSeconds > 0f
            ? Mathf.Clamp01(SurvivalElapsedSeconds / SurvivalRequiredSeconds)
            : 0f;
    }

    [DisallowMultipleComponent]
    public sealed class MissionManager : MonoBehaviour
    {
        private const float ProgressPublishIntervalSeconds = 0.25f;

        [Header("Mission")]
        [SerializeField] private MissionDefinition startingMission;
        [SerializeField] private bool startOnPlay = true;

        [Header("World")]
        [SerializeField] private GameObject playerActor;
        [SerializeField] private ZoneContext[] observedZones = Array.Empty<ZoneContext>();

        [Header("Optional objective sources")]
        [SerializeField] private ForestSurvivalController survivalController;

        private readonly HashSet<ZoneContext> subscribedZones = new HashSet<ZoneContext>();
        private MissionDefinition currentMission;
        private MissionState state = MissionState.Inactive;
        private bool targetContextActive;
        private float survivalElapsedSeconds;
        private float lastPublishedSurvivalElapsed = -1f;

        public MissionDefinition StartingMission => startingMission;
        public MissionDefinition CurrentMission => currentMission;
        public string CurrentMissionId => currentMission != null ? currentMission.MissionId : string.Empty;
        public MissionState State => state;
        public GameObject PlayerActor => playerActor;
        public int ObservedZoneCount => observedZones != null ? observedZones.Length : 0;
        public ForestSurvivalController SurvivalController => survivalController;
        public bool HasActiveMission => currentMission != null && state == MissionState.Active;
        public MissionProgressSnapshot Progress => CreateProgressSnapshot();

        public event Action<MissionDefinition> MissionStarted;
        public event Action<MissionDefinition> MissionCompleted;
        public event Action<MissionDefinition> MissionFailed;
        public event Action<MissionDefinition, MissionState> MissionStateChanged;
        public event Action<MissionProgressSnapshot> MissionProgressChanged;

        private void OnEnable()
        {
            SubscribeToZones();
            SubscribeToSurvival();
        }

        private void Start()
        {
            if (startOnPlay && startingMission != null && currentMission == null)
            {
                StartMission(startingMission);
            }
        }

        private void Update()
        {
            TickMission(Time.deltaTime);
        }

        private void OnDisable()
        {
            UnsubscribeFromZones();
            UnsubscribeFromSurvival();
        }

        public bool StartMission(MissionDefinition mission)
        {
            if (mission == null || !mission.IsConfigured)
            {
                Debug.LogError("[Beyond The Beat] MissionManager cannot start an unconfigured mission definition.");
                return false;
            }

            if (mission.ObjectiveType == MissionObjectiveType.ReachAndSurvive && survivalController == null)
            {
                Debug.LogError(
                    "[Beyond The Beat] MissionManager cannot start ReachAndSurvive without a survival controller source.");
                return false;
            }

            currentMission = mission;
            ResetObjectiveProgress();
            SetState(MissionState.Active);
            PublishProgress(true);

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

            if (mission.ObjectiveType == MissionObjectiveType.ReachAndSurvive && survivalController == null)
            {
                Debug.LogWarning(
                    $"[Beyond The Beat] Mission restore cannot activate ReachAndSurvive mission '{missionId}' without a survival source.");
                return false;
            }

            currentMission = mission;
            ResetObjectiveProgress();
            SetState(restoredState);
            PublishProgress(true);

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
            PublishProgress(true);
            MissionFailed?.Invoke(failedMission);

            Debug.Log($"[Beyond The Beat] Mission FAILED: id='{failedMission.MissionId}'.");
            return true;
        }

        public void ClearMission()
        {
            currentMission = null;
            ResetObjectiveProgress();
            SetState(MissionState.Inactive);
            PublishProgress(true);
        }

        public bool TryProcessZoneEntry(ZoneContext zone, GameObject actor)
        {
            if (!HasActiveMission)
            {
                return false;
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ReachLocation)
            {
                return MissionObjectiveEvaluator.IsSatisfied(currentMission, zone, actor, playerActor) &&
                       CompleteActiveMission();
            }

            if (currentMission.ObjectiveType != MissionObjectiveType.ReachAndSurvive ||
                !MissionObjectiveEvaluator.IsTargetZone(currentMission, zone, actor, playerActor))
            {
                return false;
            }

            targetContextActive = true;
            survivalElapsedSeconds = 0f;
            PublishProgress(true);
            return true;
        }

        public bool TryProcessZoneExit(ZoneContext zone, GameObject actor)
        {
            if (!HasActiveMission ||
                currentMission.ObjectiveType != MissionObjectiveType.ReachAndSurvive ||
                !MissionObjectiveEvaluator.IsTargetZone(currentMission, zone, actor, playerActor))
            {
                return false;
            }

            targetContextActive = false;
            survivalElapsedSeconds = 0f;
            PublishProgress(true);
            return true;
        }

        public bool TryProcessSurvivalDepleted()
        {
            if (!HasActiveMission ||
                currentMission.ObjectiveType != MissionObjectiveType.ReachAndSurvive ||
                !targetContextActive ||
                survivalController == null ||
                survivalController.Resource == null ||
                !survivalController.Resource.IsDepleted)
            {
                return false;
            }

            return FailActiveMission();
        }

        public bool TickMission(float deltaTime)
        {
            if (!HasActiveMission ||
                currentMission.ObjectiveType != MissionObjectiveType.ReachAndSurvive ||
                !targetContextActive ||
                deltaTime <= 0f ||
                survivalController == null ||
                !survivalController.IsPressureActive ||
                survivalController.Resource == null)
            {
                return false;
            }

            if (survivalController.Resource.IsDepleted)
            {
                return TryProcessSurvivalDepleted();
            }

            float requiredSeconds = currentMission.SurvivalDurationSeconds;
            survivalElapsedSeconds = Mathf.Min(requiredSeconds, survivalElapsedSeconds + deltaTime);
            PublishProgress(false);

            if (survivalElapsedSeconds + 0.0001f < requiredSeconds)
            {
                return false;
            }

            return CompleteActiveMission();
        }

        private void HandleZoneEntered(ZoneContext zone, GameObject actor)
        {
            TryProcessZoneEntry(zone, actor);
        }

        private void HandleZoneExited(ZoneContext zone, GameObject actor)
        {
            TryProcessZoneExit(zone, actor);
        }

        private void HandleSurvivalPressureChanged(bool active)
        {
            if (HasActiveMission && currentMission.ObjectiveType == MissionObjectiveType.ReachAndSurvive)
            {
                PublishProgress(true);
            }
        }

        private void HandleSurvivalDepleted()
        {
            TryProcessSurvivalDepleted();
        }

        private bool CompleteActiveMission()
        {
            if (!HasActiveMission)
            {
                return false;
            }

            MissionDefinition completedMission = currentMission;
            SetState(MissionState.Completed);
            PublishProgress(true);
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

        private void ResetObjectiveProgress()
        {
            targetContextActive = false;
            survivalElapsedSeconds = 0f;
            lastPublishedSurvivalElapsed = -1f;
        }

        private MissionProgressSnapshot CreateProgressSnapshot()
        {
            MissionObjectiveType objectiveType = currentMission != null
                ? currentMission.ObjectiveType
                : MissionObjectiveType.ReachLocation;
            float requiredSeconds = currentMission != null
                ? currentMission.SurvivalDurationSeconds
                : 0f;

            return new MissionProgressSnapshot(
                objectiveType,
                targetContextActive,
                survivalElapsedSeconds,
                requiredSeconds);
        }

        private void PublishProgress(bool force)
        {
            if (!force &&
                Mathf.Abs(survivalElapsedSeconds - lastPublishedSurvivalElapsed) < ProgressPublishIntervalSeconds)
            {
                return;
            }

            lastPublishedSurvivalElapsed = survivalElapsedSeconds;
            MissionProgressChanged?.Invoke(CreateProgressSnapshot());
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
                zone.ActorExited += HandleZoneExited;
            }
        }

        private void UnsubscribeFromZones()
        {
            foreach (ZoneContext zone in subscribedZones)
            {
                if (zone != null)
                {
                    zone.ActorEntered -= HandleZoneEntered;
                    zone.ActorExited -= HandleZoneExited;
                }
            }

            subscribedZones.Clear();
        }

        private void SubscribeToSurvival()
        {
            if (survivalController == null)
            {
                return;
            }

            survivalController.PressureChanged -= HandleSurvivalPressureChanged;
            survivalController.PressureChanged += HandleSurvivalPressureChanged;

            if (survivalController.Resource != null)
            {
                survivalController.Resource.Depleted -= HandleSurvivalDepleted;
                survivalController.Resource.Depleted += HandleSurvivalDepleted;
            }
        }

        private void UnsubscribeFromSurvival()
        {
            if (survivalController == null)
            {
                return;
            }

            survivalController.PressureChanged -= HandleSurvivalPressureChanged;
            if (survivalController.Resource != null)
            {
                survivalController.Resource.Depleted -= HandleSurvivalDepleted;
            }
        }
    }
}
