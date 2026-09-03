using System;
using System.IO;
using System.Reflection;
using BeyondTheBeat.Tutorial;
using BeyondTheBeat.UI;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6TutorialFastValidation
    {
        private const string ValidationDocPath = "Docs/Validation/PHASE_6_TUTORIAL_ONBOARDING.md";

        public static void ValidateTutorialOnly()
        {
            TutorialProfile profile = ScriptableObject.CreateInstance<TutorialProfile>();
            GameObject inputObject = null;
            GameObject controllerObject = null;
            GameObject skipControllerObject = null;

            try
            {
                profile.Configure(
                    "phase6-fast-tutorial",
                    true,
                    new[]
                    {
                        new TutorialStep("steer", "Steer", "Use LEFT or RIGHT.", TutorialSignal.Steering, 0.5f, 0.25f),
                        new TutorialStep("accelerate", "Move", "Use GO.", TutorialSignal.Accelerate, 0.5f, 0.25f),
                        new TutorialStep("brake-reverse", "Brake / Reverse", "Use REV.", TutorialSignal.BrakeOrReverse, 0.5f, 0.25f),
                        new TutorialStep("action", "Interact", "Use ACTION.", TutorialSignal.Interaction, 0.1f, 0f)
                    });

                inputObject = new GameObject("FastTutorialInput");
                MobileDrivingInput input = inputObject.AddComponent<MobileDrivingInput>();

                controllerObject = new GameObject("FastTutorialController");
                TutorialController controller = controllerObject.AddComponent<TutorialController>();
                controller.Configure(profile, input, false, false);

                bool began = controller.Begin(true);
                bool wrongInputDoesNotAdvance =
                    !controller.EvaluateSampleForValidation(0f, 0f, 0f, false, 0.5f) &&
                    controller.CurrentStepIndex == 0;

                bool steerAdvances = controller.EvaluateSampleForValidation(1f, 0f, 0f, false, 0.3f) &&
                                     controller.CurrentStepIndex == 1;
                bool accelerateAdvances = controller.EvaluateSampleForValidation(0f, 1f, 0f, false, 0.3f) &&
                                          controller.CurrentStepIndex == 2;
                bool reverseAdvances = controller.EvaluateSampleForValidation(0f, -1f, 0f, false, 0.3f) &&
                                       controller.CurrentStepIndex == 3;
                bool interactionCompletes = controller.EvaluateSampleForValidation(0f, 0f, 0f, true, 0f) &&
                                            controller.IsComplete && !controller.IsActive && !controller.WasSkipped;

                skipControllerObject = new GameObject("FastTutorialSkipController");
                TutorialController skipController = skipControllerObject.AddComponent<TutorialController>();
                skipController.Configure(profile, input, false, false);
                bool skipPass = skipController.Begin(true);
                skipController.Skip();
                skipPass &= skipController.IsComplete && skipController.WasSkipped && !skipController.IsActive;

                bool profilePass = profile.IsConfigured && profile.StepCount == 4 && profile.AllowSkip;
                bool signalPass = TutorialController.IsSignalSatisfied(profile.Steps[0], -1f, 0f, 0f, false) &&
                                  TutorialController.IsSignalSatisfied(profile.Steps[1], 0f, 1f, 0f, false) &&
                                  TutorialController.IsSignalSatisfied(profile.Steps[2], 0f, -1f, 0f, false) &&
                                  TutorialController.IsSignalSatisfied(profile.Steps[2], 0f, 0f, 1f, false) &&
                                  TutorialController.IsSignalSatisfied(profile.Steps[3], 0f, 0f, 0f, true);

                bool hudNoUpdate = typeof(TutorialHud).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) == null;

                bool repositoryPass = ValidateRepositoryContract();
                bool pass = began && wrongInputDoesNotAdvance && steerAdvances && accelerateAdvances &&
                            reverseAdvances && interactionCompletes && skipPass && profilePass && signalPass &&
                            hudNoUpdate && repositoryPass;

                if (!pass)
                {
                    throw new InvalidOperationException(
                        "Phase 6 fast tutorial validation failed: " +
                        $"began={began}, wrong={wrongInputDoesNotAdvance}, steer={steerAdvances}, " +
                        $"accelerate={accelerateAdvances}, reverse={reverseAdvances}, interact={interactionCompletes}, " +
                        $"skip={skipPass}, profile={profilePass}, signals={signalPass}, hudNoUpdate={hudNoUpdate}, repo={repositoryPass}.");
                }
            }
            finally
            {
                if (skipControllerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(skipControllerObject);
                }
                if (controllerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(controllerObject);
                }
                if (inputObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(inputObject);
                }
                UnityEngine.Object.DestroyImmediate(profile);
            }

            Debug.Log("[Beyond The Beat] FAST tutorial validation PASS: ordered control progression, wrong-input rejection, skip behavior and presentation-no-Update contract passed without scene generation or APK packaging.");
        }

        private static bool ValidateRepositoryContract()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            string docPath = Path.Combine(root, ValidationDocPath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(docPath) &&
                   File.ReadAllText(docPath).Contains("CI GREEN IS NOT DEVICE ONBOARDING SIGN-OFF", StringComparison.Ordinal);
        }
    }
}
