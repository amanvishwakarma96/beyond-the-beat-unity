using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Visual feedback")]
        [SerializeField] private Graphic visualTarget;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.white;

        private int activePointerId = int.MinValue;
        private RectTransform cachedRectTransform;

        public bool IsPressed { get; private set; }
        public RectTransform RectTransform => cachedRectTransform != null
            ? cachedRectTransform
            : (cachedRectTransform = transform as RectTransform);

        private void Awake()
        {
            SetVisualPressed(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || IsPressed)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            IsPressed = true;
            SetVisualPressed(true);
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

        public void ConfigureVisual(Graphic target, Color normal, Color pressed)
        {
            visualTarget = target;
            normalColor = normal;
            pressedColor = pressed;
            SetVisualPressed(false);
        }

        public void SetVisualPressed(bool pressed)
        {
            if (visualTarget != null)
            {
                visualTarget.color = pressed ? pressedColor : normalColor;
            }
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
            SetVisualPressed(false);
        }
    }
}
