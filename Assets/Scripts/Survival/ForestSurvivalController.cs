using System;
using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Survival
{
    public enum SurvivalExitMode
    {
        Pause = 0,
        RecoverOverTime = 1,
        ResetToMax = 2
    }

    [DisallowMultipleComponent]
    public sealed class ForestSurvivalController : MonoBehaviour
    {
        [Header("Context")]
        [SerializeField] private ZoneContext forestZone;
        [SerializeField] private GameObject playerActor;
        [SerializeField] private SurvivalResource resource;

        [Header("Pressure")]
        [SerializeField, Min(0f)] private float drainPerSecond = 4f;
        [SerializeField] private SurvivalExitMode exitMode = SurvivalExitMode.RecoverOverTime;
        [SerializeField, Min(0f)] private float recoveryPerSecond = 12f;

        private bool pressureActive;
        private bool recovering;

        public ZoneContext ForestZone => forestZone;
        public GameObject PlayerActor => playerActor;
        public SurvivalResource Resource => resource;
        public float DrainPerSecond => drainPerSecond;
        public float RecoveryPerSecond => recoveryPerSecond;
        public SurvivalExitMode ExitMode => exitMode;
        public bool IsPressureActive => pressureActive;
        public bool IsRecovering => recovering;

        public event Action<bool> PressureChanged;

        private void OnEnable()
        {
            Subscribe();
            RefreshConfiguredContext();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetPressureActive(false);
            recovering = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool TryEnterContext(ZoneContext zone, GameObject actor)
        {
            if (!MatchesConfiguredForest(zone, actor))
            {
                return false;
            }

            recovering = false;
            SetPressureActive(true);
            return true;
        }

        public bool TryExitContext(ZoneContext zone, GameObject actor)
        {
            if (!MatchesConfiguredForest(zone, actor))
            {
                return false;
            }

            SetPressureActive(false);
            ApplyExitMode();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || resource == null || deltaTime <= 0f)
            {
                return;
            }

            if (pressureActive)
            {
                if (drainPerSecond > 0f)
                {
                    resource.Drain(drainPerSecond * deltaTime);
                }

                return;
            }

            if (!recovering || exitMode != SurvivalExitMode.RecoverOverTime)
            {
                return;
            }

            if (resource.CurrentValue >= resource.MaxValue)
            {
                recovering = false;
                return;
            }

            if (recoveryPerSecond > 0f)
            {
                resource.Recover(recoveryPerSecond * deltaTime);
            }

            if (resource.CurrentValue >= resource.MaxValue)
            {
                recovering = false;
            }
        }

        public void ResetResource()
        {
            SetPressureActive(false);
            recovering = false;
            resource?.ResetToStartingValue();
        }

        private void HandleActorEntered(ZoneContext zone, GameObject actor)
        {
            TryEnterContext(zone, actor);
        }

        private void HandleActorExited(ZoneContext zone, GameObject actor)
        {
            TryExitContext(zone, actor);
        }

        private void RefreshConfiguredContext()
        {
            if (forestZone != null &&
                playerActor != null &&
                forestZone.ZoneType == WorldZoneType.Forest &&
                forestZone.IsActorInside(playerActor))
            {
                recovering = false;
                SetPressureActive(true);
            }
        }

        private bool MatchesConfiguredForest(ZoneContext zone, GameObject actor)
        {
            return zone != null &&
                   zone == forestZone &&
                   zone.ZoneType == WorldZoneType.Forest &&
                   actor != null &&
                   actor == playerActor;
        }

        private void ApplyExitMode()
        {
            recovering = false;
            if (resource == null)
            {
                return;
            }

            switch (exitMode)
            {
                case SurvivalExitMode.RecoverOverTime:
                    recovering = resource.CurrentValue < resource.MaxValue;
                    break;
                case SurvivalExitMode.ResetToMax:
                    resource.ResetToMax();
                    break;
                case SurvivalExitMode.Pause:
                default:
                    break;
            }
        }

        private void SetPressureActive(bool active)
        {
            if (pressureActive == active)
            {
                return;
            }

            pressureActive = active;
            PressureChanged?.Invoke(pressureActive);
        }

        private void Subscribe()
        {
            if (forestZone == null)
            {
                return;
            }

            forestZone.ActorEntered -= HandleActorEntered;
            forestZone.ActorEntered += HandleActorEntered;
            forestZone.ActorExited -= HandleActorExited;
            forestZone.ActorExited += HandleActorExited;
        }

        private void Unsubscribe()
        {
            if (forestZone == null)
            {
                return;
            }

            forestZone.ActorEntered -= HandleActorEntered;
            forestZone.ActorExited -= HandleActorExited;
        }
    }
}
