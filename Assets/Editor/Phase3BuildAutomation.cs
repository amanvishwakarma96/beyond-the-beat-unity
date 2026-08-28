using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase3BuildAutomation
    {
        private const string ScenePath = Phase3RestrictedAreaBuilder.Phase3ScenePath;
        private const string AndroidApplicationId = "com.beyondthebeat.mvp";
        private const string DiagnosticsRelativePath = "build/phase3-ci-diagnostics.log";

        public static void BuildAndroid()
        {
            InitializeDiagnostics();

            try
            {
                InputBackendBuildGuard.EnsureBothInputBackends();
                AppendDiagnostic("Android input backend guard PASS: Active Input Handling verified as Both.");
                AppendDiagnostic(
                    $"BuildAndroid START. Unity={Application.unityVersion}, batchMode={Application.isBatchMode}, dataPath={Application.dataPath}");

                PrepareRestrictedAreaInternal();
                EnsurePhase3SceneAndSettings();
                BuildDevelopmentAndroidApk();
                AppendDiagnostic("BuildAndroid PASS.");
            }
            catch (Exception exception)
            {
                AppendDiagnostic("BuildAndroid FATAL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
        }

        public static void PrepareRestrictedArea()
        {
            InitializeDiagnostics();
            PrepareRestrictedAreaInternal();
            EnsurePhase3SceneAndSettings();
            AppendDiagnostic("PrepareRestrictedArea PASS.");
        }

        private static void PrepareRestrictedAreaInternal()
        {
            Debug.Log("[Beyond The Beat] Starting Phase 3 restricted-area foundation CI preparation.");
            AppendDiagnostic("Phase 3 restricted-area foundation preparation START.");

            AppendDiagnostic("Rebuilding and validating the integrated Phase 2 prerequisite scene.");
            Phase2BuildAutomation.PrepareReachAndSurvive();

            Phase3RestrictedAreaBuilder.BuildRestrictedAreaFoundation();
            AppendDiagnostic("Phase 3 restricted-area generation completed.");

            Phase3RestrictedAreaBuilder.ValidateRestrictedAreaFoundationOrThrow();
            AppendDiagnostic("Phase 3 restricted-area structural/behavior validation PASS.");
        }

        private static void EnsurePhase3SceneAndSettings()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException(
                    $"Phase 3 preparation completed without producing the required scene at '{ScenePath}'.");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            PlayerSettings.productName = "Beyond The Beat";
            PlayerSettings.companyName = "Beyond The Beat";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidApplicationId);
            EditorUserBuildSettings.buildAppBundle = false;

            ApplyVersionFromCommandLine();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildDevelopmentAndroidApk()
        {
            string outputPath = GetCommandLineArgument("customBuildPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(
                    Path.Combine("build", "Android", "BeyondTheBeat-Phase3-foundation-local.apk"));
            }

            if (!string.Equals(Path.GetExtension(outputPath), ".apk", StringComparison.OrdinalIgnoreCase))
            {
                outputPath += ".apk";
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException($"Unable to resolve APK output directory from '{outputPath}'.");
            }

            Directory.CreateDirectory(outputDirectory);
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
            Debug.Log($"[Beyond The Beat] Building Phase 3 restricted-area development APK: {outputPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            AppendDiagnostic(
                $"Android BuildPipeline RESULT={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}, " +
                $"size={summary.totalSize}, duration={summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Phase 3 Android build failed with result {summary.result}. " +
                    $"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException(
                    "Unity reported a successful Phase 3 Android build but the APK was not found.",
                    outputPath);
            }

            string stagedPath = StageApkInsideProject(outputPath);
            AppendDiagnostic($"APK staging PASS. source={outputPath}, staged={stagedPath}");
            Debug.Log(
                "[Beyond The Beat] Phase 3 restricted-area Android APK build PASS. " +
                $"Output: {outputPath}, staged output: {stagedPath}, size: {summary.totalSize} bytes, " +
                $"duration: {summary.totalTime}, warnings: {summary.totalWarnings}.");
        }

        private static string StageApkInsideProject(string outputPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve the Unity project root while staging the Phase 3 APK.");
            }

            string apkFileName = Path.GetFileName(outputPath);
            if (string.IsNullOrWhiteSpace(apkFileName))
            {
                throw new InvalidOperationException($"Unable to resolve the APK file name from '{outputPath}'.");
            }

            string stageDirectory = Path.Combine(projectRoot, "build", "Android");
            Directory.CreateDirectory(stageDirectory);
            string stagedPath = Path.Combine(stageDirectory, apkFileName);

            string sourceFullPath = Path.GetFullPath(outputPath);
            string stagedFullPath = Path.GetFullPath(stagedPath);
            if (!string.Equals(sourceFullPath, stagedFullPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourceFullPath, stagedFullPath, true);
            }

            if (!File.Exists(stagedFullPath))
            {
                throw new FileNotFoundException("Unable to stage the Phase 3 APK inside the project workspace.", stagedFullPath);
            }

            return stagedFullPath;
        }

        private static void ApplyVersionFromCommandLine()
        {
            string customVersion = GetCommandLineArgument("customBuildVersion");
            if (!string.IsNullOrWhiteSpace(customVersion))
            {
                PlayerSettings.bundleVersion = customVersion;
            }

            string versionCodeValue = GetCommandLineArgument("androidVersionCode");
            if (int.TryParse(versionCodeValue, out int versionCode) && versionCode > 0)
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
            }
        }

        private static string GetCommandLineArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            string prefix = "-" + name;
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return i + 1 < args.Length ? args[i + 1] : string.Empty;
            }

            return string.Empty;
        }

        private static void InitializeDiagnostics()
        {
            string path = GetDiagnosticsPath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                path,
                $"Beyond The Beat Phase 3 CI diagnostics\nUTC: {DateTime.UtcNow:O}\nUnity: {Application.unityVersion}\n\n");
        }

        private static void AppendDiagnostic(string message)
        {
            string line = $"[{DateTime.UtcNow:O}] {message}\n";
            File.AppendAllText(GetDiagnosticsPath(), line);
            Debug.Log("[Beyond The Beat] " + message);
        }

        private static string GetDiagnosticsPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return DiagnosticsRelativePath;
            }

            return Path.Combine(projectRoot, DiagnosticsRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
