# Phase 6 — Tutorial / Onboarding Validation

## Milestone goal

Add a lightweight first-launch onboarding flow that teaches the existing mobile driving controls without creating a parallel gameplay, input, mission, or interaction system.

## Automated contract

The Phase 6 tutorial milestone is data-driven through `TutorialProfile` and an ordered set of stable step IDs.

The generated core-control sequence is:

1. `steer` — hold LEFT or RIGHT briefly.
2. `accelerate` — hold GO briefly.
3. `brake-reverse` — hold REV briefly.
4. `action` — press ACTION.

`TutorialController` observes the existing `MobileDrivingInput` contract. It does not write vehicle physics, mission state, interaction state, swim state, camera state, or economy state.

Fast PR validation proves:

- the profile is configured and has unique ordered step IDs;
- unrelated input does not advance the current step;
- steering, acceleration, brake/reverse and ACTION each advance only their matching step;
- the tutorial completes exactly after the fourth step;
- Skip completes the tutorial when the profile permits it;
- `TutorialHud` does not add an `Update()` polling loop;
- PR validation remains independent of scene regeneration and Android packaging.

Integrated editor validation additionally proves:

- one `Phase6Tutorial` controller is wired to the existing `MobileDrivingCanvas` `MobileDrivingInput`;
- one tutorial HUD is attached to the current canvas;
- tutorial panel graphics are non-raycasting except for the explicit Skip button;
- Phase 6 performance plus inherited Phase 5/4/3/1 gameplay roots remain present;
- exactly one enabled gameplay camera and one build scene remain configured.

## Completion persistence

Completed or skipped onboarding writes one stable PlayerPrefs completion key derived from the tutorial ID. The controller can ignore that persisted key for deterministic editor/CI validation. PlayerPrefs is used only for this UX completion preference; gameplay progression remains owned by the existing save/mission systems.

## Android device checklist

Physical testing must use the single `TEST-THIS-BUILD-<run>` artifact produced after the milestone merges to `main`.

Record:

- device model and Android version;
- clean-install first-launch result;
- tutorial panel readability and safe-area placement;
- LEFT/RIGHT recognition without control obstruction;
- GO recognition while steering;
- REV recognition and transition to ACTION step;
- ACTION recognition against a real interaction prompt;
- Skip button tap reliability without accidental underlying control activation;
- completion suppression after relaunch;
- existing DRIVE -> SWIM TEST -> DRIVE and DIVE/SURFACE behavior;
- mission, exploration, parking, cook, repair, mechanic-job, survival and puzzle regressions;
- FPS/frame-time/thermal observations with the tutorial visible and after it completes.

## Acceptance boundary

Automated validation proves configuration, progression semantics, wiring and non-blocking UI contracts. It cannot prove touch ergonomics, text readability, safe-area fit, accidental taps, device-specific input behavior, or whether the onboarding feels understandable.

**CI GREEN IS NOT DEVICE ONBOARDING SIGN-OFF.** Physical Android first-launch, touch, readability, persistence and regression evidence is required before this milestone is considered fully accepted.

## Follow-up

Broad UI/UX polish, store icon/assets, broader device-matrix testing and final release-candidate readiness remain later Phase 6 work.
