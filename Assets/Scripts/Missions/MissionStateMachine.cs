namespace BeyondTheBeat.Missions
{
    internal sealed class MissionStateMachine
    {
        public MissionState State { get; private set; } = MissionState.Inactive;

        public bool SetState(MissionState next)
        {
            if (State == next)
            {
                return false;
            }

            State = next;
            return true;
        }
    }
}
