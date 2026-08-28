using System;
using System.Collections;
using UnityEngine;

namespace BeyondTheBeat.Puzzles
{
    [DisallowMultipleComponent]
    public sealed class RestrictedGateController : MonoBehaviour
    {
        [SerializeField] private Transform gateTransform;
        [SerializeField] private Vector3 closedLocalPosition;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 5f, 0f);
        [SerializeField, Min(0f)] private float transitionDuration = 0.35f;
        [SerializeField] private bool startLocked = true;
        [SerializeField] private bool openWhenUnlocked = true;

        private Coroutine transitionRoutine;
        private bool initialized;
        private bool isLocked;
        private bool isOpen;

        public Transform GateTransform => gateTransform;
        public Vector3 ClosedLocalPosition => closedLocalPosition;
        public Vector3 OpenLocalPosition => closedLocalPosition + openLocalOffset;
        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;
        public bool StartLocked => startLocked;

        public event Action<bool> LockStateChanged;
        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
        }

        public bool SetLocked(bool locked)
        {
            InitializeIfNeeded();
            bool changed = isLocked != locked;
            isLocked = locked;
            ApplyTargetPose();

            if (changed)
            {
                LockStateChanged?.Invoke(isLocked);
            }

            return changed;
        }

        public void SnapToCurrentState()
        {
            InitializeIfNeeded();
            ApplyPoseImmediate(!isLocked && openWhenUnlocked);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            if (gateTransform == null)
            {
                gateTransform = transform;
            }

            isLocked = startLocked;
            initialized = true;
            ApplyPoseImmediate(!isLocked && openWhenUnlocked);
        }

        private void ApplyTargetPose()
        {
            bool shouldOpen = !isLocked && openWhenUnlocked;
            if (!Application.isPlaying || transitionDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyPoseImmediate(shouldOpen);
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(AnimateTo(shouldOpen));
        }

        private IEnumerator AnimateTo(bool shouldOpen)
        {
            Vector3 start = gateTransform.localPosition;
            Vector3 target = shouldOpen ? OpenLocalPosition : closedLocalPosition;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionDuration > 0f ? Mathf.Clamp01(elapsed / transitionDuration) : 1f;
                gateTransform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            gateTransform.localPosition = target;
            transitionRoutine = null;
            SetOpenState(shouldOpen);
        }

        private void ApplyPoseImmediate(bool open)
        {
            if (gateTransform == null)
            {
                return;
            }

            gateTransform.localPosition = open ? OpenLocalPosition : closedLocalPosition;
            SetOpenState(open);
        }

        private void SetOpenState(bool open)
        {
            if (isOpen == open)
            {
                return;
            }

            isOpen = open;
            OpenStateChanged?.Invoke(isOpen);
        }
    }
}
