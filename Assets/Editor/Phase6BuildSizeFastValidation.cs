using System;
using BeyondTheBeat.Performance;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6BuildSizeFastValidation
    {
        public static void ValidateBuildSizeOnly()
        {
            MobileBuildOptimizationProfile profile = ScriptableObject.CreateInstance<MobileBuildOptimizationProfile>();
            try
            {
                SerializedObject serialized = new SerializedObject(profile);
                SetBool(serialized, "stripEngineCode", true);
                SetInt(serialized, "managedStripping", (int)MobileManagedStrippingPolicy.Low);
                SetBool(serialized, "useLz4HcCompression", true);
                SetInt(serialized, "buildReportTopFileCount", 15);
                SetBool(serialized, "preserveCurrentAndroidArchitectures", true);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                BuildOptions options = Phase6BuildSizeBuilder.GetBuildOptions(profile, BuildOptions.Development);
                bool profilePass = profile.IsConfigured &&
                                   profile.StripEngineCode &&
                                   profile.ManagedStripping == MobileManagedStrippingPolicy.Low &&
                                   profile.PreserveCurrentAndroidArchitectures &&
                                   profile.BuildReportTopFileCount == 15;
                bool compressionPass = (options & BuildOptions.CompressWithLz4HC) != 0 &&
                                       (options & BuildOptions.Development) != 0;

                if (!profilePass || !compressionPass)
                {
                    throw new InvalidOperationException(
                        $"Phase 6 fast build-size validation failed: profile={profilePass}, compression={compressionPass}, options={options}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }

            Debug.Log(
                "[Beyond The Beat] FAST BUILD-SIZE VALIDATION PASS: engine-strip policy, conservative managed stripping, architecture preservation, LZ4HC composition and BuildReport diagnostics code compiled without APK packaging.");
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
