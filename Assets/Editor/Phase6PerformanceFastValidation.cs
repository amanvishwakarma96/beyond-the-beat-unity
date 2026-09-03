using System;
using System.Reflection;
using BeyondTheBeat.Performance;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase6PerformanceFastValidation
    {
        public static void Validate()
        {
            // Reuse the latest Phase 5 gameplay contracts, but do not rerun the historical
            // Phase 5 workflow-name assertions after CI ownership has advanced to Phase 6.
            Phase5ExplorationFastValidation.Validate();

            MobilePerformanceBudget budget = ScriptableObject.CreateInstance<MobilePerformanceBudget>();
            try
            {
                SerializedObject serialized = new SerializedObject(budget);
                SetInt(serialized, "baselineTargetFps", 30);
                SetInt(serialized, "stretchTargetFps", 60);
                SetFloat(serialized, "warningFrameTimeMilliseconds", 37f);
                SetInt(serialized, "warningAllocatedMemoryMb", 1024);
                SetInt(serialized, "maximumApkSizeMb", 200);
                SetFloat(serialized, "sampleIntervalSeconds", 1f);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                MobilePerformanceSnapshot healthy = MobilePerformanceMonitor.EvaluateSnapshot(
                    budget, 32f, 31.25f, 500L * 1024L * 1024L);
                MobilePerformanceSnapshot degraded = MobilePerformanceMonitor.EvaluateSnapshot(
                    budget, 22f, 45f, 1300L * 1024L * 1024L);

                bool thresholdsPass = budget.IsConfigured &&
                                      healthy.IsWithinBudget &&
                                      degraded.WarningFlags == (
                                          PerformanceWarningFlags.LowFps |
                                          PerformanceWarningFlags.HighFrameTime |
                                          PerformanceWarningFlags.HighAllocatedMemory);

                bool overlayNoUpdate = typeof(PerformanceDiagnosticsOverlay).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;
                bool monitorHasSamplingLoop = typeof(MobilePerformanceMonitor).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null;

                if (!thresholdsPass || !overlayNoUpdate || !monitorHasSamplingLoop)
                {
                    throw new InvalidOperationException(
                        $"Phase 6 fast performance validation failed: thresholds={thresholdsPass}, overlayNoUpdate={overlayNoUpdate}, monitorUpdate={monitorHasSamplingLoop}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(budget);
            }

            Debug.Log(
                "[Beyond The Beat] FAST PR VALIDATION PASS: Phase 5 gameplay contracts plus Phase 6 30/60 FPS budget, frame-time/memory classification and sampled diagnostics architecture passed without scene regeneration or APK packaging.");
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property '{propertyName}'.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property '{propertyName}'.");
            property.floatValue = value;
        }
    }
}
