using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    [Serializable]
    internal sealed class DeviceMatrixConfig
    {
        public int schemaVersion;
        public DeviceMatrixLane[] lanes;
        public DeviceMatrixScenario[] scenarios;
        public DeviceMatrixReleaseGate releaseGate;
    }

    [Serializable]
    internal sealed class DeviceMatrixLane
    {
        public string id;
        public string displayName;
        public int apiLevel;
        public string androidVersion;
        public int ramClassGb;
        public string screenClass;
        public string cutoutCoverage;
        public bool physicalRequired;
        public string purpose;
    }

    [Serializable]
    internal sealed class DeviceMatrixScenario
    {
        public string id;
        public string name;
        public string passCriteria;
    }

    [Serializable]
    internal sealed class DeviceMatrixReleaseGate
    {
        public int minimumPhysicalDevices;
        public int minimumManufacturers;
        public bool requirePhysicalHardware;
        public bool emulatorCountsForSignOff;
        public int requiredTargetApiLevel;
        public int baselineFps;
        public int stretchFps;
        public int minimumSoakMinutes;
    }

    internal static class Phase6DeviceMatrixBuilder
    {
        internal const string ConfigPath = "Docs/Validation/PHASE_6_DEVICE_MATRIX.json";
        internal const string ValidationDocPath = "Docs/Validation/PHASE_6_DEVICE_MATRIX.md";
        internal const string OutputDirectoryRelativePath = "build/device-matrix";
        internal const string TestPlanRelativePath = OutputDirectoryRelativePath + "/DEVICE-MATRIX-TEST-PLAN.md";
        internal const string EvidenceRelativePath = OutputDirectoryRelativePath + "/DEVICE-MATRIX-EVIDENCE.csv";
        internal const string StatusRelativePath = OutputDirectoryRelativePath + "/DEVICE-MATRIX-STATUS.txt";
        internal const int RequiredTargetApiLevel = 36;
        internal const int RequiredLaneCount = 3;
        internal const int RequiredScenarioCount = 12;

        [MenuItem("Beyond The Beat/Phase 6/Prepare Device Matrix + API 36")]
        public static void PrepareAndValidateOrThrow()
        {
            ConfigureAndroid16Target();
            DeviceMatrixConfig config = LoadConfigOrThrow();
            ValidateConfigOrThrow(config);
            ValidateTargetSdkOrThrow();
            WriteArtifacts(config);
            ValidateGeneratedArtifactsOrThrow(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Beyond The Beat] Phase 6 device matrix PASS: Android target SDK 36 configured; " +
                $"{config.lanes.Length} physical lanes x {config.scenarios.Length} scenarios prepared with PENDING evidence only.");
        }

        internal static DeviceMatrixConfig LoadConfigOrThrow()
        {
            string path = ResolveProjectPath(ConfigPath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Phase 6 device-matrix configuration is missing.", path);
            }

            string json = File.ReadAllText(path);
            DeviceMatrixConfig config = JsonUtility.FromJson<DeviceMatrixConfig>(json);
            if (config == null)
            {
                throw new InvalidOperationException("Unable to deserialize the Phase 6 device-matrix configuration.");
            }

            return config;
        }

        internal static void ValidateConfigOrThrow(DeviceMatrixConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.schemaVersion != 1)
            {
                throw new InvalidOperationException($"Unsupported device-matrix schema version {config.schemaVersion}.");
            }

            if (config.lanes == null || config.lanes.Length < RequiredLaneCount)
            {
                throw new InvalidOperationException($"Device matrix requires at least {RequiredLaneCount} lanes.");
            }

            if (config.scenarios == null || config.scenarios.Length < RequiredScenarioCount)
            {
                throw new InvalidOperationException($"Device matrix requires at least {RequiredScenarioCount} scenarios.");
            }

            HashSet<string> laneIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<int> apiLevels = new HashSet<int>();
            foreach (DeviceMatrixLane lane in config.lanes)
            {
                if (lane == null || string.IsNullOrWhiteSpace(lane.id) || string.IsNullOrWhiteSpace(lane.displayName) ||
                    string.IsNullOrWhiteSpace(lane.androidVersion) || string.IsNullOrWhiteSpace(lane.screenClass) ||
                    string.IsNullOrWhiteSpace(lane.purpose) || lane.apiLevel <= 0 || lane.ramClassGb <= 0 || !lane.physicalRequired)
                {
                    throw new InvalidOperationException("Every device-matrix lane must be a configured physical-hardware lane.");
                }

                if (!laneIds.Add(lane.id))
                {
                    throw new InvalidOperationException($"Duplicate device-matrix lane id '{lane.id}'.");
                }

                apiLevels.Add(lane.apiLevel);
            }

            int[] requiredApis = { 30, 33, RequiredTargetApiLevel };
            if (requiredApis.Any(required => !apiLevels.Contains(required)))
            {
                throw new InvalidOperationException(
                    $"Device matrix must cover API {string.Join(", ", requiredApis)}. Found: {string.Join(", ", apiLevels.OrderBy(value => value))}.");
            }

            HashSet<string> scenarioIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DeviceMatrixScenario scenario in config.scenarios)
            {
                if (scenario == null || string.IsNullOrWhiteSpace(scenario.id) || string.IsNullOrWhiteSpace(scenario.name) ||
                    string.IsNullOrWhiteSpace(scenario.passCriteria))
                {
                    throw new InvalidOperationException("Every device-matrix scenario must have an id, name and pass criteria.");
                }

                if (!scenarioIds.Add(scenario.id))
                {
                    throw new InvalidOperationException($"Duplicate device-matrix scenario id '{scenario.id}'.");
                }
            }

            DeviceMatrixReleaseGate gate = config.releaseGate ??
                                           throw new InvalidOperationException("Device matrix releaseGate is missing.");
            if (gate.minimumPhysicalDevices < 3 || gate.minimumManufacturers < 2 || !gate.requirePhysicalHardware ||
                gate.emulatorCountsForSignOff || gate.requiredTargetApiLevel != RequiredTargetApiLevel ||
                gate.baselineFps < 30 || gate.stretchFps < gate.baselineFps || gate.minimumSoakMinutes < 15)
            {
                throw new InvalidOperationException(
                    "Device matrix release gate must require >=3 physical devices, >=2 OEMs, API 36, >=30 FPS baseline, " +
                    "15+ minute soak, and must reject emulator-only sign-off.");
            }

            if (!File.Exists(ResolveProjectPath(ValidationDocPath)))
            {
                throw new FileNotFoundException("Phase 6 device-matrix validation guide is missing.", ValidationDocPath);
            }
        }

        internal static void ConfigureAndroid16Target()
        {
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        }

        internal static void ValidateTargetSdkOrThrow()
        {
            if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevel36)
            {
                throw new InvalidOperationException(
                    $"Google Play readiness requires Android API 36 target. Current setting: {PlayerSettings.Android.targetSdkVersion}.");
            }
        }

        private static void WriteArtifacts(DeviceMatrixConfig config)
        {
            string outputDirectory = ResolveProjectPath(OutputDirectoryRelativePath);
            Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(ResolveProjectPath(TestPlanRelativePath), BuildTestPlan(config));
            File.WriteAllText(ResolveProjectPath(EvidenceRelativePath), BuildEvidenceCsv(config));
            File.WriteAllText(ResolveProjectPath(StatusRelativePath), BuildStatus(config));
        }

        private static void ValidateGeneratedArtifactsOrThrow(DeviceMatrixConfig config)
        {
            string testPlanPath = ResolveProjectPath(TestPlanRelativePath);
            string evidencePath = ResolveProjectPath(EvidenceRelativePath);
            string statusPath = ResolveProjectPath(StatusRelativePath);
            if (!File.Exists(testPlanPath) || !File.Exists(evidencePath) || !File.Exists(statusPath))
            {
                throw new InvalidOperationException("Device-matrix generated test-plan/evidence/status files are incomplete.");
            }

            string evidence = File.ReadAllText(evidencePath);
            int expectedRows = config.lanes.Length * config.scenarios.Length;
            int pendingCount = evidence.Split(new[] { ",PENDING," }, StringSplitOptions.None).Length - 1;
            if (pendingCount != expectedRows || evidence.Contains(",PASS,", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Generated device evidence must contain exactly {expectedRows} PENDING scenario rows and zero automatic PASS rows. pending={pendingCount}.");
            }

            string status = File.ReadAllText(statusPath);
            if (!status.Contains("targetApi=36", StringComparison.Ordinal) ||
                !status.Contains("physicalEvidence=PENDING", StringComparison.Ordinal) ||
                !status.Contains($"requiredRows={expectedRows}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-matrix status file does not preserve the API 36 / pending-evidence contract.");
            }
        }

        private static string BuildTestPlan(DeviceMatrixConfig config)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Beyond The Beat — Device Matrix Test Plan");
            builder.AppendLine();
            builder.AppendLine("Generated by Phase6DeviceMatrixBuilder. Physical evidence starts PENDING and must be completed manually.");
            builder.AppendLine();
            builder.AppendLine($"Target SDK: Android 16 / API {config.releaseGate.requiredTargetApiLevel}");
            builder.AppendLine($"Release gate: >= {config.releaseGate.minimumPhysicalDevices} physical devices, >= {config.releaseGate.minimumManufacturers} manufacturers, emulator sign-off = {config.releaseGate.emulatorCountsForSignOff}");
            builder.AppendLine($"Performance objective: baseline {config.releaseGate.baselineFps} FPS, stretch {config.releaseGate.stretchFps} FPS, soak >= {config.releaseGate.minimumSoakMinutes} minutes");
            builder.AppendLine();
            builder.AppendLine("## Device lanes");
            foreach (DeviceMatrixLane lane in config.lanes)
            {
                builder.AppendLine($"- **{lane.id}** — {lane.displayName}; {lane.androidVersion} / API {lane.apiLevel}; ~{lane.ramClassGb} GB RAM; {lane.screenClass}; {lane.cutoutCoverage}. {lane.purpose}");
            }
            builder.AppendLine();
            builder.AppendLine("## Required scenarios");
            foreach (DeviceMatrixScenario scenario in config.scenarios)
            {
                builder.AppendLine($"- **{scenario.id} — {scenario.name}:** {scenario.passCriteria}");
            }
            builder.AppendLine();
            builder.AppendLine("## Sign-off boundary");
            builder.AppendLine("CI/emulators may support debugging but cannot satisfy this matrix. Complete DEVICE-MATRIX-EVIDENCE.csv on real physical Android phones and attach evidence before the release-candidate milestone.");
            return builder.ToString();
        }

        private static string BuildEvidenceCsv(DeviceMatrixConfig config)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("lane_id,scenario_id,result,manufacturer,device_model,android_version,api_level,ram_gb,resolution_aspect,refresh_hz,cutout_navigation,soc_gpu,install_result,launch_result,average_fps,p95_frame_ms,thermal_result,touch_result,safe_area_result,evidence_reference,tester,date_utc,notes");

            foreach (DeviceMatrixLane lane in config.lanes)
            {
                foreach (DeviceMatrixScenario scenario in config.scenarios)
                {
                    builder.Append(Csv(lane.id)).Append(',')
                        .Append(Csv(scenario.id)).Append(",PENDING,")
                        .Append(",,")
                        .Append(Csv(lane.androidVersion)).Append(',')
                        .Append(lane.apiLevel).Append(',')
                        .Append(lane.ramClassGb).Append(',')
                        .Append(Csv(lane.screenClass)).Append(',')
                        .Append(",")
                        .Append(Csv(lane.cutoutCoverage)).Append(',')
                        .Append(",,,,,,,,,,,")
                        .AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string BuildStatus(DeviceMatrixConfig config)
        {
            int requiredRows = config.lanes.Length * config.scenarios.Length;
            return
                "Beyond The Beat Phase 6 Device Matrix Status\n" +
                $"targetApi={RequiredTargetApiLevel}\n" +
                $"requiredLanes={config.lanes.Length}\n" +
                $"requiredScenarios={config.scenarios.Length}\n" +
                $"requiredRows={requiredRows}\n" +
                $"minimumPhysicalDevices={config.releaseGate.minimumPhysicalDevices}\n" +
                $"minimumManufacturers={config.releaseGate.minimumManufacturers}\n" +
                $"emulatorCountsForSignOff={config.releaseGate.emulatorCountsForSignOff}\n" +
                "physicalEvidence=PENDING\n" +
                "releaseCandidateSignOff=BLOCKED_UNTIL_PHYSICAL_EVIDENCE\n";
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string escaped = value.Replace("\"", "\"\"");
            return escaped.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? $"\"{escaped}\"" : escaped;
        }

        internal static string ResolveProjectPath(string relativePath)
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root.");
            }

            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
