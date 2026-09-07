using System;
using System.IO;
using System.Reflection;
using BeyondTheBeat.Performance;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase6PerformanceFastValidation
    {
        private const string FastWorkflowPath = ".github/workflows/fast-current-milestone-validation.yml";
        private const string FullWorkflowPath = ".github/workflows/phase2-forest-foundation.yml";
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_PERFORMANCE_FOUNDATION.md";

        public static void Validate()
        {
            // Reuse the latest Phase 5 gameplay contracts, but do not rerun the historical
            // Phase 5 workflow-name assertions after CI ownership has advanced to Phase 6.
            Phase5ExplorationFastValidation.Validate();
            Phase6MobileQualityFastValidation.ValidateQualityOnly();
            Phase6BuildSizeFastValidation.ValidateBuildSizeOnly();
            Phase6TutorialFastValidation.ValidateTutorialOnly();

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
                bool repositoryPass = ValidateRepositoryContracts();

                if (!thresholdsPass || !overlayNoUpdate || !monitorHasSamplingLoop || !repositoryPass)
                {
                    throw new InvalidOperationException(
                        $"Phase 6 fast performance validation failed: thresholds={thresholdsPass}, overlayNoUpdate={overlayNoUpdate}, monitorUpdate={monitorHasSamplingLoop}, repository={repositoryPass}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(budget);
            }

            Debug.Log(
                "[Beyond The Beat] FAST PR VALIDATION PASS: Phase 5 gameplay plus Phase 6 performance, render-quality, build-size/stripping and tutorial/onboarding contracts passed without scene regeneration or APK packaging.");
        }

        private static bool ValidateRepositoryContracts()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            string fastPath = Path.Combine(root, FastWorkflowPath.Replace('/', Path.DirectorySeparatorChar));
            string fullPath = Path.Combine(root, FullWorkflowPath.Replace('/', Path.DirectorySeparatorChar));
            string docPath = Path.Combine(root, ValidationDocPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fastPath) || !File.Exists(fullPath) || !File.Exists(docPath))
            {
                return false;
            }

            string fast = File.ReadAllText(fastPath);
            string full = File.ReadAllText(fullPath);
            string doc = File.ReadAllText(docPath);

            return fast.Contains("BeyondTheBeat.Editor.Phase6PerformanceFastValidation.Validate", StringComparison.Ordinal) &&
                   fast.Contains("pull_request:", StringComparison.Ordinal) &&
                   !fast.Contains("androidExportType: androidPackage", StringComparison.Ordinal) &&
                   full.Contains("BeyondTheBeat.Editor.Phase6BuildAutomation.BuildAndroid", StringComparison.Ordinal) &&
                   full.Contains("push:", StringComparison.Ordinal) &&
                   full.Contains("- main", StringComparison.Ordinal) &&
                   full.Contains("maximumApkSizeMb", StringComparison.Ordinal) &&
                   full.Contains("APK size", StringComparison.Ordinal) &&
                   full.Contains("TEST-THIS-BUILD-${GITHUB_RUN_NUMBER}", StringComparison.Ordinal) &&
                   doc.Contains("CI GREEN IS NOT DEVICE PERFORMANCE SIGN-OFF", StringComparison.Ordinal);
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
