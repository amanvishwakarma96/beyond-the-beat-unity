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
        [SerializeField, Min(0f)] private float motorTorque = 1800f;
        [SerializeField, Min(0f)] private float brakeTorque = 3500f;
        [SerializeField, Min(1f)] private float maxForwardSpeedKph = 110f;
        [SerializeField, Min(1f)] private float maxReverseSpeedKph = 25f;
        [SerializeField, Min(0f)] private float directionChangeBrakeTorque = 2200f;

        [Header("Steering")]
        [SerializeField, Range(1f, 45f)] private float maxSteerAngle = 32f;
        [SerializeField, Min(0.1f)] private float steeringResponse = 7f;
        [SerializeField, Min(0f)] private float highSpeedSteerStartKph = 45f;
        [SerializeField, Range(0.1f, 1f)] private float highSpeedSteerMultiplier = 0.45f;

        [Header("Chassis")]
        [SerializeField, Min(100f)] private float vehicleMass = 1250f;
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
        [SerializeField, Min(0f)] private float linearDrag = 0.02f;
        [SerializeField, Min(0f)] private float angularDrag = 0.5f;
        [SerializeField, Min(0f)] private float downforceCoefficient = 18f;

        [Header("Wheel & Suspension")]
        [SerializeField, Min(0.05f)] private float wheelRadius = 0.34f;
        [SerializeField, Min(1f)] private float wheelMass = 28f;
        [SerializeField, Min(0.01f)] private float suspensionDistance = 0.22f;
        [SerializeField, Min(100f)] private float suspensionSpring = 35000f;
        [SerializeField, Min(100f)] private float suspensionDamper = 4500f;
        [SerializeField, Range(0f, 1f)] private float suspensionTargetPosition = 0.5f;
        [SerializeField, Min(0f)] private float forceAppPointDistance = 0.25f;
        [SerializeField, Min(0.1f)] private float forwardFrictionStiffness = 1.35f;
        [SerializeField, Min(0.1f)] private float sidewaysFrictionStiffness = 1.55f;

        [Header("Simulation")]
        [SerializeField, Min(0.1f)] private float substepSpeedThreshold = 5f;
        [SerializeField, Range(1, 20)] private int substepsBelowThreshold = 12;
        [SerializeField, Range(1, 20)] private int substepsAboveThreshold = 15;

        private Rigidbody body;
        private float steeringInput;
        private float throttleInput;
        private float brakeInput;
        private float currentSteerAngle;

        public float CurrentSpeedKph { get; private set; }

        public bool IsGrounded =>
            (frontLeftCollider != null && frontLeftCollider.isGrounded) ||
            (frontRightCollider != null && frontRightCollider.isGrounded) ||
            (rearLeftCollider != null && rearLeftCollider.isGrounded) ||
            (rearRightCollider != null && rearRightCollider.isGrounded);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ApplyChassisTuning();
            ApplyWheelTuning();
        }

        private void FixedUpdate()
        {
            if (!HasRequiredWheelReferences())
            {
                return;
            }

            CurrentSpeedKph = body.velocity.magnitude * 3.6f;

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

        private void ApplySteering()
        {
            float speedBlend = highSpeedSteerStartKph <= 0f
                ? 1f
                : Mathf.Clamp01(CurrentSpeedKph / highSpeedSteerStartKph);

            float steerMultiplier = Mathf.Lerp(1f, highSpeedSteerMultiplier, speedBlend);
            float targetSteerAngle = steeringInput * maxSteerAngle * steerMultiplier;
            currentSteerAngle = Mathf.MoveTowards(
                currentSteerAngle,
                targetSteerAngle,
                steeringResponse * maxSteerAngle * Time.fixedDeltaTime);

            frontLeftCollider.steerAngle = currentSteerAngle;
            frontRightCollider.steerAngle = currentSteerAngle;
        }

        private void ApplyDriveAndBrakes()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);
            bool movingForward = localVelocity.z > 0.5f;
            bool movingBackward = localVelocity.z < -0.5f;
            bool requestingForward = throttleInput > 0.01f;
            bool requestingReverse = throttleInput < -0.01f;
            bool changingDirection =
                (requestingForward && movingBackward) ||
                (requestingReverse && movingForward);

            float appliedBrakeTorque = brakeInput * brakeTorque;
            float requestedAxleTorque = 0f;

            if (brakeInput > 0.01f)
            {
                // Explicit braking always wins over throttle input.
            }
            else if (changingDirection)
            {
                appliedBrakeTorque = Mathf.Max(appliedBrakeTorque, directionChangeBrakeTorque);
            }
            else if (requestingForward && CurrentSpeedKph < maxForwardSpeedKph)
            {
                requestedAxleTorque = throttleInput * motorTorque;
            }
            else if (requestingReverse && CurrentSpeedKph < maxReverseSpeedKph)
            {
                requestedAxleTorque = throttleInput * motorTorque;
            }

            // Rear-wheel drive keeps the prototype easy to reason about while the front wheels steer.
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

            float speedMetersPerSecond = body.velocity.magnitude;
            body.AddForce(-transform.up * downforceCoefficient * speedMetersPerSecond, ForceMode.Force);
        }

        private void ApplyChassisTuning()
        {
            body.mass = vehicleMass;
            body.drag = linearDrag;
            body.angularDrag = angularDrag;
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

            WheelFrictionCurve forwardFriction = wheel.forwardFriction;
            forwardFriction.stiffness = forwardFrictionStiffness;
            wheel.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.stiffness = sidewaysFrictionStiffness;
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
