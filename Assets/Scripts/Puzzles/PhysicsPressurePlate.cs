using System.Collections.Generic;
using UnityEngine;

namespace BeyondTheBeat.Puzzles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class PhysicsPressurePlate : MonoBehaviour
    {
        [SerializeField] private PuzzleStateController puzzleState;
        [SerializeField, Min(0.01f)] private float requiredMass = 1f;
        [SerializeField] private bool resetWhenBelowRequirement = true;

        private readonly Dictionary<Rigidbody, int> overlapCounts = new Dictionary<Rigidbody, int>(4);

        public PuzzleStateController PuzzleState => puzzleState;
        public float RequiredMass => requiredMass;
        public float CurrentMass => CalculateCurrentMass();
        public int OccupantBodyCount => overlapCounts.Count;

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnEnable()
        {
            EnsureTrigger();
            Reevaluate();
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other != null ? other.attachedRigidbody : null;
            if (body == null)
            {
                return;
            }

            overlapCounts.TryGetValue(body, out int count);
            overlapCounts[body] = count + 1;
            Reevaluate();
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody body = other != null ? other.attachedRigidbody : null;
            if (body == null || !overlapCounts.TryGetValue(body, out int count))
            {
                return;
            }

            count--;
            if (count <= 0)
            {
                overlapCounts.Remove(body);
            }
            else
            {
                overlapCounts[body] = count;
            }

            Reevaluate();
        }

        private void OnDisable()
        {
            overlapCounts.Clear();
            if (resetWhenBelowRequirement && puzzleState != null)
            {
                puzzleState.ResetPuzzle();
            }
        }

        public bool MeetsRequirement(float totalMass)
        {
            return totalMass + 0.0001f >= requiredMass;
        }

        public void Reevaluate()
        {
            if (puzzleState == null)
            {
                return;
            }

            bool requirementMet = MeetsRequirement(CalculateCurrentMass());
            if (requirementMet)
            {
                puzzleState.SetSolved(true);
            }
            else if (resetWhenBelowRequirement)
            {
                puzzleState.ResetPuzzle();
            }
        }

        private float CalculateCurrentMass()
        {
            float totalMass = 0f;
            foreach (KeyValuePair<Rigidbody, int> entry in overlapCounts)
            {
                if (entry.Key != null && entry.Value > 0)
                {
                    totalMass += Mathf.Max(0f, entry.Key.mass);
                }
            }

            return totalMass;
        }

        private void EnsureTrigger()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }
    }
}
