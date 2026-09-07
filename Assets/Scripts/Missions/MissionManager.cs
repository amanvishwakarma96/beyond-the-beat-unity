using System;
using System.Collections.Generic;
using BeyondTheBeat.Puzzles;
using BeyondTheBeat.Survival;
using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionManager : MonoBehaviour
    {
        [Header("Mission")]
        [SerializeField] private MissionDefinition startingMission;
        [SerializeField] private bool startOnPlay = true;

        [Header("World")]
        [SerializeField] private GameObject playerActor;
        [SerializeField] private ZoneContext[] observedZones = Array.Empty<ZoneContext>();

        [Header("Optional objective sources")]
        [SerializeField] private ForestSurvivalController survivalController;
        [SerializeField] private PuzzleStateController[] observedPuzzles = Array.Empty<PuzzleStateController>();

        private readonly HashSet<ZoneContext> subscribedZones = new HashSet<ZoneContext>();
        private readonly Dictionary<PuzzleStateController, Action<bool>> puzzleStateHandlers =
            new Dictionary<PuzzleStateController, Action<bool>>();
        private readonly MissionStateMachine stateMachine = new MissionStateMachine();
        private readonly MissionProgressTracker progressTracker = new MissionProgressTracker();

        private MissionDefinition currentMission;

        public MissionDefinition StartingMission => startingMission;
        public MissionDefinition CurrentMission => currentMission;
        public string CurrentMissionId => currentMission != null ? currentMission.MissionId : string.Empty;
        public MissionState State => stateMachine.State;
        public GameObject PlayerActor => playerActor;
        public int ObservedZoneCount => observedZones != null ? observedZones.Length : 0;
        public int ObservedPuzzleCount => observedPuzzles != null ? observedPuzzles.Length : 0;
        public ForestSurvivalController SurvivalController => survivalController;
        public bool HasActiveMission => currentMission != null && State == MissionState.Active;
        public int ExplorationVisitedCount => progressTracker.ExplorationVisitedCount;
        public MissionProgressSnapshot Progress => progressTracker.CreateSnapshot(currentMission);

        public event Action<MissionDefinition> MissionStarted;
        public event Action<MissionDefinition> MissionCompleted;
        public event Action<MissionDefinition> MissionFailed;
        public event Action<MissionDefinition, MissionState> MissionStateChanged;
        public event Action<MissionProgressSnapshot> MissionProgressChanged;

        private void OnEnable()
        {
            SubscribeToZones();
            SubscribeToSurvival();
            SubscribeToPuzzles();
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
            UnsubscribeFromPuzzles();
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

            if (mission.ObjectiveType == MissionObjectiveType.ExploreLocations && !HasObservedExplorationZones(mission))
            {
                Debug.LogError(
                    $"[Beyond The Beat] MissionManager cannot start exploration mission '{mission.MissionId}' because one or more configured checkpoints are not observed.");
                return false;
            }

            PuzzleStateController puzzleSource = null;
            if (mission.ObjectiveType == MissionObjectiveType.ReachAndSolve)
            {
                puzzleSource = ResolvePuzzleSource(mission.TargetPuzzleId);
                if (puzzleSource == null)
                {
                    Debug.LogError(
                        $"[Beyond The Beat] MissionManager cannot start ReachAndSolve because puzzle '{mission.TargetPuzzleId}' is not observed.");
                    return false;
                }
            }

            currentMission = mission;
            progressTracker.Reset();
            if (puzzleSource != null)
            {
                progressTracker.SetPuzzleSolved(puzzleSource.IsSolved);
            }

            SetState(MissionState.Active);
            PublishProgress(true);

            Debug.Log(
                $"[Beyond The Beat] Mission STARTED: id='{mission.MissionId}', objective={mission.ObjectiveType}, " +
                $"targetZone='{mission.TargetZoneId}', targetPuzzle='{mission.TargetPuzzleId}', " +
                $"explorationCheckpoints={mission.ExplorationZoneCount}.");

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

            if (mission.ObjectiveType == MissionObjectiveType.ExploreLocations && !HasObservedExplorationZones(mission))
            {
                Debug.LogWarning(
                    $"[Beyond The Beat] Mission restore cannot activate exploration mission '{missionId}' because one or more configured checkpoints are not observed.");
                return false;
            }

            PuzzleStateController puzzleSource = null;
            if (mission.ObjectiveType == MissionObjectiveType.ReachAndSolve)
            {
                puzzleSource = ResolvePuzzleSource(mission.TargetPuzzleId);
                if (puzzleSource == null)
                {
                    Debug.LogWarning(
                        $"[Beyond The Beat] Mission restore cannot activate ReachAndSolve mission '{missionId}' without puzzle '{mission.TargetPuzzleId}'.");
                    return false;
                }
            }

            currentMission = mission;
            progressTracker.Reset();
            if (puzzleSource != null)
            {
                progressTracker.SetPuzzleSolved(puzzleSource.IsSolved);
            }

            SetState(restoredState);
            PublishProgress(true);

            Debug.Log(
                $"[Beyond The Beat] Mission state RESTORED: id='{mission.MissionId}', state={restoredState}.");
            return true;
        }

        public bool RestoreObjectiveProgress(bool restoredTargetContextActive, float restoredSurvivalElapsedSeconds)
        {
            if (!HasActiveMission || currentMission.ObjectiveType != MissionObjectiveType.ReachAndSurvive)
            {
                return false;
            }

            progressTracker.RestoreSurvival(
                restoredTargetContextActive,
                restoredSurvivalElapsedSeconds,
                currentMission.SurvivalDurationSeconds);
            PublishProgress(true);
            return true;
        }

        public bool RestoreExplorationProgress(IEnumerable<string> restoredZoneIds)
        {
            if (!HasActiveMission || currentMission.ObjectiveType != MissionObjectiveType.ExploreLocations)
            {
                return false;
            }

            bool completed = progressTracker.RestoreExploration(currentMission, restoredZoneIds);
            PublishProgress(true);
            if (completed)
            {
                CompleteActiveMission();
            }

            return true;
        }

        public string[] GetVisitedExplorationZoneIds()
        {
            return progressTracker.GetVisitedExplorationZoneIds();
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
            progressTracker.Reset();
            SetState(MissionState.Inactive);
            PublishProgress(true);
        }

        public bool TryProcessZoneEntry(ZoneContext zone, GameObject actor)
        {
            if (!HasActiveMission)
            {
                return false;
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ExploreLocations)
            {
                return TryProcessExplorationCheckpoint(zone, actor);
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ReachLocation)
            {
                return MissionObjectiveEvaluator.IsSatisfied(currentMission, zone, actor, playerActor) &&
                       CompleteActiveMission();
            }

            if (!MissionObjectiveEvaluator.IsTargetZone(currentMission, zone, actor, playerActor))
            {
                return false;
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ReachAndSurvive)
            {
                progressTracker.EnterTargetContext(resetSurvival: true);
                PublishProgress(true);
                return true;
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ReachAndSolve)
            {
                progressTracker.EnterTargetContext(resetSurvival: false);
                PuzzleStateController source = ResolvePuzzleSource(currentMission.TargetPuzzleId);
                progressTracker.SetPuzzleSolved(source != null && source.IsSolved);
                PublishProgress(true);
                return progressTracker.PuzzleSolved ? CompleteActiveMission() : true;
            }

            return false;
        }

        public bool TryProcessZoneExit(ZoneContext zone, GameObject actor)
        {
            if (!HasActiveMission || currentMission.ObjectiveType == MissionObjectiveType.ExploreLocations ||
                !MissionObjectiveEvaluator.IsTargetZone(currentMission, zone, actor, playerActor))
            {
                return false;
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ReachAndSurvive)
            {
                progressTracker.ExitTargetContext(resetSurvival: true);
                PublishProgress(true);
                return true;
            }

            if (currentMission.ObjectiveType == MissionObjectiveType.ReachAndSolve)
            {
                progressTracker.ExitTargetContext(resetSurvival: false);
                PuzzleStateController source = ResolvePuzzleSource(currentMission.TargetPuzzleId);
                progressTracker.SetPuzzleSolved(source != null && source.IsSolved);
                PublishProgress(true);
                return true;
            }

            return false;
        }

        public bool TryProcessPuzzleState(PuzzleStateController puzzle, bool solved)
        {
            if (!HasActiveMission || currentMission.ObjectiveType != MissionObjectiveType.ReachAndSolve || puzzle == null)
            {
                return false;
            }

            PuzzleStateController expectedPuzzle = ResolvePuzzleSource(currentMission.TargetPuzzleId);
            if (expectedPuzzle == null || puzzle != expectedPuzzle)
            {
                return false;
            }

            progressTracker.SetPuzzleSolved(solved);
            PublishProgress(true);

            if (progressTracker.TargetContextActive && progressTracker.PuzzleSolved)
            {
                CompleteActiveMission();
            }

            return true;
        }

        public bool TryProcessSurvivalDepleted()
        {
            if (!HasActiveMission ||
                currentMission.ObjectiveType != MissionObjectiveType.ReachAndSurvive ||
                !progressTracker.TargetContextActive ||
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
                !progressTracker.TargetContextActive ||
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

            bool completed = progressTracker.TickSurvival(deltaTime, currentMission.SurvivalDurationSeconds);
            PublishProgress(false);
            return completed && CompleteActiveMission();
        }

        public void RebindZoneSources()
        {
            UnsubscribeFromZones();
            if (isActiveAndEnabled)
            {
                SubscribeToZones();
            }
        }

        public void RebindPuzzleSources()
        {
            UnsubscribeFromPuzzles();
            if (isActiveAndEnabled)
            {
                SubscribeToPuzzles();
            }
        }

        private bool TryProcessExplorationCheckpoint(ZoneContext zone, GameObject actor)
        {
            if (!MissionObjectiveEvaluator.IsExplorationCheckpoint(currentMission, zone, actor, playerActor))
            {
                return false;
            }

            if (!progressTracker.TryVisitExplorationZone(zone.ZoneId))
            {
                return false;
            }

            PublishProgress(true);
            if (progressTracker.ExplorationVisitedCount >= currentMission.ExplorationZoneCount)
            {
                CompleteActiveMission();
            }

            return true;
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

        private void HandlePuzzleStateChanged(PuzzleStateController puzzle, bool solved)
        {
            TryProcessPuzzleState(puzzle, solved);
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
                $"[Beyond The Beat] Mission COMPLETED: id='{completedMission.MissionId}'. Free roam remains available.");
            return true;
        }

        private MissionDefinition ResolveMissionById(string missionId)
        {
            if (currentMission != null && string.Equals(currentMission.MissionId, missionId, StringComparison.Ordinal))
            {
                return currentMission;
            }

            if (startingMission != null && string.Equals(startingMission.MissionId, missionId, StringComparison.Ordinal))
            {
                return startingMission;
            }

            return null;
        }

        private bool HasObservedExplorationZones(MissionDefinition mission)
        {
            if (mission == null || mission.ObjectiveType != MissionObjectiveType.ExploreLocations || observedZones == null)
            {
                return false;
            }

            IReadOnlyList<string> required = mission.ExplorationZoneIds;
            for (int i = 0; i < required.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < observedZones.Length; j++)
                {
                    ZoneContext zone = observedZones[j];
                    if (zone != null && string.Equals(zone.ZoneId, required[i], StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return required.Count > 0;
        }

        private PuzzleStateController ResolvePuzzleSource(string puzzleId)
        {
            if (observedPuzzles == null || string.IsNullOrWhiteSpace(puzzleId))
            {
                return null;
            }

            for (int i = 0; i < observedPuzzles.Length; i++)
            {
                PuzzleStateController puzzle = observedPuzzles[i];
                if (puzzle != null && puzzle.IsConfigured &&
                    string.Equals(puzzle.PuzzleId, puzzleId, StringComparison.Ordinal))
                {
                    return puzzle;
                }
            }

            return null;
        }

        private void SetState(MissionState newState)
        {
            if (stateMachine.SetState(newState))
            {
                MissionStateChanged?.Invoke(currentMission, State);
            }
        }

        private void PublishProgress(bool force)
        {
            if (progressTracker.ShouldPublish(force))
            {
                MissionProgressChanged?.Invoke(progressTracker.CreateSnapshot(currentMission));
            }
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

        private void SubscribeToPuzzles()
        {
            if (observedPuzzles == null)
            {
                return;
            }

            for (int i = 0; i < observedPuzzles.Length; i++)
            {
                PuzzleStateController puzzle = observedPuzzles[i];
                if (puzzle == null || puzzleStateHandlers.ContainsKey(puzzle))
                {
                    continue;
                }

                PuzzleStateController source = puzzle;
                Action<bool> handler = solved => HandlePuzzleStateChanged(source, solved);
                puzzle.StateChanged += handler;
                puzzleStateHandlers.Add(puzzle, handler);
            }
        }

        private void UnsubscribeFromPuzzles()
        {
            foreach (KeyValuePair<PuzzleStateController, Action<bool>> entry in puzzleStateHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.StateChanged -= entry.Value;
                }
            }

            puzzleStateHandlers.Clear();
        }
    }
}
