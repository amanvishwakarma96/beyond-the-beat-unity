# Phase 5 Exploration Mission Validation

## Milestone scope

This milestone adds the first data-driven ocean exploration mission on top of the existing Phase 5 ocean, swim/dive controller, mobile swim input and single-camera handoff.

It does **not** add oxygen/stamina, underwater combat, collectibles, a minimap system, or final Phase 5 exit sign-off.

## Automated architecture contract

- `MissionObjectiveType.ExploreLocations` is additive and does not change existing serialized mission enum values.
- `MissionDefinition` owns stable exploration checkpoint ZoneContext IDs.
- Duplicate, empty or missing exploration IDs make the mission definition invalid.
- `MissionManager` reuses its existing ZoneContext event pipeline; there is no separate exploration polling loop.
- Exploration progress is unique-by-zone-ID and may be completed in any order.
- `WaterVolume`, `SwimController`, `MobileSwimInput` and `AquaticModeCoordinator` contain no exploration mission conditionals.
- The ocean remains the single `WorldZoneType.Ocean` context; checkpoint triggers use additive `WorldZoneType.Exploration`.
- HUD progress is presentation-only and reads `MissionProgressSnapshot`.
- Save data uses additive Phase 5 fields; the save version remains unchanged and older JSON may omit them safely.

## Deterministic CI validation

The fast PR gate validates:

1. Existing swim/dive physics contract.
2. Existing mobile swim input mapping and drive/swim camera handoff.
3. Exploration mission configuration.
4. Wrong actor rejection.
5. Unique checkpoint counting.
6. Duplicate checkpoint rejection.
7. Any-order completion.
8. `1/3`, `2/3`, `3/3` normalized progress semantics.
9. HUD exploration text/progress.
10. Additive exploration save-data round-trip.

The full post-merge Android workflow additionally regenerates the integrated Phase 5 scene, creates the Cove/Reef/Wreck checkpoint volumes, wires the swimmer as the exploration mission actor, updates persistence ZoneContext references, runs integrated validation, and then packages the single `TEST-THIS-BUILD-*` APK.

## Physical Android acceptance

CI GREEN IS NOT DEVICE SIGN-OFF.

On the APK generated after the PR is merged to `main`, verify:

- LEFT / RIGHT / GO / REV / ACTION still work before entering swim mode.
- `SWIM TEST` switches to the existing swim controls and the same gameplay camera follows the swimmer.
- Mission HUD starts at `0/3` exploration checkpoints.
- Cove, Reef and Wreck may be visited in any order.
- Each checkpoint increments progress once only.
- Re-entering a visited checkpoint does not double-count.
- Completing all three checkpoints produces mission completion and leaves free roam available.
- Save/relaunch after one or two checkpoints restores visited exploration progress.
- `DRIVE` restores vehicle controls and the existing camera target.
- Parking, Cook, Repair, Mechanic Job, Reach Location, Reach + Survive, Reach + Solve, forest survival and restricted puzzle/gate remain regression-safe.
- Record FPS/stutter/GC, thermal and battery observations around the shoreline and during an extended swim/exploration session.

## Exit boundary

The next Phase 5 milestone should handle final integration/device exit validation (and any explicitly approved remaining survival/oxygen scope) rather than expanding the Exploration mission system further.
