using System;
using UnityEngine;

namespace BeyondTheBeat.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider frontLeftCollider;
        [SerializeField] private WheelCollider frontRightCollider;
        [SerializeField] private WheelCollider rearLeftCollider;
        [SerializeField] private WheelCollider rearRightCollider;

        [Header("Wheel Visuals")]
        [SerializeField] private Transform frontLeftVisual;
        [SerializeField] private Transform frontRightVisual;
        [SerializeField] private Transform rearLeftVisual;
        [SerializeField] private Transform rearRightVisual;

        [Header("Power & Braking")]
        [SerializeField, Min(0f)] private float motorTorque = 1700f;
        [SerializeField, Min(0f)] private float brakeTorque = 3800f;
        [SerializeField, Min(1f)] private float maxForwardSpeedKph = 110f;
        [SerializeField, Min(1f)] private float maxReverseSpeedKph = 25f;
        [SerializeField, Min(0f)] private float directionChangeBrakeTorque = 2600f;

        [Header("Steering")]
        [SerializeField, Range(1f, 45f)] private float maxSteerAngle = 30f;
        [SerializeField, Min(0.1f)] private float steeringResponse = 6f;
        [SerializeField, Min(0f)] private float highSpeedSteerStartKph = 5f;
        [SerializeField, Min(0f)] private float highSpeedSteerFullKph = 50f;
        [SerializeField, Range(0.1f, 1f)] private float highSpeedSteerMultiplier = 0.38f;

        [Header("Chassis")]
        [SerializeField, Min(100f)] private float vehicleMass = 1250f;
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);
        [SerializeField, Min(0f)] private float linearDrag = 0.02f;
        [SerializeField, Min(0f)] private float angularDrag = 0.6f;
        [SerializeField, Min(0f)] private float downforceCoefficient = 20f;

        [Header("Wheel & Suspension")]
        [SerializeField, Min(0.05f)] private float wheelRadius = 0.34f;
        [SerializeField, Min(1f)] private float wheelMass = 28f;
        [SerializeField, Min(0.01f)] private float suspensionDistance = 0.22f;
        [SerializeField, Min(100f)] private float suspensionSpring = 35000f;
        [SerializeField, Min(100f)] private float suspensionDamper = 5000f;
        [SerializeField, Range(0f, 1f)] private float suspensionTargetPosition = 0.5f;
        [SerializeField, Min(0f)] private float forceAppPointDistance = 0.25f;
        [SerializeField, Min(0.1f)] private float forwardFrictionStiffness = 1.4f;
        [SerializeField, Min(0.1f)] private float sidewaysFrictionStiffness = 1.6f;

        [Header("Vehicle Condition")]
        [SerializeField, Range(0f, 1f)] private float healthCondition = 1f;
        [SerializeField, Range(0f, 1f)] private float tireWear;
        [SerializeField, Min(1f)] private float offRoadWearStartSpeedKph = 45f;
        [SerializeField, Min(0f)] private float offRoadHealthWearPerSecond = 0.0025f;
        [SerializeField, Min(0f)] private float offRoadTireWearPerSecond = 0.008f;
        [SerializeField, Min(0f)] private float severeCollisionSpeed = 8f;
        [SerializeField, Min(0f)] private float collisionHealthDamagePerMeterPerSecond = 0.018f;
        [SerializeField, Range(0f, 5f)] private float maxSteeringDriftDegrees = 1.8f;

        [Header("Simulation")]
        [SerializeField, Min(0.1f)] private float substepSpeedThreshold = 5f;
        [SerializeField, Range(1, 20)] private int substepsBelowThreshold = 12;
        [SerializeField, Range(1, 20)] private int substepsAboveThreshold = 15;

        private Rigidbody body;
        private float steeringInput;
        private float throttleInput;
        private float brakeInput;
        private float currentSteerAngle;
        private bool offRoadSurfaceActive;

        public float CurrentSpeedKph { get; private set; }

        public float MotorTorque => motorTorque;
        public float BrakeTorque => brakeTorque;
        public float MaxSteerAngle => maxSteerAngle;
        public float SteeringResponse => steeringResponse;
        public float HighSpeedSteerStartKph => highSpeedSteerStartKph;
        public float HighSpeedSteerFullKph => highSpeedSteerFullKph;
        public float HighSpeedSteerMultiplier => highSpeedSteerMultiplier;
        public float SuspensionSpring => suspensionSpring;
        public float SuspensionDamper => suspensionDamper;
        public float ForwardFrictionStiffness => forwardFrictionStiffness;
        public float SidewaysFrictionStiffness => sidewaysFrictionStiffness;
        public Vector3 CenterOfMassOffset => centerOfMassOffset;
        public float DownforceCoefficient => downforceCoefficient;

        public float HealthCondition => Mathf.Clamp01(healthCondition);
        public float TireWear => Mathf.Clamp01(tireWear);
        public float TireTraction01 => 1f - TireWear;
        public float ServiceNeed01 => Mathf.Max(1f - HealthCondition, TireWear);
        public bool NeedsService => ServiceNeed01 > 0.0001f;
        public bool OffRoadSurfaceActive => offRoadSurfaceActive;
        public float EffectiveAccelerationMultiplier => EvaluateAccelerationMultiplier(HealthCondition);
        public float EffectiveBrakeMultiplier => EvaluateBrakeMultiplier(HealthCondition, TireWear);
        public float EffectiveTractionMultiplier => EvaluateTractionMultiplier(TireWear);

        public event Action<VehicleController, float, float> ConditionChanged;

        public bool IsGrounded =>
            (frontLeftCollider != null && frontLeftCollider.isGrounded) ||
            (frontRightCollider != null && frontRightCollider.isGrounded) ||
            (rearLeftCollider != null && rearLeftCollider.isGrounded) ||
            (rearRightCollider != null && rearRightCollider.isGrounded);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            healthCondition = Mathf.Clamp01(healthCondition);
            tireWear = Mathf.Clamp01(tireWear);
            ApplyChassisTuning();
            ApplyWheelTuning();
        }

        private void FixedUpdate()
        {
            if (!HasRequiredWheelReferences())
            {
                return;
            }

            CurrentSpeedKph = body.linearVelocity.magnitude * 3.6f;
            ApplyOffRoadWear();
            ApplySteering();
            ApplyDriveAndBrakes();
            ApplyDownforce();
        }

        private void LateUpdate()
        {
            UpdateWheelVisual(frontLeftCollider, frontLeftVisual);
            UpdateWheelVisual(frontRightCollider, frontRightVisual);
            UpdateWheelVisual(rearLeftCollider, rearLeftVisual);
            UpdateWheelVisual(rearRightCollider, rearRightVisual);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
            {
                return;
            }

            float damage = EvaluateCollisionHealthDamage(
                collision.relativeVelocity.magnitude,
                severeCollisionSpeed,
                collisionHealthDamagePerMeterPerSecond);
            if (damage <= 0f)
            {
                return;
            }

            ApplyConditionWear(damage, damage * 0.25f);
        }

        public void SetInput(float steering, float throttle, float brake)
        {
            steeringInput = Mathf.Clamp(steering, -1f, 1f);
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            brakeInput = Mathf.Clamp01(brake);
        }

        public void ClearInput()
        {
            steeringInput = 0f;
            throttleInput = 0f;
            brakeInput = 0f;
        }

        public void ReapplyTuning()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            ApplyChassisTuning();
            ApplyWheelTuning();
        }

        public void SetOffRoadState(bool active)
        {
            offRoadSurfaceActive = active;
        }

        public bool RestoreCondition(float health, float wear)
        {
            return SetConditionInternal(health, wear);
        }

        public bool ApplyConditionWear(float healthDamage, float tireWearAmount)
        {
            if (healthDamage <= 0f && tireWearAmount <= 0f)
            {
                return false;
            }

            return SetConditionInternal(
                HealthCondition - Mathf.Max(0f, healthDamage),
                TireWear + Mathf.Max(0f, tireWearAmount));
        }

        public bool RepairCondition(float healthRestore, float tireWearReduction)
        {
            if (healthRestore <= 0f && tireWearReduction <= 0f)
            {
                return false;
            }

            return SetConditionInternal(
                HealthCondition + Mathf.Max(0f, healthRestore),
                TireWear - Mathf.Max(0f, tireWearReduction));
        }

        public static float EvaluateAccelerationMultiplier(float health)
        {
            return Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(health));
        }

        public static float EvaluateBrakeMultiplier(float health, float wear)
        {
            float healthFactor = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(health));
            float tireFactor = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(wear));
            return Mathf.Clamp(healthFactor * tireFactor, 0.5f, 1f);
        }

        public static float EvaluateTractionMultiplier(float wear)
        {
            return Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(wear));
        }

        public static float EvaluateOffRoadWearIntensity(float speedKph, float wearStartSpeedKph)
        {
            if (speedKph <= wearStartSpeedKph)
            {
                return 0f;
            }

            return Mathf.Clamp01((speedKph - wearStartSpeedKph) / 60f);
        }

        public static float EvaluateCollisionHealthDamage(float impactSpeed, float threshold, float damagePerSpeed)
        {
            if (impactSpeed <= threshold || damagePerSpeed <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01((impactSpeed - threshold) * damagePerSpeed);
        }

        private void ApplyOffRoadWear()
        {
            if (!offRoadSurfaceActive || !IsGrounded)
            {
                return;
            }

            float intensity = EvaluateOffRoadWearIntensity(CurrentSpeedKph, offRoadWearStartSpeedKph);
            if (intensity <= 0f)
            {
                return;
            }

            ApplyConditionWear(
                offRoadHealthWearPerSecond * intensity * Time.fixedDeltaTime,
                offRoadTireWearPerSecond * intensity * Time.fixedDeltaTime);
        }

        private bool SetConditionInternal(float health, float wear)
        {
            float nextHealth = Mathf.Clamp01(health);
            float nextWear = Mathf.Clamp01(wear);
            if (Mathf.Approximately(nextHealth, HealthCondition) && Mathf.Approximately(nextWear, TireWear))
            {
                return false;
            }

            healthCondition = nextHealth;
            tireWear = nextWear;
            ApplyDynamicWheelFriction();
            ConditionChanged?.Invoke(this, HealthCondition, TireWear);
            return true;
        }

        private void ApplySteering()
        {
            float reductionBlend;
            if (highSpeedSteerFullKph <= highSpeedSteerStartKph)
            {
                reductionBlend = CurrentSpeedKph >= highSpeedSteerStartKph ? 1f : 0f;
            }
            else
            {
                reductionBlend = Mathf.InverseLerp(
                    highSpeedSteerStartKph,
                    highSpeedSteerFullKph,
                    CurrentSpeedKph);
            }

            float steerMultiplier = Mathf.Lerp(1f, highSpeedSteerMultiplier, reductionBlend);
            float wearDrift = Mathf.Sin(Time.fixedTime * 1.7f + GetEntityId() * 0.01f) *
                              maxSteeringDriftDegrees * TireWear;
            float targetSteerAngle = steeringInput * maxSteerAngle * steerMultiplier + wearDrift;
            targetSteerAngle = Mathf.Clamp(targetSteerAngle, -maxSteerAngle, maxSteerAngle);
            currentSteerAngle = Mathf.MoveTowards(
                currentSteerAngle,
                targetSteerAngle,
                steeringResponse * maxSteerAngle * Time.fixedDeltaTime);

            frontLeftCollider.steerAngle = currentSteerAngle;
            frontRightCollider.steerAngle = currentSteerAngle;
        }

        private void ApplyDriveAndBrakes()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            bool movingForward = localVelocity.z > 0.5f;
            bool movingBackward = localVelocity.z < -0.5f;
            bool requestingForward = throttleInput > 0.01f;
            bool requestingReverse = throttleInput < -0.01f;
            bool changingDirection =
                (requestingForward && movingBackward) ||
                (requestingReverse && movingForward);

            float effectiveBrakeTorque = brakeTorque * EffectiveBrakeMultiplier;
            float appliedBrakeTorque = brakeInput * effectiveBrakeTorque;
            float requestedAxleTorque = 0f;

            if (brakeInput > 0.01f)
            {
                // Explicit braking always wins over throttle input.
            }
            else if (changingDirection)
            {
                appliedBrakeTorque = Mathf.Max(
                    appliedBrakeTorque,
                    directionChangeBrakeTorque * EffectiveBrakeMultiplier);
            }
            else if (requestingForward && CurrentSpeedKph < maxForwardSpeedKph)
            {
                requestedAxleTorque = throttleInput * motorTorque * EffectiveAccelerationMultiplier;
            }
            else if (requestingReverse && CurrentSpeedKph < maxReverseSpeedKph)
            {
                requestedAxleTorque = throttleInput * motorTorque * EffectiveAccelerationMultiplier;
            }

            float torquePerDrivenWheel = requestedAxleTorque * 0.5f;
            rearLeftCollider.motorTorque = torquePerDrivenWheel;
            rearRightCollider.motorTorque = torquePerDrivenWheel;
            frontLeftCollider.motorTorque = 0f;
            frontRightCollider.motorTorque = 0f;

            frontLeftCollider.brakeTorque = appliedBrakeTorque;
            frontRightCollider.brakeTorque = appliedBrakeTorque;
            rearLeftCollider.brakeTorque = appliedBrakeTorque;
            rearRightCollider.brakeTorque = appliedBrakeTorque;
        }

        private void ApplyDownforce()
        {
            if (downforceCoefficient <= 0f || !IsGrounded)
            {
                return;
            }

            float speedMetersPerSecond = body.linearVelocity.magnitude;
            body.AddForce(-transform.up * downforceCoefficient * speedMetersPerSecond, ForceMode.Force);
        }

        private void ApplyChassisTuning()
        {
            body.mass = vehicleMass;
            body.linearDamping = linearDrag;
            body.angularDamping = angularDrag;
            body.centerOfMass = centerOfMassOffset;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void ApplyWheelTuning()
        {
            if (!HasRequiredWheelReferences())
            {
                return;
            }

            ConfigureWheel(frontLeftCollider);
            ConfigureWheel(frontRightCollider);
            ConfigureWheel(rearLeftCollider);
            ConfigureWheel(rearRightCollider);

            frontLeftCollider.ConfigureVehicleSubsteps(
                substepSpeedThreshold,
                substepsBelowThreshold,
                substepsAboveThreshold);
        }

        private void ConfigureWheel(WheelCollider wheel)
        {
            wheel.radius = wheelRadius;
            wheel.mass = wheelMass;
            wheel.suspensionDistance = suspensionDistance;
            wheel.forceAppPointDistance = forceAppPointDistance;

            JointSpring spring = wheel.suspensionSpring;
            spring.spring = suspensionSpring;
            spring.damper = suspensionDamper;
            spring.targetPosition = suspensionTargetPosition;
            wheel.suspensionSpring = spring;

            ApplyWheelFriction(wheel);
        }

        private void ApplyDynamicWheelFriction()
        {
            if (!HasRequiredWheelReferences())
            {
                return;
            }

            ApplyWheelFriction(frontLeftCollider);
            ApplyWheelFriction(frontRightCollider);
            ApplyWheelFriction(rearLeftCollider);
            ApplyWheelFriction(rearRightCollider);
        }

        private void ApplyWheelFriction(WheelCollider wheel)
        {
            float traction = EffectiveTractionMultiplier;

            WheelFrictionCurve forwardFriction = wheel.forwardFriction;
            forwardFriction.stiffness = forwardFrictionStiffness * traction;
            wheel.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.stiffness = sidewaysFrictionStiffness * traction;
            wheel.sidewaysFriction = sidewaysFriction;
        }

        private static void UpdateWheelVisual(WheelCollider wheel, Transform visual)
        {
            if (wheel == null || visual == null)
            {
                return;
            }

            wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
            visual.SetPositionAndRotation(position, rotation);
        }

        private bool HasRequiredWheelReferences()
        {
            return frontLeftCollider != null &&
                   frontRightCollider != null &&
                   rearLeftCollider != null &&
                   rearRightCollider != null;
        }
    }
}
