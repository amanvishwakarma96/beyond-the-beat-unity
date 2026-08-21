using System;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase0BuildAutomation
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Phase0_Prototype.unity";

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

        /// <summary>
        /// CI entry point used before the Android build.
        /// Generates the complete Phase 0 prototype from the repository's editor builders,
        /// runs every structural validator, and leaves exactly the generated prototype scene
        /// enabled in Build Settings. This method intentionally does not create an APK.
        /// </summary>
        public static void PreparePrototype()
        {
            Application.logMessageReceived += CaptureLog;

            try
            {
                Debug.Log("[Beyond The Beat] Starting Phase 0 CI prototype preparation.");

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
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.beyondthebeat.prototype");
                EditorUserBuildSettings.buildAppBundle = false;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Beyond The Beat] Phase 0 CI preparation PASS. " +
                    $"Scene '{ScenePath}' is generated, validated, and enabled for build.");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
            }
        }

        private static void ExecuteMenuStep(string menuPath)
        {
            capturedErrors = 0;
            Debug.Log($"[Beyond The Beat] CI step: {menuPath}");

            bool executed = EditorApplication.ExecuteMenuItem(menuPath);
            if (!executed)
            {
                throw new InvalidOperationException($"Unity menu command could not be executed: {menuPath}");
            }

            if (capturedErrors > 0)
            {
                throw new InvalidOperationException(
                    $"Unity menu command reported {capturedErrors} error(s): {menuPath}");
            }
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                capturedErrors++;
            }
        }
    }
}
