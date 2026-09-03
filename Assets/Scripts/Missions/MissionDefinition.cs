using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.Missions
{
    public enum MissionObjectiveType
    {
        ReachLocation = 0,
        ReachAndSurvive = 1,
        ReachAndSolve = 2,
        ExploreLocations = 3
    }

    [CreateAssetMenu(
        fileName = "MissionDefinition",
        menuName = "Beyond The Beat/Missions/Mission Definition")]
    public sealed class MissionDefinition : ScriptableObject
    {
        [SerializeField] private string missionId = "mission";
        [SerializeField] private string displayName = "Mission";
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;
        [SerializeField] private MissionObjectiveType objectiveType = MissionObjectiveType.ReachLocation;
        [SerializeField] private string targetZoneId = string.Empty;
        [SerializeField, Min(0f)] private float survivalDurationSeconds;
        [SerializeField] private string targetPuzzleId = string.Empty;
        [SerializeField] private string[] explorationZoneIds = Array.Empty<string>();

        public string MissionId => missionId;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionObjectiveType ObjectiveType => objectiveType;
        public string TargetZoneId => targetZoneId;
        public float SurvivalDurationSeconds => survivalDurationSeconds;
        public string TargetPuzzleId => targetPuzzleId;
        public IReadOnlyList<string> ExplorationZoneIds => explorationZoneIds ?? Array.Empty<string>();
        public int ExplorationZoneCount => explorationZoneIds != null ? explorationZoneIds.Length : 0;

        public bool IsConfigured
        {
            get
            {
                if (string.IsNullOrWhiteSpace(missionId) || string.IsNullOrWhiteSpace(displayName))
                {
                    return false;
                }

                switch (objectiveType)
                {
                    case MissionObjectiveType.ReachLocation:
                        return !string.IsNullOrWhiteSpace(targetZoneId);
                    case MissionObjectiveType.ReachAndSurvive:
                        return !string.IsNullOrWhiteSpace(targetZoneId) && survivalDurationSeconds > 0f;
                    case MissionObjectiveType.ReachAndSolve:
                        return !string.IsNullOrWhiteSpace(targetZoneId) &&
                               !string.IsNullOrWhiteSpace(targetPuzzleId);
                    case MissionObjectiveType.ExploreLocations:
                        return HasValidExplorationZones();
                    default:
                        return false;
                }
            }
        }

        public bool IsExplorationZone(string zoneId)
        {
            if (objectiveType != MissionObjectiveType.ExploreLocations ||
                string.IsNullOrWhiteSpace(zoneId) ||
                explorationZoneIds == null)
            {
                return false;
            }

            for (int i = 0; i < explorationZoneIds.Length; i++)
            {
                if (string.Equals(explorationZoneIds[i], zoneId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasValidExplorationZones()
        {
            if (explorationZoneIds == null || explorationZoneIds.Length == 0)
            {
                return false;
            }

            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < explorationZoneIds.Length; i++)
            {
                string zoneId = explorationZoneIds[i];
                if (string.IsNullOrWhiteSpace(zoneId) || !unique.Add(zoneId))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
