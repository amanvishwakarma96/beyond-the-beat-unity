# Phase 4 Validation

## Scope

Phase 4 — Free Roam Expansion validates four activities on the shared interaction foundation:

- Parking
- Cook
- Repair
- Mechanic Job

No new gameplay feature is introduced by this exit gate. It consolidates the already-implemented Phase 4 milestones and their inherited Phase 0–3 regressions.

## Automated CI Gate

The exact PR head must pass the single **Current Android Test Build** workflow. The automated Phase 4 exit validator checks:

- Parking, Cook and Repair are wired through `InteractableObject` / `InteractionTrigger`.
- Cook and Repair reuse `TimedActivityInteractable`.
- Mechanic Job consumes the existing `RepairStation.RepairCompleted` path instead of adding a parallel interaction.
- Exactly one shared `InteractionController` and one `MobileDrivingInput` are present in the Phase 4 scene.
- LEFT, RIGHT, GO, REV and ACTION references are assigned; direct New Input System and legacy touch fallbacks remain enabled.
- Exactly one EventSystem is present.
- `MechanicJobManager`, `CreditWallet`, and non-raycasting `MechanicJobHud` are correctly wired.
- Parking, Phase 1 mission, and Phase 3 restricted-area roots remain present.
- Build Settings contain only `Assets/Scenes/Phase4/Phase4_FreeRoam.unity`.
- The repository retains one `Current Android Test Build` entry point and one `TEST-THIS-BUILD-*` device-test artifact contract.

Automated behavior validators also re-check Cook cancel/repeat behavior, Vehicle Repair cancel/re-damage/repeat behavior, Mechanic Job matching/unrelated repair behavior, and one-time credit rewards.

## Physical Android Gate — PENDING DEVICE VALIDATION

CI GREEN IS NOT DEVICE SIGN-OFF.

Install only the latest `TEST-THIS-BUILD-<run>` artifact and record the device/model, Android version, source commit, APK checksum, and observations below.

### Core controls and driving

- [ ] LEFT works.
- [ ] RIGHT works.
- [ ] GO works.
- [ ] REV / brake works.
- [ ] ACTION works while another driving touch is held.
- [ ] Vehicle moves, steers, brakes, reverses, and camera remains usable.

### Parking

- [ ] Parking prompt appears only when eligible.
- [ ] ACTION completes parking once.
- [ ] Leaving/re-entering resets the visit correctly.

### Cook

- [ ] COOK prompt appears at the Cooking Station.
- [ ] ACTION starts the timed activity.
- [ ] Leaving the trigger cancels without counting completion.
- [ ] Re-entering allows restart and completion.
- [ ] Cooking can be repeated without stuck/duplicate state.

### Repair

- [ ] REPAIR prompt appears for the damaged prototype vehicle.
- [ ] Leaving during repair cancels and keeps damage unchanged.
- [ ] Completing repair restores full condition once.
- [ ] Fully repaired target rejects another repair.
- [ ] Re-damaging allows another repair cycle.

### Mechanic Job

- [ ] Active mechanic job is readable on the HUD.
- [ ] Cancelling repair does not complete or pay the job.
- [ ] Matching completed repair marks JOB COMPLETE.
- [ ] Exactly the configured reward is credited once.
- [ ] No duplicate payment occurs from further repair signals.
- [ ] A new valid paid cycle works after clear + re-damage.

### Regression and performance

- [ ] Reach Location still works.
- [ ] Reach + Survive still works.
- [ ] Reach + Solve / restricted gate puzzle still works.
- [ ] Save/resume regressions remain acceptable.
- [ ] No blocking input overlap from Phase 4 HUD.
- [ ] Record FPS/stutter/GC observations.
- [ ] Record thermal/battery observations during an extended session.

## Evidence

- Device: PENDING
- Android version: PENDING
- Source commit: PENDING
- Workflow run: PENDING
- Artifact: PENDING
- APK SHA-256: PENDING
- Result: **PENDING DEVICE VALIDATION**

## Phase 4 Exit Rule

Phase 4 can be formally signed off only after automated validation is green **and** the physical Android checklist has recorded evidence. Phase 5 Ocean / Exploration must not be treated as validated progression based on CI alone.
