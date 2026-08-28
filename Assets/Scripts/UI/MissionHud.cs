using BeyondTheBeat.Missions;
using UnityEngine;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    [DisallowMultipleComponent]
    public sealed class MissionHud : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private Image progressFill;

        public MissionManager MissionManager => missionManager;
        public GameObject PanelRoot => panelRoot;
        public Text TitleText => titleText;
        public Text ObjectiveText => objectiveText;
        public Text StatusText => statusText;
        public GameObject ProgressRoot => progressRoot;
        public Image ProgressFill => progressFill;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetSource(MissionManager manager)
        {
            Unsubscribe();
            missionManager = manager;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            Refresh();
        }

        public void Refresh()
        {
            MissionDefinition mission = missionManager != null ? missionManager.CurrentMission : null;
            MissionState state = missionManager != null ? missionManager.State : MissionState.Inactive;
            MissionProgressSnapshot progress = missionManager != null ? missionManager.Progress : default;
            MissionHudSnapshot snapshot = CreateSnapshot(mission, state, progress);

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = snapshot.Title;
            }

            if (objectiveText != null)
            {
                objectiveText.text = snapshot.Objective;
            }

            if (statusText != null)
            {
                statusText.text = AppendSurvivalResourceStatus(snapshot.Status, mission, state);
            }

            bool showProgress = mission != null &&
                                state == MissionState.Active &&
                                mission.ObjectiveType == MissionObjectiveType.ReachAndSurvive &&
                                progress.TargetContextActive;

            if (progressRoot != null)
            {
                progressRoot.SetActive(showProgress);
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = showProgress ? progress.NormalizedProgress : 0f;
            }
        }

        public static MissionHudSnapshot CreateSnapshot(MissionDefinition mission, MissionState state)
        {
            MissionProgressSnapshot progress = mission != null
                ? new MissionProgressSnapshot(
                    mission.ObjectiveType,
                    false,
                    0f,
                    mission.SurvivalDurationSeconds)
                : default;
            return CreateSnapshot(mission, state, progress);
        }

        public static MissionHudSnapshot CreateSnapshot(
            MissionDefinition mission,
            MissionState state,
            MissionProgressSnapshot progress)
        {
            if (mission == null || state == MissionState.Inactive)
            {
                return new MissionHudSnapshot(
                    "FREE ROAM",
                    "Explore, drive and discover the world.",
                    "NO ACTIVE MISSION");
            }

            switch (state)
            {
                case MissionState.Active:
                    if (mission.ObjectiveType == MissionObjectiveType.ReachAndSurvive)
                    {
                        return CreateReachAndSurviveSnapshot(mission, progress);
                    }

                    return new MissionHudSnapshot(
                        mission.DisplayName,
                        string.IsNullOrWhiteSpace(mission.Description)
                            ? "Reach the marked objective."
                            : mission.Description,
                        "MISSION ACTIVE");

                case MissionState.Completed:
                    return new MissionHudSnapshot(
                        "MISSION COMPLETE",
                        mission.DisplayName + "\nFree roam is available.",
                        "COMPLETE • KEEP EXPLORING");

                case MissionState.Failed:
                    return new MissionHudSnapshot(
                        "MISSION FAILED",
                        mission.DisplayName + "\nFree roam remains available.",
                        "FAILED • FREE ROAM");

                default:
                    return new MissionHudSnapshot(
                        "FREE ROAM",
                        "Explore, drive and discover the world.",
                        "NO ACTIVE MISSION");
            }
        }

        private string AppendSurvivalResourceStatus(string baseStatus, MissionDefinition mission, MissionState state)
        {
            if (mission == null ||
                state != MissionState.Active ||
                mission.ObjectiveType != MissionObjectiveType.ReachAndSurvive ||
                missionManager == null ||
                missionManager.SurvivalController == null ||
                missionManager.SurvivalController.Resource == null)
            {
                return baseStatus;
            }

            int resourcePercent = Mathf.RoundToInt(
                missionManager.SurvivalController.Resource.NormalizedValue * 100f);
            return $"{baseStatus} • RESOURCE {resourcePercent}%";
        }

        private static MissionHudSnapshot CreateReachAndSurviveSnapshot(
            MissionDefinition mission,
            MissionProgressSnapshot progress)
        {
            if (!progress.TargetContextActive)
            {
                return new MissionHudSnapshot(
                    mission.DisplayName,
                    "Reach the forest survival zone.",
                    "REACH TARGET ZONE");
            }

            int elapsed = Mathf.FloorToInt(progress.SurvivalElapsedSeconds);
            int required = Mathf.CeilToInt(progress.SurvivalRequiredSeconds);
            int percent = Mathf.RoundToInt(progress.NormalizedProgress * 100f);

            return new MissionHudSnapshot(
                mission.DisplayName,
                $"Hold out in the forest • {elapsed}/{required}s",
                $"SURVIVING • {percent}%");
        }

        private void Subscribe()
        {
            if (missionManager == null)
            {
                return;
            }

            missionManager.MissionStateChanged -= HandleMissionStateChanged;
            missionManager.MissionStateChanged += HandleMissionStateChanged;
            missionManager.MissionProgressChanged -= HandleMissionProgressChanged;
            missionManager.MissionProgressChanged += HandleMissionProgressChanged;

            if (missionManager.SurvivalController != null && missionManager.SurvivalController.Resource != null)
            {
                missionManager.SurvivalController.Resource.ValueChanged -= HandleSurvivalValueChanged;
                missionManager.SurvivalController.Resource.ValueChanged += HandleSurvivalValueChanged;
            }
        }

        private void Unsubscribe()
        {
            if (missionManager == null)
            {
                return;
            }

            missionManager.MissionStateChanged -= HandleMissionStateChanged;
            missionManager.MissionProgressChanged -= HandleMissionProgressChanged;

            if (missionManager.SurvivalController != null && missionManager.SurvivalController.Resource != null)
            {
                missionManager.SurvivalController.Resource.ValueChanged -= HandleSurvivalValueChanged;
            }
        }

        private void HandleMissionStateChanged(MissionDefinition mission, MissionState state)
        {
            Refresh();
        }

        private void HandleMissionProgressChanged(MissionProgressSnapshot progress)
        {
            Refresh();
        }

        private void HandleSurvivalValueChanged(float currentValue, float maxValue)
        {
            Refresh();
        }
    }

    public readonly struct MissionHudSnapshot
    {
        public MissionHudSnapshot(string title, string objective, string status)
        {
            Title = title;
            Objective = objective;
            Status = status;
        }

        public string Title { get; }
        public string Objective { get; }
        public string Status { get; }
    }
}
