using System;
using BeyondTheBeat.UI;
using UnityEngine;

namespace BeyondTheBeat.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialController : MonoBehaviour
    {
        [SerializeField] private TutorialProfile profile;
        [SerializeField] private MobileDrivingInput inputSource;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool persistCompletion = true;

        private int currentStepIndex = -1;
        private float satisfiedDuration;
        private bool interactionPressed;

        public TutorialProfile Profile => profile;
        public MobileDrivingInput InputSource => inputSource;
        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }
        public bool WasSkipped { get; private set; }
        public int CurrentStepIndex => currentStepIndex;
        public int StepCount => profile != null ? profile.StepCount : 0;
        public TutorialStep CurrentStep =>
            profile != null && currentStepIndex >= 0 && currentStepIndex < profile.StepCount
                ? profile.Steps[currentStepIndex]
                : null;

        public event Action Changed;

        private void OnEnable()
        {
            if (inputSource != null)
            {
                inputSource.InteractionPressed += HandleInteractionPressed;
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                Begin();
            }
        }

        private void Update()
        {
            if (!IsActive || inputSource == null || CurrentStep == null)
            {
                return;
            }

            ProcessSample(
                inputSource.Steering,
                inputSource.Throttle,
                inputSource.Brake,
                interactionPressed,
                Time.unscaledDeltaTime);
            interactionPressed = false;
        }

        private void OnDisable()
        {
            if (inputSource != null)
            {
                inputSource.InteractionPressed -= HandleInteractionPressed;
            }
            interactionPressed = false;
        }

        public void Configure(
            TutorialProfile tutorialProfile,
            MobileDrivingInput mobileInput,
            bool shouldAutoStart = true,
            bool shouldPersistCompletion = true)
        {
            if (isActiveAndEnabled && inputSource != null)
            {
                inputSource.InteractionPressed -= HandleInteractionPressed;
            }

            profile = tutorialProfile;
            inputSource = mobileInput;
            autoStart = shouldAutoStart;
            persistCompletion = shouldPersistCompletion;

            if (isActiveAndEnabled && inputSource != null)
            {
                inputSource.InteractionPressed += HandleInteractionPressed;
            }
        }

        public bool Begin(bool ignorePersistedCompletion = false)
        {
            IsActive = false;
            IsComplete = false;
            WasSkipped = false;
            currentStepIndex = -1;
            satisfiedDuration = 0f;
            interactionPressed = false;

            if (profile == null || !profile.IsConfigured || inputSource == null)
            {
                Changed?.Invoke();
                return false;
            }

            if (!ignorePersistedCompletion && persistCompletion && PlayerPrefs.GetInt(profile.CompletionKey, 0) == 1)
            {
                IsComplete = true;
                Changed?.Invoke();
                return false;
            }

            currentStepIndex = 0;
            IsActive = true;
            Changed?.Invoke();
            return true;
        }

        public void Skip()
        {
            if (!IsActive || profile == null || !profile.AllowSkip)
            {
                return;
            }

            WasSkipped = true;
            CompleteTutorial();
        }

        public void ResetPersistedCompletion()
        {
            if (profile == null)
            {
                return;
            }

            PlayerPrefs.DeleteKey(profile.CompletionKey);
            PlayerPrefs.Save();
        }

        public bool EvaluateSampleForValidation(
            float steering,
            float throttle,
            float brake,
            bool interact,
            float deltaTime)
        {
            if (!IsActive || CurrentStep == null)
            {
                return false;
            }

            int before = currentStepIndex;
            bool completeBefore = IsComplete;
            ProcessSample(steering, throttle, brake, interact, Mathf.Max(0f, deltaTime));
            return currentStepIndex != before || IsComplete != completeBefore;
        }

        public static bool IsSignalSatisfied(
            TutorialStep step,
            float steering,
            float throttle,
            float brake,
            bool interact)
        {
            if (step == null)
            {
                return false;
            }

            float threshold = Mathf.Max(0f, step.InputThreshold);
            switch (step.Signal)
            {
                case TutorialSignal.Steering:
                    return Mathf.Abs(steering) >= threshold;
                case TutorialSignal.Accelerate:
                    return throttle >= threshold;
                case TutorialSignal.BrakeOrReverse:
                    return brake >= threshold || throttle <= -threshold;
                case TutorialSignal.Interaction:
                    return interact;
                default:
                    return false;
            }
        }

        private void ProcessSample(
            float steering,
            float throttle,
            float brake,
            bool interact,
            float deltaTime)
        {
            TutorialStep step = CurrentStep;
            if (step == null)
            {
                return;
            }

            if (!IsSignalSatisfied(step, steering, throttle, brake, interact))
            {
                satisfiedDuration = 0f;
                return;
            }

            if (step.Signal == TutorialSignal.Interaction || step.HoldSeconds <= 0f)
            {
                CompleteCurrentStep();
                return;
            }

            satisfiedDuration += Mathf.Max(0f, deltaTime);
            if (satisfiedDuration >= step.HoldSeconds)
            {
                CompleteCurrentStep();
            }
        }

        private void CompleteCurrentStep()
        {
            satisfiedDuration = 0f;
            interactionPressed = false;
            currentStepIndex++;

            if (profile == null || currentStepIndex >= profile.StepCount)
            {
                CompleteTutorial();
                return;
            }

            Changed?.Invoke();
        }

        private void CompleteTutorial()
        {
            IsActive = false;
            IsComplete = true;
            currentStepIndex = profile != null ? profile.StepCount : 0;
            satisfiedDuration = 0f;
            interactionPressed = false;

            if (persistCompletion && profile != null)
            {
                PlayerPrefs.SetInt(profile.CompletionKey, 1);
                PlayerPrefs.Save();
            }

            Changed?.Invoke();
        }

        private void HandleInteractionPressed()
        {
            interactionPressed = true;
        }
    }
}
