using UnityEngine;
using UnityEngine.EventSystems;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private int activePointerId = int.MinValue;
        private RectTransform cachedRectTransform;

        public bool IsPressed { get; private set; }
        public RectTransform RectTransform => cachedRectTransform != null
            ? cachedRectTransform
            : (cachedRectTransform = transform as RectTransform);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || IsPressed)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            IsPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || !IsPressed || eventData.pointerId != activePointerId)
            {
                return;
            }

            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData != null && IsPressed && eventData.pointerId == activePointerId)
            {
                Release();
            }
        }

        private void OnDisable()
        {
            Release();
        }

        public void ForceRelease()
        {
            Release();
        }

        public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera = null)
        {
            RectTransform rectTransform = RectTransform;
            return rectTransform != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
        }

        private void Release()
        {
            IsPressed = false;
            activePointerId = int.MinValue;
        }
    }
}
