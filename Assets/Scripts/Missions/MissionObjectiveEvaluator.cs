using System;
using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Missions
{
    public static class MissionObjectiveEvaluator
    {
        public static bool IsSatisfied(
            MissionDefinition mission,
            ZoneContext zone,
            GameObject actor,
            GameObject expectedActor)
        {
            if (mission == null || !mission.IsConfigured || zone == null || actor == null || expectedActor == null)
            {
                return false;
            }

            if (actor != expectedActor)
            {
                return false;
            }

            switch (mission.ObjectiveType)
            {
                case MissionObjectiveType.ReachLocation:
                    return string.Equals(
                        zone.ZoneId,
                        mission.TargetZoneId,
                        StringComparison.Ordinal);
                default:
                    return false;
            }
        }
    }
}
