using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase5ExplorationBuildAutomation
    {
        private const string ScenePath = Phase5OceanBuilder.Phase5ScenePath;
        private const string DiagnosticsRelativePath = "build/phase5-ci-diagnostics.log";

        public static void BuildAndroid()
        {
            InputBackendBuildGuard.EnsureBothInputBackends();
            Phase5BuildAutomation.PrepareMobileSwim();
            AppendDiagnostic("Phase 5 exploration preparation START.");

            Phase5ExplorationMissionBuilder.BuildExplorationMission();
            AppendDiagnostic("Phase 5 exploration checkpoints/mission/persistence integration completed.");

            Phase5ExplorationMissionBuilder.ValidateExplorationMissionOrThrow();
            AppendDiagnostic("Phase 5 exploration mission validation PASS.");

            Phase5ExitBuilder.ValidateFinalExitIntegrationOrThrow();
            AppendDiagnostic(
                "Phase 5 FINAL exit integration validation PASS. Automated evidence only; physical Android sign-off remains separate.");

            EnsureSceneBuildSettings();
            BuildDevelopmentAndroidApk();
            AppendDiagnostic(
                "BuildAndroid PASS. Ocean + swim/dive + mobile swim/camera + exploration + final exit automated validation passed. Physical Android validation remains required.");
        }

        private static void EnsureSceneBuildSettings()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Phase 5 exploration build requires scene '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildDevelopmentAndroidApk()
        {
            string outputPath = GetCommandLineArgument("customBuildPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(
                    Path.Combine("build", "Android", "BeyondTheBeat-Phase5-exploration-local.apk"));
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
                options = BuildOptions.Development
            };

            AppendDiagnostic($"Android BuildPipeline START. output={outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            AppendDiagnostic(
                $"Android BuildPipeline RESULT={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}, " +
                $"size={summary.totalSize}, duration={summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Phase 5 exploration Android build failed with result {summary.result}. Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException(
                    "Unity reported a successful Phase 5 exploration Android build but the APK was not found.",
                    outputPath);
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

        private static void AppendDiagnostic(string message)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string path = string.IsNullOrWhiteSpace(projectRoot)
                ? DiagnosticsRelativePath
                : Path.Combine(projectRoot, DiagnosticsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.AppendAllText(path, $"[{DateTime.UtcNow:O}] {message}\n");
            Debug.Log("[Beyond The Beat] " + message);
        }
    }
}
