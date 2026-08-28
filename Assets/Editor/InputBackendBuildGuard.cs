using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    /// <summary>
    /// Fail-closed Android input-backend configuration for clean CI checkouts.
    ///
    /// Unity 6 has moved PlayerSettings ownership between the global project settings object and
    /// Build Profiles across revisions, so BuildProfile access stays reflection-based for editor
    /// compatibility. The Android runtime intentionally uses both New Input System and legacy touch
    /// paths; therefore the build is allowed to continue only after Active Input Handling is read
    /// back and deterministically verified as Both.
    /// </summary>
    internal static class InputBackendBuildGuard
    {
        private const string ActiveInputHandlerProperty = "activeInputHandler";
        private const string NativeBackendsProperty = "enableNativePlatformBackendsForNewInputSystem";
        private const string BuildProfileTypeName = "UnityEditor.Build.Profile.BuildProfile";
        private const int BothInputBackends = 2;
        private const string FailurePrefix = "[Beyond The Beat] Android input backend guard FAILED. ";

        /// <summary>
        /// Enforces Active Input Handling = Both and verifies the authoritative PlayerSettings value
        /// after configuration. This method either returns true with a verified backend or throws and
        /// aborts the CI build; there is deliberately no warn-and-continue path.
        /// </summary>
        public static bool EnsureBothInputBackends()
        {
            RunFailClosedContractCheck();

            PlayerSettings playerSettings = ResolvePlayerSettings();
            if (playerSettings == null)
            {
                throw CreateFailure(
                    "Expected Active Input Handling = Both (2), but Unity PlayerSettings could not be resolved. " +
                    "Verification is impossible, so the Android build is being aborted.");
            }

            int previousValue;
            bool nativeBackendsPropertyWasAvailable;

            try
            {
                SerializedObject serialized = new SerializedObject(playerSettings);
                serialized.Update();

                SerializedProperty activeInputHandler = serialized.FindProperty(ActiveInputHandlerProperty);
                if (activeInputHandler == null)
                {
                    AssertBackendValueOrThrow(
                        null,
                        $"PlayerSettings.{ActiveInputHandlerProperty}",
                        true);
                }

                previousValue = activeInputHandler.intValue;
                activeInputHandler.intValue = BothInputBackends;

                SerializedProperty nativeBackends = serialized.FindProperty(NativeBackendsProperty);
                nativeBackendsPropertyWasAvailable = nativeBackends != null;
                if (nativeBackends != null)
                {
                    nativeBackends.boolValue = true;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(playerSettings);
                AssetDatabase.SaveAssets();
            }
            catch (InputBackendVerificationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw CreateFailure(
                    "An editor error occurred while configuring Active Input Handling = Both (2). " +
                    $"{exception.GetType().Name}: {exception.Message}",
                    exception);
            }

            PlayerSettings verificationSettings = ResolvePlayerSettings();
            if (verificationSettings == null)
            {
                throw CreateFailure(
                    "Expected Active Input Handling = Both (2), but PlayerSettings could not be resolved during read-back verification. " +
                    "Verification is impossible, so the Android build is being aborted.");
            }

            int? verifiedInputHandler;
            bool? verifiedNativeBackends = null;

            try
            {
                SerializedObject verification = new SerializedObject(verificationSettings);
                verification.Update();

                SerializedProperty verifiedInputHandlerProperty = verification.FindProperty(ActiveInputHandlerProperty);
                verifiedInputHandler = verifiedInputHandlerProperty != null
                    ? verifiedInputHandlerProperty.intValue
                    : (int?)null;

                if (nativeBackendsPropertyWasAvailable)
                {
                    SerializedProperty verifiedNativeBackendsProperty = verification.FindProperty(NativeBackendsProperty);
                    verifiedNativeBackends = verifiedNativeBackendsProperty != null
                        ? verifiedNativeBackendsProperty.boolValue
                        : (bool?)null;
                }
            }
            catch (Exception exception)
            {
                throw CreateFailure(
                    "An editor error occurred while reading back the configured Android input backend. " +
                    $"Expected Active Input Handling = Both (2). {exception.GetType().Name}: {exception.Message}",
                    exception);
            }

            AssertBackendValueOrThrow(
                verifiedInputHandler,
                $"PlayerSettings.{ActiveInputHandlerProperty}",
                true);

            if (nativeBackendsPropertyWasAvailable && verifiedNativeBackends != true)
            {
                string found = verifiedNativeBackends.HasValue
                    ? verifiedNativeBackends.Value.ToString()
                    : "<unavailable>";

                throw CreateFailure(
                    $"Expected PlayerSettings.{NativeBackendsProperty} = True because this Unity revision exposes that setting, " +
                    $"but read-back verification found {found}. The Android build is being aborted.");
            }

            Debug.Log(
                "[Beyond The Beat] Android input backend guard PASS. " +
                $"Active Input Handling {DescribeBackend(previousValue)} -> Both (2), read-back verified Both (2)" +
                (nativeBackendsPropertyWasAvailable
                    ? "; native Input System platform backends read-back verified enabled."
                    : "; native Input System platform-backend property is not exposed by this Unity revision."));

            return true;
        }

        /// <summary>
        /// CI regression contract: the exact assertion used by the real guard must pass only for
        /// Both (2), and must throw for missing or wrong values. Running this before configuration
        /// prevents a future warn-and-continue regression from silently producing a bad APK.
        /// </summary>
        private static void RunFailClosedContractCheck()
        {
            AssertBackendValueOrThrow(BothInputBackends, "CI contract/pass", false);
            AssertContractValueFails(null, "CI contract/unavailable");
            AssertContractValueFails(0, "CI contract/legacy-only");
            AssertContractValueFails(1, "CI contract/new-input-only");

            Debug.Log(
                "[Beyond The Beat] Android input backend guard CI contract PASS. " +
                "Both (2) is accepted; unavailable, legacy-only (0), and new-input-only (1) are rejected.");
        }

        private static void AssertContractValueFails(int? actualValue, string source)
        {
            try
            {
                AssertBackendValueOrThrow(actualValue, source, false);
            }
            catch (InputBackendVerificationException)
            {
                return;
            }

            throw CreateFailure(
                $"Fail-closed CI contract regression: '{source}' with value {DescribeBackend(actualValue)} did not fail verification.");
        }

        private static void AssertBackendValueOrThrow(int? actualValue, string source, bool logFailure)
        {
            if (actualValue == BothInputBackends)
            {
                return;
            }

            string message =
                $"Expected Active Input Handling = Both (2), but {source} was {DescribeBackend(actualValue)}. " +
                "The Android build is being aborted because the mobile input backend is not deterministically verified.";

            if (logFailure)
            {
                Debug.LogError(FailurePrefix + message);
            }

            throw new InputBackendVerificationException(FailurePrefix + message);
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
                    "[Beyond The Beat] Build Profile PlayerSettings reflection is unavailable in this Unity revision; " +
                    "the guard will try the loaded PlayerSettings object and will hard-fail if no verifiable setting can be resolved. " +
                    $"{exception.GetType().Name}: {exception.Message}");
                return null;
            }
        }

        private static InputBackendVerificationException CreateFailure(string message, Exception innerException = null)
        {
            string fullMessage = FailurePrefix + message;
            Debug.LogError(fullMessage);
            return innerException == null
                ? new InputBackendVerificationException(fullMessage)
                : new InputBackendVerificationException(fullMessage, innerException);
        }

        private static string DescribeBackend(int? value)
        {
            if (!value.HasValue)
            {
                return "<unavailable>";
            }

            switch (value.Value)
            {
                case 0:
                    return "Input Manager (Old) (0)";
                case 1:
                    return "Input System Package (New) (1)";
                case BothInputBackends:
                    return "Both (2)";
                default:
                    return $"Unknown ({value.Value})";
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

        private sealed class InputBackendVerificationException : InvalidOperationException
        {
            public InputBackendVerificationException(string message)
                : base(message)
            {
            }

            public InputBackendVerificationException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }
    }
}
