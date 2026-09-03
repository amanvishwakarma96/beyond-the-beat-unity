# Phase 5 Mobile Swim Controls + Camera Handoff Validation

## Milestone boundary

This slice integrates the existing `SwimController` with mobile touch input and the existing `CameraFollow` gameplay camera. It does not add the Exploration mission type, oxygen/stamina, underwater post-processing, combat swimming, or a general on-foot controller.

## Automated PR validation

`.github/workflows/fast-current-milestone-validation.yml` (`Fast Current Milestone Validation`) is the automatic pull-request gate. It validates:

- Unity script compilation.
- fail-closed Android Active Input Handling = Both guard.
- existing swim/dive physics contract.
- deterministic mobile swim movement mapping.
- DIVE/SURFACE conflict handling with SURFACE winning safely.
- `AquaticModeCoordinator` has no `Update()` polling loop.
- drive → swim → drive input ownership transition.
- camera target switches between the existing vehicle and swim prototype using the same `CameraFollow` component.

The fast PR gate intentionally does not rebuild historical phase scenes and does not package an APK.

## Full integrated Android build

`.github/workflows/phase2-forest-foundation.yml` retains the historical single-APK contract used by older regression validators, but its displayed workflow is now `Current Android Test Build`. It performs the expensive integrated generation/validation/build path. It can be started manually and is also triggered automatically whenever changes are pushed to `main`, including PR merge commits.

The integrated validator must confirm:

- one `MobileDrivingCanvas`.
- existing `DrivingControls` remain intact.
- one `SwimControls` overlay with LEFT, RIGHT, SWIM, BACK, DIVE and SURFACE hold controls.
- direct Input System and legacy touch fallbacks remain enabled for swim controls.
- one `SWIM TEST` mode-entry control and one `DRIVE` return control.
- driving input is disabled while swim input owns control.
- swim input is disabled outside swim mode.
- exactly one enabled gameplay camera exists.
- the same `CameraFollow` changes target for drive/swim handoff.
- Phase 4 activities, mission roots, restricted-area puzzle and Phase 5 ocean/swim foundations remain present.
- exactly one Phase 5 build scene is enabled.

## Physical Android acceptance

CI green is not device sign-off. On the automatically generated post-merge APK, verify:

1. Start in normal driving mode; LEFT/RIGHT/GO/REV/ACTION still work.
2. Tap `SWIM TEST`; driving controls hide and swim controls appear.
3. The existing gameplay camera moves to the swim prototype without creating a second camera or visible camera conflict.
4. LEFT/RIGHT change lateral swim movement as expected.
5. SWIM moves forward and BACK moves backward.
6. DIVE moves the swimmer to the configured underwater target depth.
7. SURFACE returns the swimmer toward the surface and overrides DIVE if both are held.
8. Multitouch works for direction + forward/back + depth controls.
9. Tap `DRIVE`; swim controls hide, vehicle controls return and camera target returns to the vehicle.
10. Re-enter swim mode and repeat to check state cleanup.
11. Check shoreline/ocean FPS, stutter, GC spikes, thermal behavior and battery impact on the target Android device.
12. Regression-check Parking, Cook, Repair, Mechanic Job, mission HUD, Reach Location, Reach + Survive, Reach + Solve, restricted gate/puzzle and save/resume.

## Performance notes

- Swim physics remains in `FixedUpdate()`.
- `AquaticModeCoordinator` is transition-driven and does not poll per frame.
- `MobileSwimInput.Update()` only reads input and forwards commands; it does not perform water/world discovery.
- No extra camera, reflection camera, underwater camera, lighting stack or post-processing volume is introduced by this milestone.
