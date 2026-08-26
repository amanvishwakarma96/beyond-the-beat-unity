using System;
using System.Text;
using BeyondTheBeat.Vehicle;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace BeyondTheBeat.UI
{
    /// <summary>
    /// Lightweight runtime telemetry used only by Phase 0 development builds.
    /// It gives device testers enough objective data to complete the Android
    /// validation report without adding future-phase gameplay systems.
    /// </summary>
    public sealed class Phase0ValidationOverlay : MonoBehaviour
    {
        private const string OverlayName = "Phase0ValidationOverlay";

        [Header("Sampling")]
        [SerializeField, Min(0.25f)] private float sampleInterval = 1f;
        [SerializeField, Min(0.25f)] private float displayRefreshInterval = 0.5f;
        [SerializeField, Min(1f)] private float logInterval = 15f;
        [SerializeField, Min(16f)] private float stutterThresholdMs = 50f;

        private readonly StringBuilder builder = new StringBuilder(512);

        private Text displayText;
        private VehicleController vehicleController;
        private int sampleFrameCount;
        private float sampleElapsed;
        private float displayElapsed;
        private float logElapsed;
        private float sessionElapsed;
        private float currentFps;
        private float minFps = float.PositiveInfinity;
        private float maxFps;
        private float worstFrameMs;
        private int stutterFrameCount;
        private int initialGcGen0;
        private int initialGcGen1;
        private int initialGcGen2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForDevelopmentBuild()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                return;
            }

            if (GameObject.Find(OverlayName) != null)
            {
                return;
            }

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[Beyond The Beat] Phase 0 validation overlay skipped because no Canvas was found.");
                return;
            }

            GameObject root = new GameObject(
                OverlayName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Phase0ValidationOverlay));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(18f, -18f);
            rootRect.sizeDelta = new Vector2(680f, 214f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.02f, 0.03f, 0.05f, 0.78f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(root.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 12f);
            textRect.offsetMax = new Vector2(-16f, -12f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.raycastTarget = false;

            Phase0ValidationOverlay overlay = root.GetComponent<Phase0ValidationOverlay>();
            overlay.displayText = text;
            overlay.vehicleController = UnityEngine.Object.FindFirstObjectByType<VehicleController>();

            root.transform.SetAsLastSibling();
        }

        private void Awake()
        {
            initialGcGen0 = GC.CollectionCount(0);
            initialGcGen1 = GC.CollectionCount(1);
            initialGcGen2 = GC.CollectionCount(2);

            LogDeviceSummary();
            RefreshDisplay();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            sessionElapsed += deltaTime;
            sampleElapsed += deltaTime;
            displayElapsed += deltaTime;
            logElapsed += deltaTime;
            sampleFrameCount++;

            float frameMs = deltaTime * 1000f;
            if (frameMs > worstFrameMs)
            {
                worstFrameMs = frameMs;
            }

            if (frameMs >= stutterThresholdMs)
            {
                stutterFrameCount++;
            }

            if (sampleElapsed >= sampleInterval)
            {
                currentFps = sampleFrameCount / sampleElapsed;
                minFps = Mathf.Min(minFps, currentFps);
                maxFps = Mathf.Max(maxFps, currentFps);

                sampleElapsed = 0f;
                sampleFrameCount = 0;
            }

            if (displayElapsed >= displayRefreshInterval)
            {
                displayElapsed = 0f;
                RefreshDisplay();
            }

            if (logElapsed >= logInterval)
            {
                logElapsed = 0f;
                Debug.Log("[Beyond The Beat] Phase 0 validation metrics: " + BuildMetricsText(singleLine: true));
            }
        }

        private void OnApplicationPause(bool paused)
        {
            Debug.Log($"[Beyond The Beat] Phase 0 validation app pause state changed. paused={paused}.");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"[Beyond The Beat] Phase 0 validation app focus changed. hasFocus={hasFocus}.");
        }

        private void RefreshDisplay()
        {
            if (displayText == null)
            {
                return;
            }

            displayText.text = BuildMetricsText(singleLine: false);
        }

        private string BuildMetricsText(bool singleLine)
        {
            string separator = singleLine ? " | " : "\n";
            float safeMinFps = float.IsPositiveInfinity(minFps) ? 0f : minFps;
            float speedKph = vehicleController != null ? vehicleController.CurrentSpeedKph : 0f;

            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            long reservedBytes = Profiler.GetTotalReservedMemoryLong();

            int gc0 = GC.CollectionCount(0) - initialGcGen0;
            int gc1 = GC.CollectionCount(1) - initialGcGen1;
            int gc2 = GC.CollectionCount(2) - initialGcGen2;

            builder.Clear();
            builder.Append("PHASE 0 DEVICE VALIDATION");
            builder.Append(separator);
            builder.Append("FPS now/min/max: ");
            builder.Append(currentFps.ToString("0"));
            builder.Append('/');
            builder.Append(safeMinFps.ToString("0"));
            builder.Append('/');
            builder.Append(maxFps.ToString("0"));
            builder.Append(separator);
            builder.Append("Worst frame: ");
            builder.Append(worstFrameMs.ToString("0.0"));
            builder.Append(" ms  Stutter frames >= ");
            builder.Append(stutterThresholdMs.ToString("0"));
            builder.Append(" ms: ");
            builder.Append(stutterFrameCount);
            builder.Append(separator);
            builder.Append("GC collections G0/G1/G2: ");
            builder.Append(gc0);
            builder.Append('/');
            builder.Append(gc1);
            builder.Append('/');
            builder.Append(gc2);
            builder.Append(separator);
            builder.Append("Memory allocated/reserved: ");
            builder.Append(BytesToMegabytes(allocatedBytes).ToString("0.0"));
            builder.Append('/');
            builder.Append(BytesToMegabytes(reservedBytes).ToString("0.0"));
            builder.Append(" MB");
            builder.Append(separator);
            builder.Append("Vehicle speed: ");
            builder.Append(speedKph.ToString("0.0"));
            builder.Append(" km/h  Session: ");
            builder.Append(sessionElapsed.ToString("0"));
            builder.Append(" s");

            return builder.ToString();
        }

        private static float BytesToMegabytes(long bytes)
        {
            return bytes / (1024f * 1024f);
        }

        private static void LogDeviceSummary()
        {
            Resolution resolution = Screen.currentResolution;

            Debug.Log(
                "[Beyond The Beat] Phase 0 device validation START. " +
                $"Device='{SystemInfo.deviceModel}', OS='{SystemInfo.operatingSystem}', " +
                $"CPU='{SystemInfo.processorType}', RAM={SystemInfo.systemMemorySize} MB, " +
                $"GPU='{SystemInfo.graphicsDeviceName}', GPU memory={SystemInfo.graphicsMemorySize} MB, " +
                $"Resolution={resolution.width}x{resolution.height}, Unity={Application.unityVersion}, " +
                $"DebugBuild={Debug.isDebugBuild}.");
        }
    }
}
