using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BeyondTheBeat.Editor
{
    internal static class Phase6DeviceMatrixFastValidation
    {
        private const string BuildAutomationPath = "Assets/Editor/Phase6BuildAutomation.cs";
        private const string DeviceMatrixBuilderPath = "Assets/Editor/Phase6DeviceMatrixBuilder.cs";
        private const string FullWorkflowPath = ".github/workflows/phase2-forest-foundation.yml";
        private const string FastWorkflowPath = ".github/workflows/fast-current-milestone-validation.yml";

        internal static void ValidateDeviceMatrixOnly()
        {
            DeviceMatrixConfig config = Phase6DeviceMatrixBuilder.LoadConfigOrThrow();
            Phase6DeviceMatrixBuilder.ValidateConfigOrThrow(config);

            bool api36EnumPass = Enum.GetNames(typeof(AndroidSdkVersions))
                .Contains(nameof(AndroidSdkVersions.AndroidApiLevel36), StringComparer.Ordinal);
            bool lanesPass = config.lanes.Length >= Phase6DeviceMatrixBuilder.RequiredLaneCount &&
                             config.lanes.Count(lane => lane.physicalRequired) == config.lanes.Length &&
                             config.lanes.Any(lane => lane.apiLevel == 30) &&
                             config.lanes.Any(lane => lane.apiLevel == 33) &&
                             config.lanes.Any(lane => lane.apiLevel == 36);
            bool scenariosPass = config.scenarios.Length >= Phase6DeviceMatrixBuilder.RequiredScenarioCount &&
                                 config.scenarios.Select(scenario => scenario.id).Distinct(StringComparer.Ordinal).Count() == config.scenarios.Length &&
                                 config.scenarios.All(scenario => !string.IsNullOrWhiteSpace(scenario.passCriteria));
            bool physicalGatePass = config.releaseGate.minimumPhysicalDevices >= 3 &&
                                    config.releaseGate.minimumManufacturers >= 2 &&
                                    config.releaseGate.requirePhysicalHardware &&
                                    !config.releaseGate.emulatorCountsForSignOff &&
                                    config.releaseGate.requiredTargetApiLevel == 36 &&
                                    config.releaseGate.minimumSoakMinutes >= 15;
            bool repositoryPass = ValidateRepositoryContracts();

            if (!api36EnumPass || !lanesPass || !scenariosPass || !physicalGatePass || !repositoryPass)
            {
                throw new InvalidOperationException(
                    "Phase 6 fast device-matrix validation failed: " +
                    $"api36Enum={api36EnumPass}, lanes={lanesPass}, scenarios={scenariosPass}, " +
                    $"physicalGate={physicalGatePass}, repository={repositoryPass}.");
            }

            Debug.Log(
                "[Beyond The Beat] Phase 6 fast device-matrix validation PASS: API 30/33/36 physical lanes, " +
                "12-scenario evidence contract, >=3-device/>=2-OEM release gate, emulator rejection and API 36 build preparation are intact.");
        }

        private static bool ValidateRepositoryContracts()
        {
            string buildAutomation = ReadProjectFile(BuildAutomationPath);
            string deviceMatrixBuilder = ReadProjectFile(DeviceMatrixBuilderPath);
            string fullWorkflow = ReadProjectFile(FullWorkflowPath);
            string fastWorkflow = ReadProjectFile(FastWorkflowPath);
            string validationDoc = ReadProjectFile(Phase6DeviceMatrixBuilder.ValidationDocPath);
            string config = ReadProjectFile(Phase6DeviceMatrixBuilder.ConfigPath);

            return buildAutomation.Contains("Phase6DeviceMatrixBuilder.PrepareAndValidateOrThrow", StringComparison.Ordinal) &&
                   buildAutomation.Contains("Phase6DeviceMatrixBuilder.ValidateTargetSdkOrThrow", StringComparison.Ordinal) &&
                   deviceMatrixBuilder.Contains("AndroidSdkVersions.AndroidApiLevel36", StringComparison.Ordinal) &&
                   deviceMatrixBuilder.Contains("physicalEvidence=PENDING", StringComparison.Ordinal) &&
                   deviceMatrixBuilder.Contains("releaseCandidateSignOff=BLOCKED_UNTIL_PHYSICAL_EVIDENCE", StringComparison.Ordinal) &&
                   fullWorkflow.Contains("DEVICE-MATRIX", StringComparison.Ordinal) &&
                   fullWorkflow.Contains("DEVICE-MATRIX-STATUS.txt", StringComparison.Ordinal) &&
                   fullWorkflow.Contains("target_api=${TARGET_API}", StringComparison.Ordinal) &&
                   fullWorkflow.Contains("TEST-THIS-BUILD-${GITHUB_RUN_NUMBER}", StringComparison.Ordinal) &&
                   fastWorkflow.Contains("BeyondTheBeat.Editor.Phase6PerformanceFastValidation.Validate", StringComparison.Ordinal) &&
                   fastWorkflow.Contains("pull_request:", StringComparison.Ordinal) &&
                   !fastWorkflow.Contains("androidExportType: androidPackage", StringComparison.Ordinal) &&
                   validationDoc.Contains("CI GREEN IS NOT DEVICE-MATRIX SIGN-OFF", StringComparison.Ordinal) &&
                   validationDoc.Contains("API level 36", StringComparison.Ordinal) &&
                   config.Contains("\"apiLevel\": 30", StringComparison.Ordinal) &&
                   config.Contains("\"apiLevel\": 33", StringComparison.Ordinal) &&
                   config.Contains("\"apiLevel\": 36", StringComparison.Ordinal) &&
                   config.Contains("\"emulatorCountsForSignOff\": false", StringComparison.Ordinal);
        }

        private static string ReadProjectFile(string relativePath)
        {
            string path = Phase6DeviceMatrixBuilder.ResolveProjectPath(relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Required device-matrix repository file is missing: {relativePath}", path);
            }
            return File.ReadAllText(path);
        }
    }
}
