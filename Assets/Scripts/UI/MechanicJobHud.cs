using BeyondTheBeat.Economy;
using BeyondTheBeat.Jobs;
using TMPro;
using UnityEngine;

namespace BeyondTheBeat.UI
{
    public sealed class MechanicJobHud : MonoBehaviour
    {
        [SerializeField] private MechanicJobManager jobManager;
        [SerializeField] private CreditWallet wallet;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private HudPanel panel;
        [SerializeField] private TMP_Text jobText;
        [SerializeField] private TMP_Text creditsText;

        public MechanicJobManager JobManager => jobManager;
        public CreditWallet Wallet => wallet;
        public GameObject PanelRoot => panelRoot;
        public TMP_Text JobText => jobText;
        public TMP_Text CreditsText => creditsText;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetSources(MechanicJobManager manager, CreditWallet creditWallet)
        {
            Unsubscribe();
            jobManager = manager;
            wallet = creditWallet;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            Refresh();
        }

        public void Refresh()
        {
            if (panelRoot == null || jobText == null || creditsText == null)
            {
                return;
            }

            bool visible = jobManager != null && wallet != null;
            SetPanelVisible(visible);
            if (!visible)
            {
                return;
            }

            MechanicJobDefinition definition = jobManager.CurrentJob ?? jobManager.StartingJob;
            switch (jobManager.State)
            {
                case MechanicJobState.Active:
                    jobText.text = definition != null
                        ? $"MECHANIC JOB  •  {definition.DisplayName}  •  +{definition.RewardCredits} CR"
                        : "MECHANIC JOB  •  ACTIVE";
                    break;
                case MechanicJobState.Completed:
                    jobText.text = definition != null
                        ? $"JOB COMPLETE  •  +{definition.RewardCredits} CR"
                        : "JOB COMPLETE";
                    break;
                default:
                    jobText.text = definition != null
                        ? $"MECHANIC JOB  •  {definition.DisplayName}"
                        : "MECHANIC JOB  •  READY";
                    break;
            }

            creditsText.text = $"CREDITS  {wallet.Balance}";
        }

        private void Subscribe()
        {
            if (jobManager != null)
            {
                jobManager.StateChanged -= HandleJobStateChanged;
                jobManager.StateChanged += HandleJobStateChanged;
                jobManager.JobCompleted -= HandleJobCompleted;
                jobManager.JobCompleted += HandleJobCompleted;
            }

            if (wallet != null)
            {
                wallet.BalanceChanged -= HandleBalanceChanged;
                wallet.BalanceChanged += HandleBalanceChanged;
            }
        }

        private void Unsubscribe()
        {
            if (jobManager != null)
            {
                jobManager.StateChanged -= HandleJobStateChanged;
                jobManager.JobCompleted -= HandleJobCompleted;
            }

            if (wallet != null)
            {
                wallet.BalanceChanged -= HandleBalanceChanged;
            }
        }

        private void HandleJobStateChanged(MechanicJobManager manager, MechanicJobState state)
        {
            Refresh();
        }

        private void HandleJobCompleted(MechanicJobManager manager, MechanicJobDefinition definition, int reward)
        {
            Refresh();
        }

        private void HandleBalanceChanged(CreditWallet source, int previous, int current)
        {
            Refresh();
        }

        private void SetPanelVisible(bool visible)
        {
            if (panel == null && panelRoot != null)
            {
                panel = panelRoot.GetComponent<HudPanel>();
            }

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
            }
            else
            {
                panelRoot.SetActive(visible);
            }
        }
    }
}
