using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class InteractionTrigger : MonoBehaviour
    {
        [SerializeField] private InteractableObject interactable;

        private readonly Dictionary<InteractionController, int> overlapCounts = new Dictionary<InteractionController, int>(2);

        public event Action<GameObject> ActorEntered;
        public event Action<GameObject> ActorExited;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (interactable == null)
            {
                interactable = GetComponent<InteractableObject>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (interactable == null)
            {
                return;
            }

            InteractionController controller = other.GetComponentInParent<InteractionController>();
            if (controller == null)
            {
                return;
            }

            int count;
            overlapCounts.TryGetValue(controller, out count);
            overlapCounts[controller] = count + 1;

            if (count == 0)
            {
                controller.Register(interactable);
                ActorEntered?.Invoke(controller.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            InteractionController controller = other.GetComponentInParent<InteractionController>();
            if (controller == null)
            {
                return;
            }

            int count;
            if (!overlapCounts.TryGetValue(controller, out count))
            {
                return;
            }

            count--;
            if (count <= 0)
            {
                overlapCounts.Remove(controller);
                controller.Unregister(interactable);
                ActorExited?.Invoke(controller.gameObject);
            }
            else
            {
                overlapCounts[controller] = count;
            }
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<InteractionController, int> entry in overlapCounts)
            {
                if (entry.Key != null && interactable != null)
                {
                    entry.Key.Unregister(interactable);
                    ActorExited?.Invoke(entry.Key.gameObject);
                }
            }

            overlapCounts.Clear();
        }
    }
}
