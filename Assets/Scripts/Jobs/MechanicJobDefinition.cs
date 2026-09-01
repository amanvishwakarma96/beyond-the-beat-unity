using UnityEngine;

namespace BeyondTheBeat.Jobs
{
    [CreateAssetMenu(fileName = "MechanicJob", menuName = "Beyond The Beat/Jobs/Mechanic Job")]
    public sealed class MechanicJobDefinition : ScriptableObject
    {
        [SerializeField] private string jobId = "prototype-mechanic-job";
        [SerializeField] private string displayName = "Repair the vehicle";
        [SerializeField] private string targetRepairableId = "prototype-vehicle";
        [SerializeField, Min(0)] private int rewardCredits = 125;

        public string JobId => jobId;
        public string DisplayName => displayName;
        public string TargetRepairableId => targetRepairableId;
        public int RewardCredits => Mathf.Max(0, rewardCredits);

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(jobId) &&
            !string.IsNullOrWhiteSpace(displayName) &&
            !string.IsNullOrWhiteSpace(targetRepairableId) &&
            rewardCredits > 0;
    }
}
