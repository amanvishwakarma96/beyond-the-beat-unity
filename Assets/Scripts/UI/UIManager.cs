using BeyondTheBeat.Interaction;
using TMPro;
using UnityEngine;

namespace BeyondTheBeat.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private InteractionController interactionController;
        [SerializeField] private ParkingZone parkingZone;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private HudPanel promptPanel;
        [SerializeField] private string promptPrefix = "ACTION / E  •  ";

        [Header("Feedback")]
        [SerializeField] private GameObject feedbackRoot;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private HudPanel feedbackPanel;
        [SerializeField, Min(0.25f)] private float feedbackDuration = 2f;

        private float feedbackRemaining;

        private void OnEnable()
        {
            Subscribe();
            RefreshPrompt();
            HideFeedback();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (feedbackRemaining <= 0f)
            {
                return;
            }

            feedbackRemaining -= Time.unscaledDeltaTime;
            if (feedbackRemaining <= 0f)
            {
                HideFeedback();
            }
        }

        public void SetSources(InteractionController controller, ParkingZone zone)
        {
            Unsubscribe();
            interactionController = controller;
            parkingZone = zone;

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshPrompt();
            }
        }

        public void ShowFeedback(string message)
        {
            if (feedbackRoot == null || feedbackText == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            feedbackText.text = message;
            SetVisible(feedbackPanel, feedbackRoot, true);
            feedbackRemaining = feedbackDuration;
        }

        public void HideFeedback()
        {
            feedbackRemaining = 0f;
            SetVisible(feedbackPanel, feedbackRoot, false);
        }

        private void Subscribe()
        {
            if (interactionController != null)
            {
                interactionController.PromptChanged -= HandlePromptChanged;
                interactionController.PromptChanged += HandlePromptChanged;
            }

            if (parkingZone != null)
            {
                parkingZone.ParkingCompleted -= HandleParkingCompleted;
                parkingZone.ParkingCompleted += HandleParkingCompleted;
            }
        }

        private void Unsubscribe()
        {
            if (interactionController != null)
            {
                interactionController.PromptChanged -= HandlePromptChanged;
            }

            if (parkingZone != null)
            {
                parkingZone.ParkingCompleted -= HandleParkingCompleted;
            }
        }

        private void RefreshPrompt()
        {
            InteractableObject active = interactionController != null
                ? interactionController.ActiveInteractable
                : null;

            HandlePromptChanged(active != null, active != null ? active.PromptText : string.Empty);
        }

        private void HandlePromptChanged(bool visible, string message)
        {
            if (promptRoot == null || promptText == null)
            {
                return;
            }

            bool show = visible && !string.IsNullOrWhiteSpace(message);
            if (show)
            {
                promptText.text = promptPrefix + message;
            }
            SetVisible(promptPanel, promptRoot, show);
        }

        private void HandleParkingCompleted(ParkingZone zone, GameObject actor)
        {
            if (zone != null)
            {
                ShowFeedback(zone.SuccessMessage);
            }
        }

        private static void SetVisible(HudPanel panel, GameObject fallbackRoot, bool visible)
        {
            if (panel != null)
            {
                if (visible)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
                return;
            }

            if (fallbackRoot != null)
            {
                fallbackRoot.SetActive(visible);
            }
        }
    }
}
