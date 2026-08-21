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
        private GameObject currentActor;

        public string PromptText => promptText;
        public bool IsInteracting => isInteracting;
        public bool HasCompleted => hasCompleted;
        public GameObject CurrentActor => currentActor;

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

            currentActor = actor;
            isInteracting = true;
            InteractionRequested?.Invoke(this, actor);
            OnInteractionRequested(actor);
            return true;
        }

        public bool CancelInteraction(GameObject actor)
        {
            if (!isInteracting)
            {
                return false;
            }

            GameObject cancelledActor = currentActor != null ? currentActor : actor;
            isInteracting = false;
            currentActor = null;
            OnInteractionCancelled(cancelledActor);
            InteractionCancelled?.Invoke(this, cancelledActor);
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

        protected virtual void OnInteractionCancelled(GameObject actor)
        {
        }

        protected void CompleteInteraction(GameObject actor)
        {
            if (!isInteracting)
            {
                return;
            }

            GameObject completedActor = currentActor != null ? currentActor : actor;
            isInteracting = false;
            hasCompleted = true;
            currentActor = null;
            InteractionCompleted?.Invoke(this, completedActor);
        }

        protected virtual void OnDisable()
        {
            if (isInteracting)
            {
                CancelInteraction(currentActor);
            }
        }
    }
}
