using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.Missions
{
    internal sealed class MissionProgressTracker
    {
        private const float PublishIntervalSeconds = 0.25f;
        private readonly HashSet<string> visitedExplorationZoneIds =
            new HashSet<string>(StringComparer.Ordinal);

        private bool targetContextActive;
        private float survivalElapsedSeconds;
        private bool puzzleSolved;
        private float lastPublishedSurvivalElapsed = -1f;

        public bool TargetContextActive => targetContextActive;
        public float SurvivalElapsedSeconds => survivalElapsedSeconds;
        public bool PuzzleSolved => puzzleSolved;
        public int ExplorationVisitedCount => visitedExplorationZoneIds.Count;

        public void Reset()
        {
            targetContextActive = false;
            survivalElapsedSeconds = 0f;
            puzzleSolved = false;
            visitedExplorationZoneIds.Clear();
            lastPublishedSurvivalElapsed = -1f;
        }

        public void SetPuzzleSolved(bool solved)
        {
            puzzleSolved = solved;
        }

        public void EnterTargetContext(bool resetSurvival)
        {
            targetContextActive = true;
            if (resetSurvival)
            {
                survivalElapsedSeconds = 0f;
            }
        }

        public void ExitTargetContext(bool resetSurvival)
        {
            targetContextActive = false;
            if (resetSurvival)
            {
                survivalElapsedSeconds = 0f;
            }
        }

        public void RestoreSurvival(bool active, float elapsedSeconds, float requiredSeconds)
        {
            targetContextActive = active;
            survivalElapsedSeconds = Mathf.Clamp(elapsedSeconds, 0f, requiredSeconds);
            lastPublishedSurvivalElapsed = -1f;
        }

        public bool RestoreExploration(MissionDefinition mission, IEnumerable<string> zoneIds)
        {
            visitedExplorationZoneIds.Clear();
            if (zoneIds != null)
            {
                foreach (string zoneId in zoneIds)
                {
                    if (!string.IsNullOrWhiteSpace(zoneId) && mission.IsExplorationZone(zoneId))
                    {
                        visitedExplorationZoneIds.Add(zoneId);
                    }
                }
            }

            return visitedExplorationZoneIds.Count >= mission.ExplorationZoneCount;
        }

        public bool TryVisitExplorationZone(string zoneId)
        {
            return !string.IsNullOrWhiteSpace(zoneId) && visitedExplorationZoneIds.Add(zoneId);
        }

        public string[] GetVisitedExplorationZoneIds()
        {
            if (visitedExplorationZoneIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] values = new string[visitedExplorationZoneIds.Count];
            visitedExplorationZoneIds.CopyTo(values);
            Array.Sort(values, StringComparer.Ordinal);
            return values;
        }

        public bool TickSurvival(float deltaTime, float requiredSeconds)
        {
            survivalElapsedSeconds = Mathf.Min(requiredSeconds, survivalElapsedSeconds + deltaTime);
            return survivalElapsedSeconds + 0.0001f >= requiredSeconds;
        }

        public MissionProgressSnapshot CreateSnapshot(MissionDefinition mission)
        {
            MissionObjectiveType objectiveType = mission != null
                ? mission.ObjectiveType
                : MissionObjectiveType.ReachLocation;
            float requiredSeconds = mission != null ? mission.SurvivalDurationSeconds : 0f;
            int explorationRequiredCount = mission != null &&
                                           mission.ObjectiveType == MissionObjectiveType.ExploreLocations
                ? mission.ExplorationZoneCount
                : 0;

            return new MissionProgressSnapshot(
                objectiveType,
                targetContextActive,
                survivalElapsedSeconds,
                requiredSeconds,
                puzzleSolved,
                visitedExplorationZoneIds.Count,
                explorationRequiredCount);
        }

        public bool ShouldPublish(bool force)
        {
            if (!force &&
                Mathf.Abs(survivalElapsedSeconds - lastPublishedSurvivalElapsed) < PublishIntervalSeconds)
            {
                return false;
            }

            lastPublishedSurvivalElapsed = survivalElapsedSeconds;
            return true;
        }
    }
}
