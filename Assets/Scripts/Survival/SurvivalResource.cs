using System;
using UnityEngine;

namespace BeyondTheBeat.Survival
{
    [DisallowMultipleComponent]
    public sealed class SurvivalResource : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float maxValue = 100f;
        [SerializeField, Min(0f)] private float startingValue = 100f;
        [SerializeField, Min(0f)] private float currentValue = 100f;

        public float MaxValue => maxValue;
        public float StartingValue => startingValue;
        public float CurrentValue => currentValue;
        public float NormalizedValue => maxValue > 0f ? currentValue / maxValue : 0f;
        public bool IsDepleted => currentValue <= 0f;

        public event Action<float, float> ValueChanged;
        public event Action Depleted;
        public event Action Recovered;

        private void Awake()
        {
            NormalizeConfiguration();
            currentValue = Mathf.Clamp(startingValue, 0f, maxValue);
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
            currentValue = Mathf.Clamp(currentValue, 0f, maxValue);
        }

        public void Configure(float configuredMaxValue, float configuredStartingValue)
        {
            maxValue = Mathf.Max(0.01f, configuredMaxValue);
            startingValue = Mathf.Clamp(configuredStartingValue, 0f, maxValue);
            ResetToStartingValue();
        }

        public bool SetValue(float value)
        {
            float clampedValue = Mathf.Clamp(value, 0f, maxValue);
            if (Mathf.Approximately(clampedValue, currentValue))
            {
                return false;
            }

            bool wasDepleted = IsDepleted;
            currentValue = clampedValue;
            ValueChanged?.Invoke(currentValue, maxValue);

            if (!wasDepleted && IsDepleted)
            {
                Depleted?.Invoke();
            }
            else if (wasDepleted && !IsDepleted)
            {
                Recovered?.Invoke();
            }

            return true;
        }

        public bool Drain(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            return SetValue(currentValue - amount);
        }

        public bool Recover(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            return SetValue(currentValue + amount);
        }

        public void ResetToStartingValue()
        {
            SetValue(startingValue);
        }

        public void ResetToMax()
        {
            SetValue(maxValue);
        }

        private void NormalizeConfiguration()
        {
            maxValue = Mathf.Max(0.01f, maxValue);
            startingValue = Mathf.Clamp(startingValue, 0f, maxValue);
        }
    }
}
