using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace BeyondTheBeat.Performance
{
    [Flags]
    public enum PerformanceWarningFlags
    {
        None = 0,
        LowFps = 1 << 0,
        HighFrameTime = 1 << 1,
        HighAllocatedMemory = 1 << 2
    }

    public readonly struct MobilePerformanceSnapshot
    {
        public MobilePerformanceSnapshot(
            float averageFps,
            float averageFrameTimeMilliseconds,
            long allocatedMemoryBytes,
            PerformanceWarningFlags warningFlags)
        {
            AverageFps = Mathf.Max(0f, averageFps);
            AverageFrameTimeMilliseconds = Mathf.Max(0f, averageFrameTimeMilliseconds);
            AllocatedMemoryBytes = Math.Max(0L, allocatedMemoryBytes);
            WarningFlags = warningFlags;
        }

        public float AverageFps { get; }
        public float AverageFrameTimeMilliseconds { get; }
        public long AllocatedMemoryBytes { get; }
        public float AllocatedMemoryMb => AllocatedMemoryBytes / (1024f * 1024f);
        public PerformanceWarningFlags WarningFlags { get; }
        public bool IsWithinBudget => WarningFlags == PerformanceWarningFlags.None;
    }

    [DisallowMultipleComponent]
    public sealed class MobilePerformanceMonitor : MonoBehaviour
    {
        [SerializeField] private MobilePerformanceBudget budget;
        [SerializeField] private bool applyFrameRatePolicy = true;

        private int sampledFrames;
        private float sampledSeconds;

        public MobilePerformanceBudget Budget => budget;
        public bool ApplyFrameRatePolicy => applyFrameRatePolicy;
        public MobilePerformanceSnapshot LatestSnapshot { get; private set; }

        public event Action<MobilePerformanceSnapshot> Sampled;

        private void Awake()
        {
            if (budget == null || !budget.IsConfigured)
            {
                Debug.LogError("[Beyond The Beat] MobilePerformanceMonitor requires a configured MobilePerformanceBudget.");
                enabled = false;
                return;
            }

            if (applyFrameRatePolicy)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = budget.BaselineTargetFps;
            }
        }

        private void Update()
        {
            sampledFrames++;
            sampledSeconds += Time.unscaledDeltaTime;

            if (sampledSeconds + 0.0001f < budget.SampleIntervalSeconds)
            {
                return;
            }

            float averageFps = sampledSeconds > 0f ? sampledFrames / sampledSeconds : 0f;
            float averageFrameMilliseconds = sampledFrames > 0
                ? sampledSeconds * 1000f / sampledFrames
                : 0f;
            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();

            LatestSnapshot = EvaluateSnapshot(budget, averageFps, averageFrameMilliseconds, allocatedBytes);
            Sampled?.Invoke(LatestSnapshot);

            sampledFrames = 0;
            sampledSeconds = 0f;
        }

        public static MobilePerformanceSnapshot EvaluateSnapshot(
            MobilePerformanceBudget performanceBudget,
            float averageFps,
            float averageFrameTimeMilliseconds,
            long allocatedMemoryBytes)
        {
            if (performanceBudget == null || !performanceBudget.IsConfigured)
            {
                throw new ArgumentException("A configured MobilePerformanceBudget is required.", nameof(performanceBudget));
            }

            PerformanceWarningFlags warnings = PerformanceWarningFlags.None;
            if (averageFps + 0.0001f < performanceBudget.BaselineTargetFps)
            {
                warnings |= PerformanceWarningFlags.LowFps;
            }

            if (averageFrameTimeMilliseconds > performanceBudget.WarningFrameTimeMilliseconds)
            {
                warnings |= PerformanceWarningFlags.HighFrameTime;
            }

            long memoryLimitBytes = (long)performanceBudget.WarningAllocatedMemoryMb * 1024L * 1024L;
            if (allocatedMemoryBytes > memoryLimitBytes)
            {
                warnings |= PerformanceWarningFlags.HighAllocatedMemory;
            }

            return new MobilePerformanceSnapshot(
                averageFps,
                averageFrameTimeMilliseconds,
                allocatedMemoryBytes,
                warnings);
        }
    }
}
