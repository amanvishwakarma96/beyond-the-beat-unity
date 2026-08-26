using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeyondTheBeat.Editor
{
    public static class Phase1BuildAutomation
    {
        private const string ScenePath = Phase1WorldBuilder.Phase1ScenePath;
        private const string AndroidApplicationId = "com.beyondthebeat.mvp";
        private const string DiagnosticsRelativePath = "build/phase1-ci-diagnostics.log";

        private static readonly string[] Phase1BuildSteps =
        {
            "Beyond The Beat/Phase 1/Build MVP World Foundation",
            "Beyond The Beat/Phase 1/Build Reach Location Mission",
            "Beyond The Beat/Phase 1/Build Local Save Resume"
        };

        private static readonly string[] Phase1ValidationSteps =
        {
            "Beyond The Beat/Phase 1/Validate MVP World Foundation",
            "Beyond The Beat/Phase 1/Validate Reach Location Mission",
            "Beyond The Beat/Phase 1/Validate Local Save Resume"
        };

        private static int capturedErrors;
        private static readonly List<string> capturedErrorMessages = new List<string>();

        /// <summary>
        /// Phase 1 GameCI entry point.
        /// Rebuilds Phase 0, derives the Phase 1 world, adds mission + local persistence slices,
        /// runs structural/data validators, then builds a Development Android APK.
        /// </summary>
        public static void BuildAndroid()
        {
            InitializeDiagnostics();
            AppendDiagnostic(
                $"BuildAndroid START. Unity={Application.unityVersion}, batchMode={Application.isBatchMode}, " +
                $"dataPath={Application.dataPath}");

            Application.logMessageReceived += CaptureLog;

            try
            {
                PrepareMvpInternal();
                BuildDevelopmentAndroidApk();
                AppendDiagnostic("BuildAndroid PASS.");
            }
            catch (Exception exception)
            {
                AppendDiagnostic("BuildAndroid FATAL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
            }
        }

        public static void PrepareMvp()
        {
            InitializeDiagnostics();
            Application.logMessageReceived += CaptureLog;

            try
            {
                PrepareMvpInternal();
                AppendDiagnostic("PrepareMvp PASS.");
            }
            catch (Exception exception)
            {
                AppendDiagnostic("PrepareMvp FATAL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
            }
        }

        private static void PrepareMvpInternal()
        {
            Debug.Log("[Beyond The Beat] Starting Phase 1 MVP CI preparation.");
            AppendDiagnostic("Phase 1 MVP preparation START.");

            AppendDiagnostic("Rebuilding Phase 0 prerequisite foundation.");
            Phase0BuildAutomation.PreparePrototype();

            for (int i = 0; i < Phase1BuildSteps.Length; i++)
            {
                ExecuteMenuStep(Phase1BuildSteps[i]);
            }

            for (int i = 0; i < Phase1ValidationSteps.Length; i++)
            {
                ExecuteMenuStep(Phase1ValidationSteps[i]);
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException(
                    $"Phase 1 preparation completed without producing the required scene at '{ScenePath}'.");
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

            AppendDiagnostic("Phase 1 MVP preparation PASS.");
            Debug.Log(
                "[Beyond The Beat] Phase 1 MVP CI preparation PASS. " +
                "World/zone, Reach Location mission, and centralized local save/resume are generated and structurally validated.");
        }

        private static void BuildDevelopmentAndroidApk()
        {
            string outputPath = GetCommandLineArgument("customBuildPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(
                    Path.Combine("build", "Android", "BeyondTheBeat-Phase1-save-resume-local.apk"));
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
            Debug.Log($"[Beyond The Beat] Building Phase 1 Save/Resume development APK: {outputPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            AppendDiagnostic(
                $"Android BuildPipeline RESULT={summary.result}, errors={summary.totalErrors}, " +
                $"warnings={summary.totalWarnings}, size={summary.totalSize}, duration={summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Phase 1 Android build failed with result {summary.result}. " +
                    $"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException(
                    "Unity reported a successful Phase 1 Android build but the APK was not found.",
                    outputPath);
            }

            string stagedPath = StageApkInsideProject(outputPath);
            AppendDiagnostic($"APK staging PASS. source={outputPath}, staged={stagedPath}");

            Debug.Log(
                "[Beyond The Beat] Phase 1 Save/Resume Android APK build PASS. " +
                $"Output: {outputPath}, staged output: {stagedPath}, size: {summary.totalSize} bytes, " +
                $"duration: {summary.totalTime}, warnings: {summary.totalWarnings}.");
        }

        private static string StageApkInsideProject(string outputPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve the Unity project root while staging the Phase 1 APK.");
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
                throw new FileNotFoundException(
                    "The Phase 1 APK could not be staged inside the Unity project workspace.",
                    stagedFullPath);
            }

            return stagedFullPath;
        }

        private static void ExecuteMenuStep(string menuPath)
        {
            if (Application.isBatchMode)
            {
                SaveDirtyPersistedScenesForBatchMode();
                AssetDatabase.SaveAssets();
            }

            capturedErrors = 0;
            capturedErrorMessages.Clear();
            AppendDiagnostic($"MENU START: {menuPath}");
            Debug.Log($"[Beyond The Beat] Phase 1 CI step: {menuPath}");

            bool executed = EditorApplication.ExecuteMenuItem(menuPath);
            if (!executed)
            {
                AppendDiagnostic($"MENU FAIL (not executable): {menuPath}");
                throw new InvalidOperationException($"Unity menu command could not be executed: {menuPath}");
            }

            if (capturedErrors > 0)
            {
                string details = capturedErrorMessages.Count == 0
                    ? string.Empty
                    : "\n" + string.Join("\n---\n", capturedErrorMessages);

                AppendDiagnostic($"MENU FAIL ({capturedErrors} error(s)): {menuPath}{details}");
                throw new InvalidOperationException(
                    $"Unity menu command reported {capturedErrors} error(s): {menuPath}.{details}");
            }

            AppendDiagnostic($"MENU PASS: {menuPath}");
        }

        private static void SaveDirtyPersistedScenesForBatchMode()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isDirty || string.IsNullOrWhiteSpace(scene.path))
                {
                    continue;
                }

                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unable to save dirty scene '{scene.path}' before the next Phase 1 CI step.");
                }
            }
        }

        private static void ApplyVersionFromCommandLine()
        {
            string buildVersion = GetCommandLineArgument("buildVersion");
            if (!string.IsNullOrWhiteSpace(buildVersion))
            {
                PlayerSettings.bundleVersion = buildVersion;
            }

            string androidVersionCode = GetCommandLineArgument("androidVersionCode");
            if (!string.IsNullOrWhiteSpace(androidVersionCode) &&
                int.TryParse(androidVersionCode, out int versionCode) &&
                versionCode > 0)
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
            }
        }

        private static string GetCommandLineArgument(string name)
        {
            string expected = "-" + name;
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            capturedErrors++;
            string message = string.IsNullOrWhiteSpace(stackTrace)
                ? condition
                : $"{condition}\n{stackTrace}";
            capturedErrorMessages.Add(message);
            AppendDiagnostic($"UNITY {type}: {message}");
        }

        private static void InitializeDiagnostics()
        {
            try
            {
                string path = GetDiagnosticsPath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    path,
                    $"[{DateTime.UtcNow:O}] Beyond The Beat Phase 1 CI diagnostics initialized.{Environment.NewLine}");
            }
            catch
            {
                // Diagnostics must never replace the actual Unity build failure.
            }
        }

        private static void AppendDiagnostic(string message)
        {
            try
            {
                string path = GetDiagnosticsPath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(
                    path,
                    $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Diagnostics must never replace the actual Unity build failure.
            }
        }

        private static string GetDiagnosticsPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot ?? ".", DiagnosticsRelativePath));
        }
    }
}
