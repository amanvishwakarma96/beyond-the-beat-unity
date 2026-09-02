using System;
using BeyondTheBeat.World;
using UnityEngine;

namespace BeyondTheBeat.Water
{
    public enum AquaticState
    {
        Dry = 0,
        Surface = 1,
        Underwater = 2
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SwimController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private WaterVolume waterVolume;
        [SerializeField] private Transform movementReference;

        [Header("Horizontal Movement")]
        [SerializeField, Min(0.1f)] private float surfaceSwimSpeed = 4f;
        [SerializeField, Min(0.1f)] private float underwaterSwimSpeed = 3.25f;
        [SerializeField, Min(0.1f)] private float acceleration = 8f;

        [Header("Depth Control")]
        [SerializeField, Min(0.05f)] private float surfaceDepth = 0.55f;
        [SerializeField, Min(0.1f)] private float targetDiveDepth = 3f;
        [SerializeField, Min(0.1f)] private float verticalSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float verticalResponsiveness = 3f;
        [SerializeField, Min(0.05f)] private float bottomClearance = 0.75f;

        [Header("Water Physics")]
        [SerializeField, Min(0f)] private float waterLinearDamping = 2f;
        [SerializeField] private bool enterWaterOnEnableWhenInside = true;

        private Vector2 moveInput;
        private bool diveRequested;
        private bool isInWater;
        private bool defaultsCaptured;
        private bool defaultUseGravity;
        private float defaultLinearDamping;
        private AquaticState state = AquaticState.Dry;

        public Rigidbody Body => body;
        public WaterVolume WaterVolume => waterVolume;
        public Transform MovementReference => movementReference;
        public AquaticState State => state;
        public bool IsInWater => isInWater;
        public bool DiveRequested => diveRequested;
        public Vector2 MoveInput => moveInput;
        public float SurfaceDepth => Mathf.Max(0.05f, surfaceDepth);
        public float RequestedDiveDepth => Mathf.Max(0.1f, targetDiveDepth);
        public float MaxAllowedDiveDepth => CalculateMaxAllowedDiveDepth();
        public float ActiveTargetDepth => state == AquaticState.Underwater
            ? Mathf.Min(RequestedDiveDepth, MaxAllowedDiveDepth)
            : SurfaceDepth;

        public event Action<AquaticState, AquaticState> StateChanged;
        public event Action<WaterVolume> WaterEntered;
        public event Action<WaterVolume> WaterExited;

        private void Awake()
        {
            ResolveReferences();
            CaptureBodyDefaults();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureBodyDefaults();
            SubscribeToWaterVolume();

            if (enterWaterOnEnableWhenInside && waterVolume != null && IsInsideBoundWater())
            {
                EnterWater(waterVolume);
            }
        }

        private void FixedUpdate()
        {
            if (!isInWater || waterVolume == null || body == null)
            {
                return;
            }

            if (!waterVolume.ContainsHorizontalPosition(body.position))
            {
                ExitWater();
                return;
            }

            Vector3 targetVelocity = GetTargetVelocity(body.position);
            float blend = Mathf.Clamp01(acceleration * Time.fixedDeltaTime);
            body.linearVelocity = Vector3.Lerp(body.linearVelocity, targetVelocity, blend);
        }

        private void OnDisable()
        {
            UnsubscribeFromWaterVolume();
            ExitWater();
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void ClearInput()
        {
            moveInput = Vector2.zero;
            diveRequested = false;
            RefreshState();
        }

        public bool SetDiveRequested(bool requested)
        {
            if (diveRequested == requested)
            {
                return false;
            }

            diveRequested = requested;
            RefreshState();
            return true;
        }

        public bool ToggleDive()
        {
            return SetDiveRequested(!diveRequested);
        }

        public float SetTargetDiveDepth(float depth)
        {
            targetDiveDepth = Mathf.Clamp(depth, 0.1f, CalculateMaxAllowedDiveDepth());
            return targetDiveDepth;
        }

        public bool BindWaterVolume(WaterVolume volume, bool enterIfInside = true)
        {
            if (waterVolume == volume)
            {
                if (enterIfInside && volume != null && IsInsideBoundWater())
                {
                    return EnterWater(volume);
                }

                return false;
            }

            UnsubscribeFromWaterVolume();
            if (isInWater)
            {
                ExitWater();
            }

            waterVolume = volume;
            SubscribeToWaterVolume();

            if (enterIfInside && waterVolume != null && IsInsideBoundWater())
            {
                return EnterWater(waterVolume);
            }

            return true;
        }

        public bool EnterWater(WaterVolume volume)
        {
            if (volume == null || body == null)
            {
                return false;
            }

            if (waterVolume != volume)
            {
                UnsubscribeFromWaterVolume();
                waterVolume = volume;
                SubscribeToWaterVolume();
            }

            CaptureBodyDefaults();
            bool changed = !isInWater;
            isInWater = true;
            body.useGravity = false;
            body.linearDamping = Mathf.Max(0f, waterLinearDamping);
            RefreshState();

            if (changed)
            {
                WaterEntered?.Invoke(waterVolume);
            }

            return changed;
        }

        public bool ExitWater()
        {
            if (!isInWater)
            {
                SetState(AquaticState.Dry);
                return false;
            }

            WaterVolume exitedVolume = waterVolume;
            isInWater = false;
            diveRequested = false;
            moveInput = Vector2.zero;
            RestoreBodyDefaults();
            SetState(AquaticState.Dry);
            WaterExited?.Invoke(exitedVolume);
            return true;
        }

        public Vector3 GetTargetVelocity(Vector3 worldPosition)
        {
            if (!isInWater || waterVolume == null)
            {
                return Vector3.zero;
            }

            Transform reference = movementReference != null ? movementReference : transform;
            Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up);

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }
            else
            {
                right.Normalize();
            }

