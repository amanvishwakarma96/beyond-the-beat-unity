using UnityEngine;
using UnityEngine.EventSystems;

namespace BeyondTheBeat.UI
{
    public sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private int activePointerId = int.MinValue;

        public bool IsPressed { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPressed)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            IsPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsPressed || eventData.pointerId != activePointerId)
            {
                return;
            }

            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        public void ForceRelease()
        {
            Release();
        }

        private void Release()
        {
            IsPressed = false;
            activePointerId = int.MinValue;
        }
    }
}
