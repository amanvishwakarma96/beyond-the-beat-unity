using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class HudPanel : MonoBehaviour
    {
        [SerializeField, Range(0.15f, 0.25f)] private float transitionDuration = 0.2f;
        [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -12f);
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Coroutine transitionRoutine;
        private Vector2 shownPosition;
        private bool initialized;
        private bool visible;

        public bool IsVisible => visible;
        public float TransitionDuration => transitionDuration;
        public Image BackgroundImage => backgroundImage;
        public Image BorderImage => borderImage;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            transitionRoutine = null;
        }

        public void Show()
        {
            EnsureInitialized();
            if (visible && gameObject.activeSelf && canvasGroup.alpha >= 0.999f)
            {
                return;
            }

            visible = true;
            gameObject.SetActive(true);
            StartTransition(1f, shownPosition, deactivateWhenDone: false);
        }

        public void Hide()
        {
            EnsureInitialized();
            visible = false;
            if (!gameObject.activeSelf)
            {
                ApplyVisualState(0f, shownPosition + hiddenOffset);
                return;
            }

            StartTransition(0f, shownPosition + hiddenOffset, deactivateWhenDone: true);
        }

        public void SetImmediate(bool show)
        {
            EnsureInitialized();
            StopTransition();
            visible = show;

            if (show)
            {
                gameObject.SetActive(true);
                ApplyVisualState(1f, shownPosition);
            }
            else
            {
                ApplyVisualState(0f, shownPosition + hiddenOffset);
                gameObject.SetActive(false);
            }
        }

        public void ConfigurePresentation(Image background, Image border)
        {
            backgroundImage = background;
            borderImage = border;
        }

        private void StartTransition(float targetAlpha, Vector2 targetPosition, bool deactivateWhenDone)
        {
            StopTransition();
            if (!Application.isPlaying || !isActiveAndEnabled || transitionDuration <= 0f)
            {
                ApplyVisualState(targetAlpha, targetPosition);
                if (deactivateWhenDone)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            transitionRoutine = StartCoroutine(Animate(targetAlpha, targetPosition, deactivateWhenDone));
        }

        private IEnumerator Animate(float targetAlpha, Vector2 targetPosition, bool deactivateWhenDone)
        {
            float startAlpha = canvasGroup.alpha;
            Vector2 startPosition = rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = t * t * (3f - 2f * t);
                ApplyVisualState(
                    Mathf.Lerp(startAlpha, targetAlpha, eased),
                    Vector2.Lerp(startPosition, targetPosition, eased));
                yield return null;
            }

            ApplyVisualState(targetAlpha, targetPosition);
            transitionRoutine = null;
            if (deactivateWhenDone)
            {
                gameObject.SetActive(false);
            }
        }

        private void StopTransition()
        {
            if (transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            shownPosition = rectTransform.anchoredPosition;
            visible = gameObject.activeSelf;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            initialized = true;

            if (!visible)
            {
                ApplyVisualState(0f, shownPosition + hiddenOffset);
            }
        }

        private void ApplyVisualState(float alpha, Vector2 position)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
            rectTransform.anchoredPosition = position;
        }
    }
}
