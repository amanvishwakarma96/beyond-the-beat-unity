using System;
using System.IO;
using System.Linq;
using BeyondTheBeat.Performance;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6BuildSizeBuilder
    {
        public const string ProfileAssetPath = "Assets/Settings/Performance/Phase6_MobileBuildOptimization.asset";
        public const string ReportRelativePath = "build/phase6-build-size-report.txt";

        [MenuItem("Beyond The Beat/Phase 6/Build Size/Prepare Android Build Optimization")]
        public static void PrepareAndroidBuildOptimization()
        {
            EnsureFolder("Assets/Settings", "Performance");
            MobileBuildOptimizationProfile profile = CreateOrUpdateProfile();
            ApplyProfile(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Beyond The Beat] Phase 6 Android build-size optimization prepared: engine stripping ON, " +
                $"managed stripping {profile.ManagedStripping}, LZ4HC={profile.UseLz4HcCompression}, " +
                $"backend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}, " +
                $"architectures preserved as {PlayerSettings.Android.targetArchitectures}.");
        }

        [MenuItem("Beyond The Beat/Phase 6/Build Size/Validate Android Build Optimization")]
        public static void ValidateAndroidBuildOptimization()
        {
            if (!ValidateAndroidBuildOptimizationInternal(out string message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static MobileBuildOptimizationProfile PrepareAndValidateOrThrow()
        {
            EnsureFolder("Assets/Settings", "Performance");
            MobileBuildOptimizationProfile profile = CreateOrUpdateProfile();
            ApplyProfile(profile);

            if (!ValidateAndroidBuildOptimizationInternal(out string message))
            {
                throw new InvalidOperationException(message);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(message);
            return profile;
        }

        public static BuildOptions GetBuildOptions(MobileBuildOptimizationProfile profile, BuildOptions baseOptions)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return profile.UseLz4HcCompression
                ? baseOptions | BuildOptions.CompressWithLz4HC
                : baseOptions;
        }

        public static void WriteBuildSizeReport(BuildReport report, MobileBuildOptimizationProfile profile)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            string path = ResolveProjectPath(ReportRelativePath);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            BuildSummary summary = report.summary;
            BuildFile[] buildFiles = report.GetFiles();
            BuildFile[] largestFiles = buildFiles
                .Where(file => file.size > 0)
                .OrderByDescending(file => file.size)
                .Take(profile.BuildReportTopFileCount)
                .ToArray();

            var largestPackedAssets = report.packedAssets
                .Where(group => group != null)
                .SelectMany(group => group.contents)
                .Where(info => info.packedSize > 0)
                .OrderByDescending(info => info.packedSize)
                .Take(profile.BuildReportTopFileCount)
                .ToArray();

            using StreamWriter writer = new StreamWriter(path, false);
            writer.WriteLine("BEYOND THE BEAT — PHASE 6 BUILD SIZE REPORT");
            writer.WriteLine($"result={summary.result}");
            writer.WriteLine($"totalBytes={summary.totalSize}");
            writer.WriteLine($"totalMiB={summary.totalSize / 1048576d:F2}");
            writer.WriteLine($"duration={summary.totalTime}");
            writer.WriteLine($"scriptingBackend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
            writer.WriteLine($"stripEngineCode={PlayerSettings.stripEngineCode}");
            writer.WriteLine($"managedStripping={PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android)}");
            writer.WriteLine($"androidArchitectures={PlayerSettings.Android.targetArchitectures}");
            writer.WriteLine($"lz4hc={profile.UseLz4HcCompression}");
            writer.WriteLine("largestBuildFiles:");

            for (int i = 0; i < largestFiles.Length; i++)
            {
                BuildFile file = largestFiles[i];
                writer.WriteLine($"{i + 1}. {file.size} bytes | {file.path}");
            }

            writer.WriteLine("largestPackedAssets:");
            for (int i = 0; i < largestPackedAssets.Length; i++)
            {
                PackedAssetInfo info = largestPackedAssets[i];
                string source = string.IsNullOrWhiteSpace(info.sourceAssetPath) ? "<generated>" : info.sourceAssetPath;
                writer.WriteLine($"{i + 1}. {info.packedSize} bytes | {info.type} | {source}");
            }

            Debug.Log($"[Beyond The Beat] Phase 6 build-size report written to '{ReportRelativePath}'.");
        }

        private static bool ValidateAndroidBuildOptimizationInternal(out string message)
        {
            MobileBuildOptimizationProfile profile = AssetDatabase.LoadAssetAtPath<MobileBuildOptimizationProfile>(ProfileAssetPath);
            if (profile == null)
            {
                message = "[Beyond The Beat] Phase 6 build-size validation FAIL: optimization profile asset is missing.";
                return false;
            }

            ManagedStrippingLevel expected = MapManagedStripping(profile.ManagedStripping);
            bool profilePass = profile.IsConfigured && profile.StripEngineCode && profile.PreserveCurrentAndroidArchitectures;
            bool strippingPass = PlayerSettings.stripEngineCode &&
                                 PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) == expected;
            bool compressionPass = profile.UseLz4HcCompression;

            bool pass = profilePass && strippingPass && compressionPass;
            message = pass
                ? "[Beyond The Beat] Phase 6 build-size optimization validation PASS: engine-strip policy, conservative managed stripping, LZ4HC compression and architecture-preservation policy are configured. Engine-code stripping only reduces native code when the preserved backend is IL2CPP. Physical install/launch validation remains required for stripping safety."
                : "[Beyond The Beat] Phase 6 build-size optimization validation FAIL: " +
                  $"profile={profilePass}, stripping={strippingPass}, compression={compressionPass}, " +
                  $"actualManaged={PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android)}, expectedManaged={expected}.";
            return pass;
        }

        private static MobileBuildOptimizationProfile CreateOrUpdateProfile()
        {
            MobileBuildOptimizationProfile profile = AssetDatabase.LoadAssetAtPath<MobileBuildOptimizationProfile>(ProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MobileBuildOptimizationProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            SetBool(serialized, "stripEngineCode", true);
            SetInt(serialized, "managedStripping", (int)MobileManagedStrippingPolicy.Low);
            SetBool(serialized, "useLz4HcCompression", true);
            SetInt(serialized, "buildReportTopFileCount", 15);
            SetBool(serialized, "preserveCurrentAndroidArchitectures", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ApplyProfile(MobileBuildOptimizationProfile profile)
        {
            if (profile == null || !profile.IsConfigured)
            {
                throw new InvalidOperationException("Phase 6 build optimization profile is not configured.");
            }

            ScriptingImplementation scriptingBackendBefore = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            AndroidArchitecture architecturesBefore = PlayerSettings.Android.targetArchitectures;

            PlayerSettings.stripEngineCode = profile.StripEngineCode;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, MapManagedStripping(profile.ManagedStripping));

            if (profile.PreserveCurrentAndroidArchitectures &&
                PlayerSettings.Android.targetArchitectures != architecturesBefore)
            {
                PlayerSettings.Android.targetArchitectures = architecturesBefore;
            }

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != scriptingBackendBefore)
            {
                throw new InvalidOperationException("Phase 6 build optimization must not change the Android scripting backend.");
            }
        }

        private static ManagedStrippingLevel MapManagedStripping(MobileManagedStrippingPolicy policy)
        {
            return policy switch
            {
                MobileManagedStrippingPolicy.Medium => ManagedStrippingLevel.Medium,
                MobileManagedStrippingPolicy.High => ManagedStrippingLevel.High,
                _ => ManagedStrippingLevel.Low
            };
        }

        private static string ResolveProjectPath(string relativePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            return string.IsNullOrWhiteSpace(root)
                ? relativePath
                : Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property '{propertyName}'.");
            property.boolValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                                          throw new InvalidOperationException($"Missing property '{propertyName}'.");
            property.intValue = value;
        }
    }
}
