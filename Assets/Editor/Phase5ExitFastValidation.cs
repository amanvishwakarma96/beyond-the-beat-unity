using UnityEngine;

namespace BeyondTheBeat.Editor
{
    public static class Phase5ExitFastValidation
    {
        public static void Validate()
        {
            Phase5ExplorationFastValidation.Validate();
            Phase5ExitBuilder.ValidateRepositoryContractsOrThrow();

            Debug.Log(
                "[Beyond The Beat] FAST PHASE 5 EXIT VALIDATION PASS: exploration/swim/camera runtime contracts plus " +
                "fast-PR, post-merge single-APK and physical-device documentation contracts are intact. " +
                "No historical scene regeneration or APK packaging was run in the PR gate.");
        }
    }
}
