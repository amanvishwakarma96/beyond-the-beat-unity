using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Clean CI checkouts in this repository do not currently carry the full PlayerSettings asset.
    /// Enforce the runtime input backend explicitly before building so Android touch cannot silently
    /// fall back to an unexpected default.
    /// </summary>
    internal static class InputBackendBuildGuard
    {
        private const string ActiveInputHandlerProperty = "activeInputHandler";
        private const string NativeBackendsProperty = "enableNativePlatformBackendsForNewInputSystem";
        private const int BothInputBackends = 2;

        public static void EnsureBothInputBackends()
        {
            PlayerSettings playerSettings = ResolvePlayerSettings();
            if (playerSettings == null)
            {
                throw new InvalidOperationException("Unable to resolve Unity PlayerSettings while configuring Android input backends.");
            }

            SerializedObject serialized = new SerializedObject(playerSettings);
            SerializedProperty activeInputHandler = serialized.FindProperty(ActiveInputHandlerProperty);
            if (activeInputHandler == null)
            {
                throw new InvalidOperationException(
                    $"Unity PlayerSettings property '{ActiveInputHandlerProperty}' could not be resolved.");
            }

            int previousValue = activeInputHandler.intValue;
            activeInputHandler.intValue = BothInputBackends;

            SerializedProperty nativeBackends = serialized.FindProperty(NativeBackendsProperty);
            if (nativeBackends != null)
            {
                nativeBackends.boolValue = true;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerSettings);
            AssetDatabase.SaveAssets();

            SerializedObject verification = new SerializedObject(playerSettings);
            SerializedProperty verifiedInputHandler = verification.FindProperty(ActiveInputHandlerProperty);
            if (verifiedInputHandler == null || verifiedInputHandler.intValue != BothInputBackends)
            {
                throw new InvalidOperationException(
                    "Failed to enforce Active Input Handling = Both before the Android build.");
            }

            Debug.Log(
                "[Beyond The Beat] Android input backend guard PASS. " +
                $"Active Input Handling {previousValue} -> {BothInputBackends} (Both); native Input System backends enabled when supported.");
        }

        private static PlayerSettings ResolvePlayerSettings()
        {
#if UNITY_6000_0_OR_NEWER
            Type buildProfileType = typeof(BuildProfile);
            FieldInfo globalPlayerSettingsField = buildProfileType.GetField(
                "s_GlobalPlayerSettings",
                BindingFlags.Static | BindingFlags.NonPublic);

            PlayerSettings playerSettings = globalPlayerSettingsField != null
                ? globalPlayerSettingsField.GetValue(null) as PlayerSettings
                : null;

            BuildProfile activeBuildProfile = BuildProfile.GetActiveBuildProfile();
            if (activeBuildProfile != null)
            {
                FieldInfo overrideField = buildProfileType.GetField(
                    "m_PlayerSettings",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                PlayerSettings profileOverride = overrideField != null
                    ? overrideField.GetValue(activeBuildProfile) as PlayerSettings
                    : null;

                if (profileOverride != null)
                {
                    playerSettings = profileOverride;
                }
            }

            return playerSettings;
#else
            PlayerSettings[] candidates = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            return candidates != null && candidates.Length > 0 ? candidates[0] : null;
#endif
        }
    }
}
