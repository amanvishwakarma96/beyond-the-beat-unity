using System;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    public abstract class InteractableObject : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private string promptText = "Interact";
        [SerializeField] private bool allowRepeatInteraction = true;

        private bool isInteracting;
        private bool hasCompleted;

        public string PromptText => promptText;
        public bool IsInteracting => isInteracting;
        public bool HasCompleted => hasCompleted;

        public event Action<InteractableObject, GameObject> InteractionRequested;
        public event Action<InteractableObject, GameObject> InteractionCompleted;
        public event Action<InteractableObject, GameObject> InteractionCancelled;

        public bool IsEligible(GameObject actor)
        {
            if (!isActiveAndEnabled || actor == null || isInteracting)
            {
                return false;
            }

            if (hasCompleted && !allowRepeatInteraction)
            {
                return false;
            }

            return CanInteract(actor);
        }

        public bool RequestInteraction(GameObject actor)
        {
            if (!IsEligible(actor))
            {
                return false;
            }

            isInteracting = true;
            InteractionRequested?.Invoke(this, actor);
            OnInteractionRequested(actor);
            return true;
        }

        public void ResetCompletion()
        {
            if (isInteracting)
            {
                return;
            }

            hasCompleted = false;
        }

        protected virtual bool CanInteract(GameObject actor)
        {
            return true;
        }

        protected abstract void OnInteractionRequested(GameObject actor);

        protected void CompleteInteraction(GameObject actor)
        {
            if (!isInteracting)
            {
                return;
            }

            isInteracting = false;
            hasCompleted = true;
            InteractionCompleted?.Invoke(this, actor);
        }

        protected void CancelInteraction(GameObject actor)
        {
            if (!isInteracting)
            {
                return;
            }

            isInteracting = false;
            InteractionCancelled?.Invoke(this, actor);
        }

        protected virtual void OnDisable()
        {
            isInteracting = false;
        }
    }
}
