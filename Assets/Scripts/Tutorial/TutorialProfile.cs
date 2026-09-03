using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.Tutorial
{
    public enum TutorialSignal
    {
        Steering = 0,
        Accelerate = 1,
        BrakeOrReverse = 2,
        Interaction = 3
    }

    [Serializable]
    public sealed class TutorialStep
    {
        [SerializeField] private string stepId;
        [SerializeField] private string title;
        [TextArea(2, 4)]
        [SerializeField] private string instruction;
        [SerializeField] private TutorialSignal signal;
        [SerializeField, Min(0f)] private float inputThreshold = 0.25f;
        [SerializeField, Min(0f)] private float holdSeconds = 0.35f;

        public string StepId => stepId;
        public string Title => title;
        public string Instruction => instruction;
        public TutorialSignal Signal => signal;
        public float InputThreshold => inputThreshold;
        public float HoldSeconds => holdSeconds;

        public TutorialStep(
            string stepId,
            string title,
            string instruction,
            TutorialSignal signal,
            float inputThreshold = 0.25f,
            float holdSeconds = 0.35f)
        {
            this.stepId = stepId;
            this.title = title;
            this.instruction = instruction;
            this.signal = signal;
            this.inputThreshold = Mathf.Max(0f, inputThreshold);
            this.holdSeconds = Mathf.Max(0f, holdSeconds);
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(stepId) &&
            !string.IsNullOrWhiteSpace(title) &&
            !string.IsNullOrWhiteSpace(instruction) &&
            inputThreshold >= 0f &&
            holdSeconds >= 0f;
    }

    [CreateAssetMenu(menuName = "Beyond The Beat/Tutorial/Tutorial Profile", fileName = "TutorialProfile")]
    public sealed class TutorialProfile : ScriptableObject
    {
        [SerializeField] private string tutorialId = "phase6-core-controls";
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        public string TutorialId => tutorialId;
        public bool AllowSkip => allowSkip;
        public IReadOnlyList<TutorialStep> Steps => steps;
        public int StepCount => steps?.Count ?? 0;
        public string CompletionKey => $"BeyondTheBeat.Tutorial.{tutorialId}.Complete";

        public bool IsConfigured
        {
            get
            {
                if (string.IsNullOrWhiteSpace(tutorialId) || steps == null || steps.Count == 0)
                {
                    return false;
                }

                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < steps.Count; i++)
                {
                    TutorialStep step = steps[i];
                    if (step == null || !step.IsConfigured || !ids.Add(step.StepId))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Configure(string id, bool canSkip, IEnumerable<TutorialStep> orderedSteps)
        {
            tutorialId = id ?? string.Empty;
            allowSkip = canSkip;
            steps = orderedSteps != null
                ? new List<TutorialStep>(orderedSteps)
                : new List<TutorialStep>();
        }
    }
}
