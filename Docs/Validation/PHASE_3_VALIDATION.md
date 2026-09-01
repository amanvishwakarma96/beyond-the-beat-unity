# Beyond The Beat — Phase 3 Validation

## Milestone
Restricted Area Puzzle Gate + Reach & Solve + Persistence/Resume

## Automated repository gate
The Current Android Test Build must complete all of the following on the exact PR head before the APK is considered a valid test candidate:

- Rebuild and validate the integrated Phase 2 prerequisite.
- Generate and validate the Phase 3 restricted-area foundation.
- Validate the restricted ZoneContext, pressure-plate puzzle and reusable gate binding.
- Generate and validate the data-driven Reach + Solve mission.
- Validate both completion orders: puzzle-first and area-first.
- Validate puzzle reset/retry behavior.
- Validate Phase 3 persistence/resume and older save defaults.
- Validate the authored Reach + Solve HUD state matrix and progress strip.
- Validate mobile controls are still wired to the vehicle with direct and legacy touch fallbacks enabled.
- Validate the integrated Phase 3 scene is the only Android Player scene.
- Produce only the single `TEST-THIS-BUILD-<run>` artifact.

## Automated acceptance status
Automated checks are evaluated by `Phase3ExitBuilder.ValidateExitIntegrationOrThrow` during `Phase3BuildAutomation.BuildAndroid`.

**CI GREEN IS NOT DEVICE SIGN-OFF.**

A green workflow only proves repository compilation, deterministic editor validation and Android Player generation. It does not prove touchscreen usability, physics feel, device performance or thermal behavior.

## Physical Android acceptance — required before formal Phase 3 exit
Install only the APK contained in the latest exact-head `TEST-THIS-BUILD-<run>` artifact.

### Touch and vehicle controls
- [ ] LEFT / RIGHT / GO / REV / ACTION all respond on a real Android touchscreen.
- [ ] LEFT + GO works as simultaneous multi-touch.
- [ ] RIGHT + GO works as simultaneous multi-touch.
- [ ] REV/braking works while steering.
- [ ] ACTION works without blocking steering/throttle input.
- [ ] Actual vehicle movement, steering and braking match the visible control state.

### Restricted-area puzzle
- [ ] Restricted gate starts closed and blocks entry.
- [ ] The crate / pressure plate interaction can be completed using the playable controls/physics loop.
- [ ] Sufficient weight solves the puzzle and unlocks/opens the gate.
- [ ] Removing/resetting the weight relocks/closes the gate when the objective is still retryable.
- [ ] No stuck gate, duplicate solve event or impossible retry state occurs.

### Reach + Solve mission and HUD
- [ ] Initial HUD clearly communicates both PUZZLE and AREA steps.
- [ ] Puzzle-first flow changes HUD to enter-area guidance.
- [ ] Area-first flow changes HUD to solve-puzzle guidance.
- [ ] Completing both conditions transitions to Mission Complete and free roam remains usable.
- [ ] HUD never consumes/blocks driving or ACTION touches.

### Persistence/resume
- [ ] Save while Reach + Solve is active and puzzle is unsolved, relaunch, and confirm the unsolved state resumes correctly.
- [ ] Save after solving the puzzle, relaunch, and confirm the gate/puzzle remains solved without requiring a second solve.
- [ ] persistence after relaunch restores mission context and does not corrupt vehicle placement.
- [ ] Reset progress restores the configured new-game puzzle/gate state.

### Regression and mobile performance
- [ ] Existing forest survival flow remains usable.
- [ ] Parking/free-roam interaction remains usable.
- [ ] No blocking exception, missing reference, frozen input or broken camera is observed.
- [ ] No obvious repeated GC hitching during driving/puzzle interaction.
- [ ] Record approximate FPS/stutter observations on the target device.
- [ ] Record thermal behavior after an extended drive/puzzle session.

## Exit decision
Phase 3 is formally complete only when:

1. The Current Android Test Build is green on the exact final commit.
2. The single test APK checksum/artifact is traceable to that commit.
3. Every required physical Android acceptance item above is verified.
4. Any blocking device defect is fixed and re-tested on a new exact-head build.
