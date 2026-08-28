using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Clean CI checkouts in this repository do not currently carry the full PlayerSettings asset.
    /// Enforce the runtime input backend explicitly before building so Android touch cannot silently
    /// fall back to an unexpected default.
    ///
    /// BuildProfile is intentionally resolved through reflection. Its editor API surface differs
    /// between Unity 6 revisions, and a compile-time dependency here can prevent the entire project
    /// from compiling before the Android build even starts.
    /// </summary>
    internal static class InputBackendBuildGuard
    {
        private const string ActiveInputHandlerProperty = "activeInputHandler";
        private const string NativeBackendsProperty = "enableNativePlatformBackendsForNewInputSystem";
        private const string BuildProfileTypeName = "UnityEditor.Build.Profile.BuildProfile";
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
            PlayerSettings playerSettings = ResolvePlayerSettingsFromBuildProfile();
            if (playerSettings != null)
            {
                return playerSettings;
            }

            PlayerSettings[] candidates = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            return candidates != null && candidates.Length > 0 ? candidates[0] : null;
        }

        private static PlayerSettings ResolvePlayerSettingsFromBuildProfile()
        {
            Type buildProfileType = FindType(BuildProfileTypeName);
            if (buildProfileType == null)
            {
                return null;
            }

            FieldInfo globalPlayerSettingsField = buildProfileType.GetField(
                "s_GlobalPlayerSettings",
                BindingFlags.Static | BindingFlags.NonPublic);

            PlayerSettings playerSettings = globalPlayerSettingsField != null
                ? globalPlayerSettingsField.GetValue(null) as PlayerSettings
                : null;

            MethodInfo getActiveBuildProfile = buildProfileType.GetMethod(
                "GetActiveBuildProfile",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            object activeBuildProfile = getActiveBuildProfile != null
                ? getActiveBuildProfile.Invoke(null, null)
                : null;

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
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
