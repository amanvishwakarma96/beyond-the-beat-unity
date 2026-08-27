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

        public MissionManager MissionManager => missionManager;
        public GameObject PanelRoot => panelRoot;
        public Text TitleText => titleText;
        public Text ObjectiveText => objectiveText;
        public Text StatusText => statusText;

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
            MissionHudSnapshot snapshot = CreateSnapshot(
                missionManager != null ? missionManager.CurrentMission : null,
                missionManager != null ? missionManager.State : MissionState.Inactive,
                missionManager != null ? missionManager.Progress : default);

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
                statusText.text = snapshot.Status;
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
                    "Explore the world, drive, park, or resume a mission.",
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
                        mission.DisplayName + "\nContinue driving and exploring in free roam.",
                        "COMPLETE • FREE ROAM AVAILABLE");

                case MissionState.Failed:
                    return new MissionHudSnapshot(
                        "MISSION FAILED",
                        mission.DisplayName + "\nFree roam remains available.",
                        "FAILED • FREE ROAM AVAILABLE");

                default:
                    return new MissionHudSnapshot(
                        "FREE ROAM",
                        "Explore the world, drive, park, or resume a mission.",
                        "NO ACTIVE MISSION");
            }
        }

        private static MissionHudSnapshot CreateReachAndSurviveSnapshot(
            MissionDefinition mission,
            MissionProgressSnapshot progress)
        {
            if (!progress.TargetContextActive)
            {
                return new MissionHudSnapshot(
                    mission.DisplayName,
                    string.IsNullOrWhiteSpace(mission.Description)
                        ? "Reach the target survival zone."
                        : mission.Description,
                    "REACH TARGET ZONE");
            }

            int elapsed = Mathf.FloorToInt(progress.SurvivalElapsedSeconds);
            int required = Mathf.CeilToInt(progress.SurvivalRequiredSeconds);
            int percent = Mathf.RoundToInt(progress.NormalizedProgress * 100f);

            return new MissionHudSnapshot(
                mission.DisplayName,
                $"Survive the environmental pressure: {elapsed}/{required}s",
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
        }

        private void Unsubscribe()
        {
            if (missionManager != null)
            {
                missionManager.MissionStateChanged -= HandleMissionStateChanged;
                missionManager.MissionProgressChanged -= HandleMissionProgressChanged;
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
