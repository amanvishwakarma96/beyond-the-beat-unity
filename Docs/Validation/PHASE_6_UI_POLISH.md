# Phase 6 — Mobile HUD / UI Polish Validation

## Milestone goal

Polish the existing mobile HUD as one coherent presentation layer without changing gameplay, mission, interaction, camera, or input ownership.

The milestone focuses on safe-area behavior, overlay separation, visual consistency, touch safety, and readability on landscape Android devices.

## Runtime architecture

`MobileSafeAreaFitter` is a small reusable component attached only to known HUD/control roots. It stores the authored RectTransform anchors and maps them into `Screen.safeArea` at runtime.

The fitter:

- performs no scene-wide object discovery;
- has no `Update()` polling loop;
- reapplies only on enable, RectTransform dimension change, or application focus restoration;
- keeps authored offsets/positions intact while moving anchor space inside the current safe area;
- falls back to the full screen for invalid/empty safe rectangles;
- exposes deterministic anchor-mapping math for fast CI validation.

## Integrated safe-area targets

The Phase 6 polish builder directly fits these eight known roots:

1. `InteractionHUD`
2. `DrivingControls`
3. `SwimControls`
4. `SwimModeEnter`
5. `Phase1MissionHUD`
6. `Phase4MechanicJobHUD`
7. `TutorialOnboardingPanel`
8. `PerformanceDiagnosticsOverlay`

`SwimModeExit` remains nested beneath `SwimControls`, matching the existing Phase 5 hierarchy. It therefore inherits the `SwimControls` safe-area mapping and intentionally does **not** receive a second `MobileSafeAreaFitter`; fitting both parent and child would double-apply the device inset.

The existing `MobileDrivingInput`, `MobileSwimInput`, touch button mappings, `AquaticModeCoordinator`, mission system, interaction controller, and gameplay camera remain the sources of truth.

## Reference-layout polish

At the 1920 x 1080 authored reference layout:

- Mission HUD remains in the upper-left objective region.
- Tutorial stays upper-center during first-launch onboarding.
- Performance diagnostics remain compact in the upper-right.
- Mechanic Job HUD moves below the performance overlay in the upper-right instead of overlapping the Mission HUD.
- Drive and Swim control roots remain in their existing authored control zones and are only safe-area fitted at runtime.
- The nested `DRIVE` (`SwimModeExit`) button moves with `SwimControls` as one unit.

The tutorial and mechanic-job panels use the same generated rounded mobile visual language already used by the Mission HUD and touch controls.

The tutorial Skip target is increased to **112 x 54** reference units. Presentation graphics remain non-raycasting; only the explicit Skip button and existing gameplay touch controls may receive UI raycasts.

## Fast PR validation

Fast validation proves without scene regeneration or APK packaging:

- full-root safe-area anchor mapping;
- top-center safe-area mapping;
- invalid safe-area fallback and authored-anchor clamping;
- `MobileSafeAreaFitter` has no `Update()` loop;
- reference Mission / Tutorial / Performance / Mechanic regions do not overlap;
- this validation document remains present.

## Integrated editor validation

The full Phase 6 Android preparation additionally proves:

- all eight direct HUD/control roots exist and have configured `MobileSafeAreaFitter` components;
- nested `SwimModeExit` exists under `SwimControls`, remains a Button, and has no second fitter;
- Mission HUD remains non-raycasting;
- Mechanic Job HUD is in the authored upper-right region, uses rounded presentation, and remains non-raycasting;
- Tutorial panel uses rounded presentation, Skip is at least 104 x 48, and only the Skip target raycasts;
- Performance overlay uses rounded presentation and remains non-raycasting;
- five driving and six swim hold controls remain present under their existing input roots;
- one gameplay camera and one build scene remain configured;
- inherited Phase 5 gameplay plus Phase 6 performance, quality, tutorial, and build-size systems remain present.

## Android device checklist

Use the single `TEST-THIS-BUILD-<run>` artifact produced after merge to `main`.

Record:

- device model and Android version;
- screen resolution/aspect ratio;
- notch, punch-hole, rounded-corner, and navigation-gesture layout behavior;
- Mission HUD readability and safe-area fit;
- first-launch Tutorial readability and safe-area fit;
- Skip target comfort and accidental-tap behavior;
- Performance overlay readability in the development build;
- Mechanic Job HUD readability and confirmation that it does not overlap Mission/Tutorial/Performance content;
- LEFT/RIGHT/GO/REV/ACTION multitouch behavior after safe-area fitting;
- SWIM/BACK/DIVE/SURFACE and DRIVE ↔ SWIM mode-button behavior after safe-area fitting;
- confirmation that nested DRIVE moves with the Swim controls and is not over-inset;
- Interaction prompt/speed HUD placement;
- Parking, Cook, Repair, Mechanic Job, mission, survival, restricted-area puzzle, exploration, save/relaunch, and camera-handoff regressions;
- FPS/frame-time/thermal observations with all Phase 6 overlays active where applicable.

## Acceptance boundary

Automated validation proves deterministic layout contracts, safe-area wiring, touch-raycast boundaries, and inherited structure. It cannot prove real device cutout behavior, hand comfort, readability, accidental touches, or visual hierarchy across the Android device matrix.

**CI GREEN IS NOT DEVICE UI/UX SIGN-OFF.** Physical Android safe-area, readability, touch, overlap, and regression evidence is required before this milestone is considered fully accepted.

## Follow-up

Store icon/assets, broader device-matrix testing, and final release-candidate / soft-launch exit validation remain follow-up Phase 6 milestones.
