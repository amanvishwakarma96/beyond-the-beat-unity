using System;
using BeyondTheBeat.Vehicle;
using UnityEngine;
using UnityEngine.Events;

namespace BeyondTheBeat.Interaction
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(InteractionTrigger))]
    public sealed class ParkingZone : InteractableObject
    {
        [Header("Parking")]
        [SerializeField, Min(0f)] private float stopThresholdKph = 2f;
        [SerializeField] private string successMessage = "Parked successfully";
        [SerializeField] private UnityEvent onParkingCompleted = new UnityEvent();

        private InteractionTrigger interactionTrigger;
        private bool completedThisVisit;

        public float StopThresholdKph => stopThresholdKph;
        public string SuccessMessage => successMessage;
        public bool CompletedThisVisit => completedThisVisit;

        public event Action<ParkingZone, GameObject> ParkingCompleted;

        private void Awake()
        {
            interactionTrigger = GetComponent<InteractionTrigger>();
        }

        private void OnEnable()
        {
            if (interactionTrigger == null)
            {
                interactionTrigger = GetComponent<InteractionTrigger>();
            }

            if (interactionTrigger != null)
            {
                interactionTrigger.ActorExited -= HandleActorExited;
                interactionTrigger.ActorExited += HandleActorExited;
            }
        }

        protected override bool CanInteract(GameObject actor)
        {
            if (completedThisVisit || actor == null)
            {
                return false;
            }

            VehicleController vehicle = actor.GetComponent<VehicleController>();
            if (vehicle == null)
            {
                return false;
            }

            return vehicle.CurrentSpeedKph <= stopThresholdKph;
        }

        protected override void OnInteractionRequested(GameObject actor)
        {
            VehicleController vehicle = actor != null ? actor.GetComponent<VehicleController>() : null;
            if (vehicle == null || vehicle.CurrentSpeedKph > stopThresholdKph)
            {
                CancelInteraction(actor);
                return;
            }

            completedThisVisit = true;
            CompleteInteraction(actor);

            onParkingCompleted.Invoke();
            ParkingCompleted?.Invoke(this, actor);
            Debug.Log($"[Beyond The Beat] {successMessage}", this);
        }

        private void HandleActorExited(GameObject actor)
        {
            if (IsInteracting)
            {
                CancelInteraction(actor);
            }

            completedThisVisit = false;
            ResetCompletion();
        }

        protected override void OnDisable()
        {
            if (interactionTrigger != null)
            {
                interactionTrigger.ActorExited -= HandleActorExited;
            }

            completedThisVisit = false;
            base.OnDisable();
        }
    }
}
