using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Best-effort Android input-backend configuration for clean CI checkouts.
    ///
    /// Unity 6 has moved PlayerSettings ownership between the global project settings object and
    /// Build Profiles across revisions. The game itself has both New Input System and legacy touch
    /// fallbacks, so failure to resolve Unity's private editor representation must never prevent the
    /// APK from building. When the serialized setting is available we force Both and verify it;
    /// otherwise we emit a diagnostic warning and continue with the project's/default backend.
    /// </summary>
    internal static class InputBackendBuildGuard
    {
        private const string ActiveInputHandlerProperty = "activeInputHandler";
        private const string NativeBackendsProperty = "enableNativePlatformBackendsForNewInputSystem";
        private const string BuildProfileTypeName = "UnityEditor.Build.Profile.BuildProfile";
        private const int BothInputBackends = 2;

        /// <summary>
        /// Attempts to enforce Active Input Handling = Both.
        /// Returns true only when the serialized setting was resolved and verified.
        /// Never throws solely because Unity's private PlayerSettings representation changed.
        /// </summary>
        public static bool EnsureBothInputBackends()
        {
            try
            {
                PlayerSettings playerSettings = ResolvePlayerSettings();
                if (playerSettings == null)
                {
                    Debug.LogWarning(
                        "[Beyond The Beat] Android input backend guard: Unity PlayerSettings object could not be resolved in this editor revision. " +
                        "Continuing build with the configured/default backend; runtime touch retains New Input System + legacy fallback paths.");
                    return false;
                }

                SerializedObject serialized = new SerializedObject(playerSettings);
                serialized.Update();

                SerializedProperty activeInputHandler = serialized.FindProperty(ActiveInputHandlerProperty);
                if (activeInputHandler == null)
                {
                    Debug.LogWarning(
                        $"[Beyond The Beat] Android input backend guard: serialized property '{ActiveInputHandlerProperty}' is unavailable. " +
                        "Continuing build instead of failing on a Unity editor-internal API difference.");
                    return false;
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
                verification.Update();
                SerializedProperty verifiedInputHandler = verification.FindProperty(ActiveInputHandlerProperty);
                bool verified =
                    verifiedInputHandler != null &&
                    verifiedInputHandler.intValue == BothInputBackends;

                if (!verified)
                {
                    Debug.LogWarning(
                        "[Beyond The Beat] Android input backend guard could not verify Active Input Handling = Both. " +
                        "Continuing build; runtime touch fallback remains enabled.");
                    return false;
                }

                Debug.Log(
                    "[Beyond The Beat] Android input backend guard PASS. " +
                    $"Active Input Handling {previousValue} -> {BothInputBackends} (Both); native Input System backends enabled when supported.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Beyond The Beat] Android input backend guard encountered a Unity editor compatibility issue and will not block the APK build. " +
                    $"{exception.GetType().Name}: {exception.Message}");
                return false;
            }
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

            try
            {
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
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Beyond The Beat] Build Profile PlayerSettings reflection is unavailable in this Unity revision. " +
                    $"{exception.GetType().Name}: {exception.Message}");
                return null;
            }
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
