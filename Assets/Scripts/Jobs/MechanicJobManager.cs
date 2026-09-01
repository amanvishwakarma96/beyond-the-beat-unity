using System;
using BeyondTheBeat.Economy;
using BeyondTheBeat.Interaction;
using UnityEngine;

namespace BeyondTheBeat.Jobs
{
    public enum MechanicJobState
    {
        Inactive = 0,
        Active = 1,
        Completed = 2
    }

    public sealed class MechanicJobManager : MonoBehaviour
    {
        [Header("Job Data")]
        [SerializeField] private MechanicJobDefinition startingJob;
        [SerializeField] private bool startStartingJobOnPlay = true;

        [Header("Sources")]
        [SerializeField] private RepairStation repairStation;
        [SerializeField] private RepairableState target;
        [SerializeField] private CreditWallet wallet;

        private MechanicJobDefinition currentJob;
        private MechanicJobState state = MechanicJobState.Inactive;
        private bool rewardPaid;

        public MechanicJobDefinition StartingJob => startingJob;
        public MechanicJobDefinition CurrentJob => currentJob;
        public MechanicJobState State => state;
        public bool HasActiveJob => state == MechanicJobState.Active && currentJob != null;
        public RepairStation RepairStation => repairStation;
        public RepairableState Target => target;
        public CreditWallet Wallet => wallet;

        public event Action<MechanicJobManager, MechanicJobState> StateChanged;
        public event Action<MechanicJobManager, MechanicJobDefinition, int> JobCompleted;

        private void OnEnable()
        {
            RebindSources();
        }

        private void Start()
        {
            if (startStartingJobOnPlay && startingJob != null)
            {
                StartJob(startingJob);
            }
        }

        private void OnDisable()
        {
            UnbindSources();
        }

        public void RebindSources()
        {
            UnbindSources();
            if (repairStation != null)
            {
                repairStation.RepairCompleted += HandleRepairCompleted;
            }
        }

        public bool CanStartJob(MechanicJobDefinition definition)
        {
            return definition != null &&
                   definition.IsConfigured &&
                   state != MechanicJobState.Active &&
                   repairStation != null &&
                   target != null &&
                   wallet != null &&
                   repairStation.Target == target &&
                   target.NeedsRepair &&
                   string.Equals(target.RepairableId, definition.TargetRepairableId, StringComparison.Ordinal);
        }

        public bool StartJob(MechanicJobDefinition definition)
        {
            if (!CanStartJob(definition))
            {
                return false;
            }

            currentJob = definition;
            rewardPaid = false;
            SetState(MechanicJobState.Active);
            return true;
        }

        public void ClearJob()
        {
            currentJob = null;
            rewardPaid = false;
            SetState(MechanicJobState.Inactive);
        }

        private void HandleRepairCompleted(
            RepairStation station,
            RepairableState completedTarget,
            GameObject actor,
            int repairCount)
        {
            if (!HasActiveJob ||
                rewardPaid ||
                station != repairStation ||
                completedTarget != target ||
                currentJob == null ||
                !string.Equals(completedTarget.RepairableId, currentJob.TargetRepairableId, StringComparison.Ordinal))
            {
                return;
            }

            int reward = currentJob.RewardCredits;
            if (!wallet.AddCredits(reward))
            {
                return;
            }

            rewardPaid = true;
            MechanicJobDefinition completedJob = currentJob;
            SetState(MechanicJobState.Completed);
            JobCompleted?.Invoke(this, completedJob, reward);
        }

        private void SetState(MechanicJobState next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            StateChanged?.Invoke(this, state);
        }

        private void UnbindSources()
        {
            if (repairStation != null)
            {
                repairStation.RepairCompleted -= HandleRepairCompleted;
            }
        }
    }
}
