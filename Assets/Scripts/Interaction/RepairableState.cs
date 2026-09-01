using System;
using UnityEngine;

namespace BeyondTheBeat.Interaction
{
    public sealed class RepairableState : MonoBehaviour
    {
        [Header("Repairable State")]
        [SerializeField] private string repairableId = "repairable";
        [SerializeField, Range(0f, 1f)] private float damage01 = 0.5f;

        public string RepairableId => repairableId;
        public float Damage01 => Mathf.Clamp01(damage01);
        public float Condition01 => 1f - Damage01;
        public bool NeedsRepair => Damage01 > 0.0001f;

        public event Action<RepairableState, float> DamageChanged;
        public event Action<RepairableState> FullyRepaired;

        public bool SetDamage01(float value)
        {
            float next = Mathf.Clamp01(value);
            float previous = Damage01;
            if (Mathf.Approximately(previous, next))
            {
                return false;
            }

            damage01 = next;
            DamageChanged?.Invoke(this, Damage01);

            if (previous > 0.0001f && !NeedsRepair)
            {
                FullyRepaired?.Invoke(this);
            }

            return true;
        }

        public bool ApplyDamage01(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            return SetDamage01(Damage01 + amount);
        }

        public bool RepairFully()
        {
            return SetDamage01(0f);
        }
    }
}
