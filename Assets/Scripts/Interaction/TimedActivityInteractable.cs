using System;
using System.Collections;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    public abstract class TimedActivityInteractable : InteractableObject
    {
        [Header("Timed Activity")]
        [SerializeField, Min(0.05f)] private float durationSeconds = 2f;
        [SerializeField] private bool resetProgressOnCancel = true;

        private float elapsedSeconds;
        private Coroutine activityRoutine;

        public float DurationSeconds => Mathf.Max(0.05f, durationSeconds);
        public float ElapsedSeconds => elapsedSeconds;
        public float Progress01 => Mathf.Clamp01(elapsedSeconds / DurationSeconds);

        public event Action<TimedActivityInteractable, float> ProgressChanged;

        protected sealed override void OnInteractionRequested(GameObject actor)
        {
            StopActivityRoutine();
            elapsedSeconds = 0f;
            ProgressChanged?.Invoke(this, 0f);
            OnActivityStarted(actor);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                activityRoutine = StartCoroutine(RunActivity());
            }
        }

        protected sealed override void OnInteractionCancelled(GameObject actor)
        {
            StopActivityRoutine();

            if (resetProgressOnCancel)
            {
                elapsedSeconds = 0f;
                ProgressChanged?.Invoke(this, 0f);
            }

            OnActivityCancelled(actor);
        }

        public bool AdvanceActivity(float deltaSeconds)
        {
            if (!IsInteracting || deltaSeconds <= 0f)
            {
                return false;
            }

            elapsedSeconds = Mathf.Min(DurationSeconds, elapsedSeconds + deltaSeconds);
            ProgressChanged?.Invoke(this, Progress01);

            if (elapsedSeconds < DurationSeconds)
            {
                return false;
            }

            GameObject actor = CurrentActor;
            activityRoutine = null;
            OnActivityCompleted(actor);
            CompleteInteraction(actor);
            return true;
        }

        protected virtual void OnActivityStarted(GameObject actor)
        {
        }

        protected virtual void OnActivityCancelled(GameObject actor)
        {
        }

        protected abstract void OnActivityCompleted(GameObject actor);

        private IEnumerator RunActivity()
        {
            while (IsInteracting)
            {
                yield return null;

                if (AdvanceActivity(Time.deltaTime))
                {
                    break;
                }
            }

            activityRoutine = null;
        }

        private void StopActivityRoutine()
        {
            if (activityRoutine == null)
            {
                return;
            }

            StopCoroutine(activityRoutine);
            activityRoutine = null;
        }
    }
}
