using System;
using System.IO;
using BeyondTheBeat.Performance;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase6BuildAutomation
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string DiagnosticsRelativePath = "build/phase6-ci-diagnostics.log";

        public static void BuildAndroid()
        {
            InitializeDiagnostics();
            try
            {
                InputBackendBuildGuard.EnsureBothInputBackends();
                AppendDiagnostic("Android input backend guard PASS.");

                // Rebuild and validate the latest Phase 5 gameplay slice without re-running the
                // historical Phase 5 workflow-name assertions after CI ownership advances to Phase 6.
                Phase5BuildAutomation.PrepareMobileSwim();
                Phase5ExplorationMissionBuilder.BuildExplorationMission();
                Phase5ExplorationMissionBuilder.ValidateExplorationMissionOrThrow();
                Phase5OceanBuilder.ValidateOceanFoundationOrThrow();
                Phase5SwimBuilder.ValidateSwimDiveFoundationOrThrow();
                Phase5MobileSwimBuilder.ValidateMobileSwimCameraIntegrationOrThrow();
                AppendDiagnostic("Phase 5 Ocean/Swim/Camera/Exploration prerequisite validators PASS (automated only).");

                Phase6PerformanceBuilder.BuildPerformanceFoundation();
                Phase6PerformanceBuilder.ValidatePerformanceFoundationOrThrow();
                AppendDiagnostic("Phase 6 performance budget/monitor/overlay validation PASS.");

                Phase6MobileQualityBuilder.BuildMobileQualityOptimization();
                Phase6MobileQualityBuilder.ValidateMobileQualityOptimizationOrThrow();
                AppendDiagnostic("Phase 6 mobile render-quality optimization validation PASS.");

                MobileBuildOptimizationProfile buildProfile = Phase6BuildSizeBuilder.PrepareAndValidateOrThrow();
                AppendDiagnostic(
                    $"Phase 6 build-size optimization PASS. stripEngine={buildProfile.StripEngineCode}, " +
                    $"managed={buildProfile.ManagedStripping}, lz4hc={buildProfile.UseLz4HcCompression}, " +
                    $"architectures={PlayerSettings.Android.targetArchitectures}.");

                EnsureSceneBuildSettings();
                BuildDevelopmentAndroidApk(buildProfile);
                AppendDiagnostic(
                    "BuildAndroid PASS. Phase 6 performance + render-quality + build-size optimization is packaged; " +
                    "physical install/FPS/thermal/battery validation remains required.");
            }
            catch (Exception exception)
            {
                AppendDiagnostic("BuildAndroid FATAL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureSceneBuildSettings()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Phase 6 build requires scene '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildDevelopmentAndroidApk(MobileBuildOptimizationProfile buildProfile)
        {
            string outputPath = GetCommandLineArgument("customBuildPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(
                    Path.Combine("build", "Android", "BeyondTheBeat-Phase6-size-local.apk"));
            }

            if (!string.Equals(Path.GetExtension(outputPath), ".apk", StringComparison.OrdinalIgnoreCase))
            {
                outputPath += ".apk";
            }

            string directory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Unable to resolve APK output directory from '{outputPath}'.");
            }

            Directory.CreateDirectory(directory);
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = Phase6BuildSizeBuilder.GetBuildOptions(buildProfile, BuildOptions.Development)
            };

            AppendDiagnostic(
                $"Android BuildPipeline START. output={outputPath}, options={options.options}, " +
                $"stripEngine={PlayerSettings.stripEngineCode}, " +
                $"managed={PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android)}, " +
                $"architectures={PlayerSettings.Android.targetArchitectures}.");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Phase6BuildSizeBuilder.WriteBuildSizeReport(report, buildProfile);

            BuildSummary summary = report.summary;
            AppendDiagnostic(
                $"Android BuildPipeline RESULT={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}, " +
                $"size={summary.totalSize}, duration={summary.totalTime}. " +
                $"Detailed report={Phase6BuildSizeBuilder.ReportRelativePath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Phase 6 Android build failed with result {summary.result}. Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException(
                    "Unity reported a successful Phase 6 Android build but the APK was not found.", outputPath);
            }
        }

        private static string GetCommandLineArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            string prefix = "-" + name;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1 < args.Length ? args[i + 1] : string.Empty;
                }
            }
            return string.Empty;
        }

        private static void InitializeDiagnostics()
        {
            string path = ResolveDiagnosticsPath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, $"[{DateTime.UtcNow:O}] Phase 6 diagnostics initialized.\n");
        }

        private static void AppendDiagnostic(string message)
        {
            string path = ResolveDiagnosticsPath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.AppendAllText(path, $"[{DateTime.UtcNow:O}] {message}\n");
            Debug.Log("[Beyond The Beat] " + message);
        }

        private static string ResolveDiagnosticsPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return string.IsNullOrWhiteSpace(projectRoot)
                ? DiagnosticsRelativePath
                : Path.Combine(projectRoot, DiagnosticsRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
