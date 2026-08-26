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
    public static class Phase0BuildAutomation
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";
        private const string AndroidApplicationId = "com.beyondthebeat.prototype";
        private const string DiagnosticsRelativePath = "build/phase0-ci-diagnostics.log";

        private static readonly string[] BuildSteps =
        {
            "Beyond The Beat/Project/Run Bootstrap",
            "Beyond The Beat/Phase 0/Build Prototype Environment",
            "Beyond The Beat/Phase 0/Build Prototype Vehicle",
            "Beyond The Beat/Phase 0/Build Smooth Vehicle Camera",
            "Beyond The Beat/Phase 0/Build Mobile Driving Controls",
            "Beyond The Beat/Phase 0/Build Interaction Foundation",
            "Beyond The Beat/Phase 0/Build Parking Interaction",
            "Beyond The Beat/Phase 0/Build Minimal HUD"
        };

        private static readonly string[] ValidationSteps =
        {
            "Beyond The Beat/Project/Validate Bootstrap",
            "Beyond The Beat/Phase 0/Validate Prototype Environment",
            "Beyond The Beat/Phase 0/Validate Prototype Vehicle",
            "Beyond The Beat/Phase 0/Validate Smooth Vehicle Camera",
            "Beyond The Beat/Phase 0/Validate Mobile Driving Controls",
            "Beyond The Beat/Phase 0/Validate Interaction Foundation",
            "Beyond The Beat/Phase 0/Validate Parking Interaction",
            "Beyond The Beat/Phase 0/Validate Minimal HUD"
        };

        private static int capturedErrors;
        private static readonly List<string> capturedErrorMessages = new List<string>();

        /// <summary>
        /// GameCI build entry point.
        /// Generates the complete Phase 0 prototype, runs all structural validators,
        /// and produces a Unity Development Android APK at GameCI's customBuildPath.
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
                PreparePrototypeInternal();
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

        /// <summary>
        /// Optional editor/CI preparation entry point when an APK is not required.
        /// </summary>
        public static void PreparePrototype()
        {
            InitializeDiagnostics();
            AppendDiagnostic(
                $"PreparePrototype START. Unity={Application.unityVersion}, batchMode={Application.isBatchMode}, " +
                $"dataPath={Application.dataPath}");

            Application.logMessageReceived += CaptureLog;

            try
            {
                PreparePrototypeInternal();
                AppendDiagnostic("PreparePrototype PASS.");
            }
            catch (Exception exception)
            {
                AppendDiagnostic("PreparePrototype FATAL\n" + exception);
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
            }
        }

        private static void PreparePrototypeInternal()
        {
            Debug.Log("[Beyond The Beat] Starting Phase 0 CI prototype preparation.");
            AppendDiagnostic("Phase 0 prototype preparation START.");

            for (int i = 0; i < BuildSteps.Length; i++)
            {
                ExecuteMenuStep(BuildSteps[i]);
            }

            for (int i = 0; i < ValidationSteps.Length; i++)
            {
                ExecuteMenuStep(ValidationSteps[i]);
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException(
                    $"Phase 0 preparation completed without producing the required scene at '{ScenePath}'.");
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

            AppendDiagnostic("Phase 0 prototype preparation PASS.");
            Debug.Log(
                "[Beyond The Beat] Phase 0 CI preparation PASS. " +
                $"Scene '{ScenePath}' is generated, validated, and enabled for build.");
        }

        private static void BuildDevelopmentAndroidApk()
        {
            string outputPath = GetCommandLineArgument("customBuildPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFullPath(
                    Path.Combine("build", "Android", "BeyondTheBeat-Phase0-local.apk"));
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
            Debug.Log($"[Beyond The Beat] Building Phase 0 development APK: {outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            AppendDiagnostic(
                $"Android BuildPipeline RESULT={summary.result}, errors={summary.totalErrors}, " +
                $"warnings={summary.totalWarnings}, size={summary.totalSize}, duration={summary.totalTime}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Phase 0 Android build failed with result {summary.result}. " +
                    $"Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}.");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException(
                    "Unity reported a successful Android build but the APK was not found.",
                    outputPath);
            }

            string stagedPath = StageApkInsideProject(outputPath);

            AppendDiagnostic($"APK staging PASS. source={outputPath}, staged={stagedPath}");
            Debug.Log(
                "[Beyond The Beat] Phase 0 Android APK build PASS. " +
                $"Output: {outputPath}, staged output: {stagedPath}, size: {summary.totalSize} bytes, " +
                $"duration: {summary.totalTime}, warnings: {summary.totalWarnings}.");
        }

        private static string StageApkInsideProject(string outputPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve the Unity project root while staging the Android APK.");
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
                    "The Android APK could not be staged inside the Unity project workspace.",
                    stagedFullPath);
            }

            Debug.Log($"[Beyond The Beat] Staged CI APK inside project workspace: {stagedFullPath}");
            return stagedFullPath;
        }

        private static void ApplyVersionFromCommandLine()
        {
            string buildVersion = GetCommandLineArgument("buildVersion");
            if (!string.IsNullOrWhiteSpace(buildVersion))
            {
                PlayerSettings.bundleVersion = buildVersion;
            }

            string androidVersionCode = GetCommandLineArgument("androidVersionCode");
            int versionCode;
            if (!string.IsNullOrWhiteSpace(androidVersionCode) &&
                int.TryParse(androidVersionCode, out versionCode) &&
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
            Debug.Log($"[Beyond The Beat] CI step: {menuPath}");

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

                AppendDiagnostic(
                    $"MENU FAIL ({capturedErrors} error(s)): {menuPath}{details}");
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

                AppendDiagnostic($"Saving dirty persisted scene before menu step: {scene.path}");
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Unable to save dirty scene '{scene.path}' before the next CI menu step.");
                }
            }
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
                    $"[{DateTime.UtcNow:O}] Beyond The Beat Phase 0 CI diagnostics initialized.{Environment.NewLine}");
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
