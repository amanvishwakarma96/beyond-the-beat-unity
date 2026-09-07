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
            float survivalRequiredSeconds,
            bool puzzleSolved)
            : this(
                objectiveType,
                targetContextActive,
                survivalElapsedSeconds,
                survivalRequiredSeconds,
                puzzleSolved,
                0,
                0)
        {
        }

        public MissionProgressSnapshot(
            MissionObjectiveType objectiveType,
            bool targetContextActive,
            float survivalElapsedSeconds,
            float survivalRequiredSeconds,
            bool puzzleSolved,
            int explorationVisitedCount,
            int explorationRequiredCount)
        {
            ObjectiveType = objectiveType;
            TargetContextActive = targetContextActive;
            SurvivalElapsedSeconds = Mathf.Max(0f, survivalElapsedSeconds);
            SurvivalRequiredSeconds = Mathf.Max(0f, survivalRequiredSeconds);
            PuzzleSolved = puzzleSolved;
            ExplorationVisitedCount = Mathf.Max(0, explorationVisitedCount);
            ExplorationRequiredCount = Mathf.Max(0, explorationRequiredCount);
        }

        public MissionObjectiveType ObjectiveType { get; }
        public bool TargetContextActive { get; }
        public float SurvivalElapsedSeconds { get; }
        public float SurvivalRequiredSeconds { get; }
        public bool PuzzleSolved { get; }
        public int ExplorationVisitedCount { get; }
        public int ExplorationRequiredCount { get; }

        public float NormalizedProgress
        {
            get
            {
                if (ObjectiveType == MissionObjectiveType.ReachAndSolve)
                {
                    float progress = 0f;
                    if (TargetContextActive) progress += 0.5f;
                    if (PuzzleSolved) progress += 0.5f;
                    return progress;
                }

                if (ObjectiveType == MissionObjectiveType.ExploreLocations)
                {
                    return ExplorationRequiredCount > 0
                        ? Mathf.Clamp01((float)ExplorationVisitedCount / ExplorationRequiredCount)
                        : 0f;
                }

                return SurvivalRequiredSeconds > 0f
                    ? Mathf.Clamp01(SurvivalElapsedSeconds / SurvivalRequiredSeconds)
                    : 0f;
            }
        }
    }
}
