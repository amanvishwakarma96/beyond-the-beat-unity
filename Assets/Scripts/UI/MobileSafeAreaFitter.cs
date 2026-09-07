using UnityEngine;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileSafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 authoredAnchorMin;
        [SerializeField] private Vector2 authoredAnchorMax = Vector2.one;
        [SerializeField] private bool hasAuthoredLayout;

        private bool applying;

        public RectTransform Target => target;
        public Vector2 AuthoredAnchorMin => authoredAnchorMin;
        public Vector2 AuthoredAnchorMax => authoredAnchorMax;
        public bool HasAuthoredLayout => hasAuthoredLayout;

        private void OnEnable()
        {
            EnsureTarget();
            if (!hasAuthoredLayout)
            {
                CaptureAuthoredLayout();
            }
            ApplySafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && !applying)
            {
                ApplySafeArea();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isActiveAndEnabled)
            {
                ApplySafeArea();
            }
        }

        public void ConfigureFromCurrentRect(RectTransform rectTransform)
        {
            target = rectTransform;
            CaptureAuthoredLayout();
        }

        public void RestoreAuthoredLayout()
        {
            EnsureTarget();
            if (target == null || !hasAuthoredLayout)
            {
                return;
            }

            applying = true;
            target.anchorMin = authoredAnchorMin;
            target.anchorMax = authoredAnchorMax;
            applying = false;
        }

        public void ApplySafeArea()
        {
            EnsureTarget();
            if (target == null || !hasAuthoredLayout || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            CalculateMappedAnchors(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Screen.safeArea,
                authoredAnchorMin,
                authoredAnchorMax,
                out Vector2 mappedMin,
                out Vector2 mappedMax);

            if (Approximately(target.anchorMin, mappedMin) && Approximately(target.anchorMax, mappedMax))
            {
                return;
            }

            applying = true;
            target.anchorMin = mappedMin;
            target.anchorMax = mappedMax;
            applying = false;
        }

        public static void CalculateMappedAnchors(
            Rect screenRect,
            Rect safeArea,
            Vector2 authoredMin,
            Vector2 authoredMax,
            out Vector2 mappedMin,
            out Vector2 mappedMax)
        {
            Vector2 min = new Vector2(
                Mathf.Clamp01(Mathf.Min(authoredMin.x, authoredMax.x)),
                Mathf.Clamp01(Mathf.Min(authoredMin.y, authoredMax.y)));
            Vector2 max = new Vector2(
                Mathf.Clamp01(Mathf.Max(authoredMin.x, authoredMax.x)),
                Mathf.Clamp01(Mathf.Max(authoredMin.y, authoredMax.y)));

            if (screenRect.width <= 0f || screenRect.height <= 0f)
            {
                mappedMin = min;
                mappedMax = max;
                return;
            }

            float safeXMin = Mathf.Clamp(safeArea.xMin, screenRect.xMin, screenRect.xMax);
            float safeYMin = Mathf.Clamp(safeArea.yMin, screenRect.yMin, screenRect.yMax);
            float safeXMax = Mathf.Clamp(safeArea.xMax, screenRect.xMin, screenRect.xMax);
            float safeYMax = Mathf.Clamp(safeArea.yMax, screenRect.yMin, screenRect.yMax);

            if (safeXMax <= safeXMin || safeYMax <= safeYMin)
            {
                safeXMin = screenRect.xMin;
                safeYMin = screenRect.yMin;
                safeXMax = screenRect.xMax;
                safeYMax = screenRect.yMax;
            }

            Vector2 normalizedSafeMin = new Vector2(
                (safeXMin - screenRect.xMin) / screenRect.width,
                (safeYMin - screenRect.yMin) / screenRect.height);
            Vector2 normalizedSafeMax = new Vector2(
                (safeXMax - screenRect.xMin) / screenRect.width,
                (safeYMax - screenRect.yMin) / screenRect.height);

            mappedMin = new Vector2(
                Mathf.Lerp(normalizedSafeMin.x, normalizedSafeMax.x, min.x),
                Mathf.Lerp(normalizedSafeMin.y, normalizedSafeMax.y, min.y));
            mappedMax = new Vector2(
                Mathf.Lerp(normalizedSafeMin.x, normalizedSafeMax.x, max.x),
                Mathf.Lerp(normalizedSafeMin.y, normalizedSafeMax.y, max.y));
        }

        private void CaptureAuthoredLayout()
        {
            EnsureTarget();
            if (target == null)
            {
                return;
            }

            authoredAnchorMin = target.anchorMin;
            authoredAnchorMax = target.anchorMax;
            hasAuthoredLayout = true;
        }

        private void EnsureTarget()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.0001f &&
                   Mathf.Abs(a.y - b.y) <= 0.0001f;
        }
    }
}
