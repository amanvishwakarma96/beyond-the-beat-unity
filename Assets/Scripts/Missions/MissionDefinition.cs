using System;
using UnityEngine;

namespace BeyondTheBeat.Missions
{
    public enum MissionObjectiveType
    {
        ReachLocation = 0,
        ReachAndSurvive = 1,
        ReachAndSolve = 2
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

        public string MissionId => missionId;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionObjectiveType ObjectiveType => objectiveType;
        public string TargetZoneId => targetZoneId;
        public float SurvivalDurationSeconds => survivalDurationSeconds;
        public string TargetPuzzleId => targetPuzzleId;

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
                    default:
                        return false;
                }
            }
        }
    }
}
