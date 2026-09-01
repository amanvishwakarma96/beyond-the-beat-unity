using System;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(InteractionTrigger))]
    public sealed class RepairStation : TimedActivityInteractable
    {
        [Header("Repair")]
        [SerializeField] private RepairableState target;
        [SerializeField] private string activityLabel = "Repair vehicle";

        private int repairsCompleted;

        public RepairableState Target => target;
        public string ActivityLabel => activityLabel;
        public int RepairsCompleted => repairsCompleted;

        public event Action<RepairStation, RepairableState, GameObject, int> RepairCompleted;

        protected override bool CanInteract(GameObject actor)
        {
            return base.CanInteract(actor) && target != null && target.NeedsRepair;
        }

        protected override void OnActivityCompleted(GameObject actor)
        {
            if (target == null || !target.RepairFully())
            {
                return;
            }

            repairsCompleted++;
            RepairCompleted?.Invoke(this, target, actor, repairsCompleted);
        }
    }
}
