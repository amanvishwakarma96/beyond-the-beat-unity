using UnityEngine;

namespace BeyondTheBeat.Performance
{
    [CreateAssetMenu(menuName = "Beyond The Beat/Performance/Mobile Performance Budget", fileName = "MobilePerformanceBudget")]
    public sealed class MobilePerformanceBudget : ScriptableObject
    {
        [Header("Frame rate")]
        [SerializeField, Min(1)] private int baselineTargetFps = 30;
        [SerializeField, Min(1)] private int stretchTargetFps = 60;
        [SerializeField, Min(1f)] private float warningFrameTimeMilliseconds = 37f;

        [Header("Memory / package")]
        [SerializeField, Min(64)] private int warningAllocatedMemoryMb = 1024;
        [SerializeField, Min(32)] private int maximumApkSizeMb = 200;

        [Header("Sampling")]
        [SerializeField, Range(0.25f, 5f)] private float sampleIntervalSeconds = 1f;

        public int BaselineTargetFps => baselineTargetFps;
        public int StretchTargetFps => stretchTargetFps;
        public float WarningFrameTimeMilliseconds => warningFrameTimeMilliseconds;
        public int WarningAllocatedMemoryMb => warningAllocatedMemoryMb;
        public int MaximumApkSizeMb => maximumApkSizeMb;
        public float SampleIntervalSeconds => sampleIntervalSeconds;

        public bool IsConfigured =>
            baselineTargetFps >= 30 &&
            stretchTargetFps >= baselineTargetFps &&
            warningFrameTimeMilliseconds > 0f &&
            warningAllocatedMemoryMb > 0 &&
            maximumApkSizeMb > 0 &&
            sampleIntervalSeconds >= 0.25f;
    }
}
