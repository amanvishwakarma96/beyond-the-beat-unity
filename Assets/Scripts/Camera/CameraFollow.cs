using UnityEngine;

namespace BeyondTheBeat.CameraSystem
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Position")]
        [SerializeField, Min(0.5f)] private float followDistance = 6.5f;
        [SerializeField, Min(0.1f)] private float followHeight = 3.4f;
        [SerializeField, Min(0f)] private float lateralOffset = 0f;
        [SerializeField, Min(0f)] private float lookAheadDistance = 2.0f;
        [SerializeField, Min(0f)] private float lookAtHeight = 1.1f;

        [Header("Smoothing")]
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.16f;
        [SerializeField, Min(0.01f)] private float rotationDamping = 8f;
        [SerializeField, Min(0f)] private float maxPositionSpeed = 60f;

        [Header("Heading")]
        [SerializeField, Range(0f, 1f)] private float targetUpInfluence = 0.15f;
        [SerializeField, Min(0.01f)] private float headingDamping = 10f;

        private Vector3 positionVelocity;
        private Vector3 smoothedForward;
        private bool initialized;

        public Transform Target => target;

        private void OnEnable()
        {
            initialized = false;
            positionVelocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (!initialized)
            {
                SnapToTarget();
                initialized = true;
                return;
            }

            UpdateHeading();
            UpdatePosition();
            UpdateRotation();
        }

        public void SetTarget(Transform newTarget, bool snapImmediately = true)
        {
            target = newTarget;
            initialized = false;
            positionVelocity = Vector3.zero;

            if (target != null && snapImmediately)
            {
                SnapToTarget();
                initialized = true;
            }
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            smoothedForward = GetPlanarForward(target.forward);
            Vector3 desiredPosition = CalculateDesiredPosition(smoothedForward);
            transform.position = desiredPosition;
            transform.rotation = CalculateDesiredRotation(desiredPosition, smoothedForward);
            positionVelocity = Vector3.zero;
        }

        private void UpdateHeading()
        {
            Vector3 desiredForward = GetPlanarForward(target.forward);
            float blend = 1f - Mathf.Exp(-headingDamping * Time.deltaTime);
            smoothedForward = Vector3.Slerp(smoothedForward, desiredForward, blend).normalized;

            if (smoothedForward.sqrMagnitude < 0.0001f)
            {
                smoothedForward = Vector3.forward;
            }
        }

        private void UpdatePosition()
        {
            Vector3 desiredPosition = CalculateDesiredPosition(smoothedForward);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime,
                maxPositionSpeed,
                Time.deltaTime);
        }

        private void UpdateRotation()
        {
            Quaternion desiredRotation = CalculateDesiredRotation(transform.position, smoothedForward);
            float blend = 1f - Mathf.Exp(-rotationDamping * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }

        private Vector3 CalculateDesiredPosition(Vector3 forward)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return target.position
                   - forward * followDistance
                   + Vector3.up * followHeight
                   + right * lateralOffset;
        }

        private Quaternion CalculateDesiredRotation(Vector3 cameraPosition, Vector3 forward)
        {
            Vector3 up = Vector3.Slerp(Vector3.up, target.up, targetUpInfluence).normalized;
            Vector3 lookPoint = target.position
                                + forward * lookAheadDistance
                                + Vector3.up * lookAtHeight;
            Vector3 direction = lookPoint - cameraPosition;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = forward;
            }

            return Quaternion.LookRotation(direction.normalized, up);
        }

        private static Vector3 GetPlanarForward(Vector3 sourceForward)
        {
            Vector3 planar = Vector3.ProjectOnPlane(sourceForward, Vector3.up);
            if (planar.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }

            return planar.normalized;
        }
    }
}
