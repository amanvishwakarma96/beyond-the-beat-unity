using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeyondTheBeat.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialHud : MonoBehaviour
    {
        [SerializeField] private TutorialController controller;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Button skipButton;

        public TutorialController Controller => controller;
        public GameObject Panel => panel;
        public TMP_Text TitleText => titleText;
        public TMP_Text InstructionText => instructionText;
        public TMP_Text ProgressText => progressText;
        public Button SkipButton => skipButton;

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Changed += Refresh;
            }
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(HandleSkip);
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Changed -= Refresh;
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkip);
            }
        }

        public void Configure(
            TutorialController tutorialController,
            GameObject panelObject,
            TMP_Text title,
            TMP_Text instruction,
            TMP_Text progress,
            Button skip)
        {
            if (isActiveAndEnabled && controller != null)
            {
                controller.Changed -= Refresh;
            }
            if (isActiveAndEnabled && skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkip);
            }

            controller = tutorialController;
            panel = panelObject;
            titleText = title;
            instructionText = instruction;
            progressText = progress;
            skipButton = skip;

            if (isActiveAndEnabled && controller != null)
            {
                controller.Changed += Refresh;
            }
            if (isActiveAndEnabled && skipButton != null)
            {
                skipButton.onClick.AddListener(HandleSkip);
            }
            Refresh();
        }

        public void Refresh()
        {
            bool visible = controller != null && controller.IsActive && controller.CurrentStep != null;
            if (panel != null)
            {
                panel.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            TutorialStep step = controller.CurrentStep;
            if (titleText != null)
            {
                titleText.text = step.Title;
            }
            if (instructionText != null)
            {
                instructionText.text = step.Instruction;
            }
            if (progressText != null)
            {
                progressText.text = $"{controller.CurrentStepIndex + 1}/{controller.StepCount}";
            }
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(controller.Profile != null && controller.Profile.AllowSkip);
            }
        }

        private void HandleSkip()
        {
            controller?.Skip();
        }
    }
}
