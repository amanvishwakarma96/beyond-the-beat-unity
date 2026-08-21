using System;
using System.Collections.Generic;
using BeyondTheBeat.UI;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    public sealed class InteractionController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private MobileDrivingInput inputSource;

        [Header("Actor")]
        [SerializeField] private GameObject actor;

        private readonly List<InteractableObject> candidates = new List<InteractableObject>(4);
        private InteractableObject activeInteractable;
        private bool lastPromptVisible;
        private string lastPromptText = string.Empty;

        public InteractableObject ActiveInteractable => activeInteractable;

        public event Action<InteractableObject> ActiveInteractableChanged;
        public event Action<bool, string> PromptChanged;

        private void Awake()
        {
            if (actor == null)
            {
                actor = gameObject;
            }
        }

        private void OnEnable()
        {
            SubscribeToInput();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            candidates.Clear();
            SetActiveInteractable(null);
        }

        private void Update()
        {
            RefreshActiveInteractable();
        }

        public void SetInputSource(MobileDrivingInput source)
        {
            if (inputSource == source)
            {
                return;
            }

            UnsubscribeFromInput();
            inputSource = source;

            if (isActiveAndEnabled)
            {
                SubscribeToInput();
            }
        }

        public void Register(InteractableObject interactable)
        {
            if (interactable == null || candidates.Contains(interactable))
            {
                return;
            }

            candidates.Add(interactable);
            RefreshActiveInteractable();
        }

        public void Unregister(InteractableObject interactable)
        {
            if (interactable == null)
            {
                return;
            }

            candidates.Remove(interactable);
            RefreshActiveInteractable();
        }

        public bool RequestActiveInteraction()
        {
            RefreshActiveInteractable();
            return activeInteractable != null && activeInteractable.RequestInteraction(actor);
        }

        private void HandleInteractionPressed()
        {
            RequestActiveInteraction();
        }

        private void SubscribeToInput()
        {
            if (inputSource != null)
            {
                inputSource.InteractionPressed -= HandleInteractionPressed;
                inputSource.InteractionPressed += HandleInteractionPressed;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (inputSource != null)
            {
                inputSource.InteractionPressed -= HandleInteractionPressed;
            }
        }

        private void RefreshActiveInteractable()
        {
            InteractableObject best = null;
            float bestDistanceSquared = float.PositiveInfinity;
            Vector3 actorPosition = actor != null ? actor.transform.position : transform.position;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                InteractableObject candidate = candidates[i];
                if (candidate == null)
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                if (!candidate.IsEligible(actor))
                {
                    continue;
                }

                float distanceSquared = (candidate.transform.position - actorPosition).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    best = candidate;
                    bestDistanceSquared = distanceSquared;
                }
            }

            SetActiveInteractable(best);
        }

        private void SetActiveInteractable(InteractableObject next)
        {
            bool activeChanged = activeInteractable != next;
            activeInteractable = next;

            if (activeChanged)
            {
                ActiveInteractableChanged?.Invoke(activeInteractable);
            }

            bool promptVisible = activeInteractable != null;
            string promptText = promptVisible ? activeInteractable.PromptText : string.Empty;

            if (lastPromptVisible != promptVisible || !string.Equals(lastPromptText, promptText, StringComparison.Ordinal))
            {
                lastPromptVisible = promptVisible;
                lastPromptText = promptText;
                PromptChanged?.Invoke(promptVisible, promptText);
            }
        }
    }
}