            Vector3 horizontalDirection = right * moveInput.x + forward * moveInput.y;
            if (horizontalDirection.sqrMagnitude > 1f)
            {
                horizontalDirection.Normalize();
            }

            float horizontalSpeed = state == AquaticState.Underwater
                ? Mathf.Max(0.1f, underwaterSwimSpeed)
                : Mathf.Max(0.1f, surfaceSwimSpeed);

            float targetY = waterVolume.SurfaceY - ActiveTargetDepth;
            float verticalError = targetY - worldPosition.y;
            float desiredVerticalVelocity = Mathf.Clamp(
                verticalError * Mathf.Max(0.1f, verticalResponsiveness),
                -Mathf.Max(0.1f, verticalSpeed),
                Mathf.Max(0.1f, verticalSpeed));

            return horizontalDirection * horizontalSpeed + Vector3.up * desiredVerticalVelocity;
        }

        private void HandleWaterActorEntered(ZoneContext context, GameObject actor)
        {
            if (waterVolume == null || context != waterVolume.ZoneContext || !IsSelfActor(actor))
            {
                return;
            }

            EnterWater(waterVolume);
        }

        private void HandleWaterActorExited(ZoneContext context, GameObject actor)
        {
            if (waterVolume == null || context != waterVolume.ZoneContext || !IsSelfActor(actor))
            {
                return;
            }

            ExitWater();
        }

        private bool IsSelfActor(GameObject actor)
        {
            return actor != null && (actor == gameObject || (body != null && actor == body.gameObject));
        }

        private bool IsInsideBoundWater()
        {
            return waterVolume != null && body != null && waterVolume.ContainsPoint(body.position);
        }

        private void RefreshState()
        {
            if (!isInWater || waterVolume == null)
            {
                SetState(AquaticState.Dry);
                return;
            }

            bool canDive = MaxAllowedDiveDepth > SurfaceDepth + 0.1f;
            SetState(diveRequested && canDive ? AquaticState.Underwater : AquaticState.Surface);
        }

        private float CalculateMaxAllowedDiveDepth()
        {
            if (waterVolume == null)
            {
                return Mathf.Max(0.1f, targetDiveDepth);
            }

            float maxDepth = waterVolume.MaxDepth - Mathf.Max(0.05f, bottomClearance);
            return Mathf.Max(SurfaceDepth, maxDepth);
        }

        private void SetState(AquaticState next)
        {
            if (state == next)
            {
                return;
            }

            AquaticState previous = state;
            state = next;
            StateChanged?.Invoke(previous, state);
        }

        private void ResolveReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (movementReference == null)
            {
                movementReference = transform;
            }
        }

        private void CaptureBodyDefaults()
        {
            if (defaultsCaptured || body == null)
            {
                return;
            }

            defaultUseGravity = body.useGravity;
            defaultLinearDamping = body.linearDamping;
            defaultsCaptured = true;
        }

        private void RestoreBodyDefaults()
        {
            if (!defaultsCaptured || body == null)
            {
                return;
            }

            body.useGravity = defaultUseGravity;
            body.linearDamping = defaultLinearDamping;
        }

        private void SubscribeToWaterVolume()
        {
            ZoneContext context = waterVolume != null ? waterVolume.ZoneContext : null;
            if (context == null)
            {
                return;
            }

            context.ActorEntered -= HandleWaterActorEntered;
            context.ActorExited -= HandleWaterActorExited;
            context.ActorEntered += HandleWaterActorEntered;
            context.ActorExited += HandleWaterActorExited;
        }

        private void UnsubscribeFromWaterVolume()
        {
            ZoneContext context = waterVolume != null ? waterVolume.ZoneContext : null;
            if (context == null)
            {
                return;
            }

            context.ActorEntered -= HandleWaterActorEntered;
            context.ActorExited -= HandleWaterActorExited;
        }
    }
}
