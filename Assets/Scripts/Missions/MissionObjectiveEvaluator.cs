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
            if (mission == null || mission.ObjectiveType != MissionObjectiveType.ReachLocation)
            {
                return false;
            }

            return IsTargetZone(mission, zone, actor, expectedActor);
        }

        public static bool IsTargetZone(
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

            return string.Equals(
                zone.ZoneId,
                mission.TargetZoneId,
                StringComparison.Ordinal);
        }
    }
}
