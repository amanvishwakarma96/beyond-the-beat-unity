# Beyond The Beat — Development Guide

## Current Working Phase

**Phase 0 — Prototype**

The purpose of this phase is to prove the driving and interaction loop before the project expands into missions or biome-specific mechanics.

## Branch Strategy

Stable branch:

```text
main
```

Current development branch:

```text
feature/phase-0-prototype
```

Keep early development simple. Additional feature branches should only be introduced when parallel work or isolated review provides clear value.

## Required Delivery Workflow

Every phase follows the same gated workflow:

1. **Implement** — complete only the scope defined for the current phase.
2. **Developer validation** — verify compilation, scene references, core functional behavior, and regression risks in the Unity Editor.
3. **Device validation** — run the current playable loop on a representative Android device and record functional/performance observations.
4. **Produce shareable artifact** — generate an installable Android APK (or later platform-equivalent build) for stakeholders/testers.
5. **Create validation report** — record build identity, device/environment, test cases, results, known issues, performance observations, and pass/fail status.
6. **PR review** — update the pull request with validation evidence and the artifact location before marking it ready for review.
7. **Merge** — merge only after current-phase exit criteria pass.
8. **Advance phase** — start the next phase only after the previous phase has a validated, shareable build.

The workflow is therefore:

```text
Implement
   ↓
Editor Validation
   ↓
Android Device Validation
   ↓
Shareable APK / Build Artifact
   ↓
Validation Report
   ↓
PR Ready for Review
   ↓
Merge
   ↓
Next Phase
```

### Shareable Artifact Rule

Compiled builds such as `.apk`, `.aab`, and platform packages remain excluded from normal Git commits through `.gitignore`.

A phase artifact should instead be shared through an appropriate build/release mechanism, such as:

- GitHub Actions artifact when CI build automation is introduced
- GitHub Release/pre-release attachment
- Other approved temporary distribution location referenced from the PR

For **Phase 0**, the minimum shareable artifact is:

```text
BeyondTheBeat-Phase0-<build>.apk
```

It must be accompanied by a completed validation report based on `Docs/Validation/PHASE_0_VALIDATION.md`.

The PR must record enough evidence for another person to verify exactly what was tested: artifact name/location, commit SHA, device used, validation status, known issues, and any relevant screenshots/video links when available.

## Phase 0 Implementation Order

1. Bootstrap the Unity URP mobile project.
2. Create the small prototype test scene.
3. Implement and tune vehicle physics.
4. Add smooth vehicle-follow camera.
5. Add mobile-friendly driving input.
6. Create reusable interaction foundation.
7. Implement the parking-zone example.
8. Add minimal interaction/prompt UI.
9. Produce the Android shareable artifact and perform device validation.
10. Complete the validation report, tune handling, and validate Phase 0 exit criteria.
11. Update the draft PR with artifact + validation evidence and mark it ready for review only after validation passes.

## Definition of Done — Phase 0

Phase 0 is complete when:
- The prototype project opens cleanly in the selected Unity LTS editor.
- The test scene is playable without missing references/errors.
- Steering, throttle, braking, suspension, and camera behavior feel coherent.
- Mobile controls can drive the same input abstraction as editor testing.
- The vehicle can enter a parking zone, stop, receive interaction feedback, and complete the interaction.
- The prototype runs acceptably on a representative mid-range Android device.
- A shareable Android APK has been produced and successfully installed/tested.
- `Docs/Validation/PHASE_0_VALIDATION.md` is completed with test results and known issues.
- The PR references the validated artifact and validation evidence.
- No Phase 1+ systems have been prematurely implemented.

## Coding Guidelines

- Keep scripts small and single-responsibility.
- Use clear names such as `VehicleController`, `CameraFollow`, `InteractableObject`, `ParkingZone`, and `UIManager`.
- Keep tuning values serialized/Inspector-accessible when designers need to iterate on feel.
- Prefer composition and events over tightly coupled manager dependencies.
- Avoid hidden global state.
- Do not scatter persistence or PlayerPrefs calls through gameplay code.
- Avoid unnecessary work every frame.
- Document non-obvious physics tuning decisions.

## Mobile Development Guidelines

- Treat touch input as a first-class target rather than a later conversion from desktop controls.
- Keep UI hit areas practical for phones.
- Test landscape ergonomics on a physical Android device.
- Profile CPU, GPU, memory, and garbage collection as content grows.
- Prefer scalable assets and settings suitable for URP/mobile.

## Validation Guidelines

Validation should cover at minimum:

- Project opens and compiles without errors.
- Prototype scene loads without missing references.
- Vehicle accelerates, brakes/reverses, and steers correctly.
- Vehicle physics remains stable during normal prototype driving.
- Camera remains usable without major jitter.
- Touch steering/throttle/brake can be used simultaneously as required.
- Parking prompt appears only when valid.
- Moving vehicle cannot incorrectly complete parking.
- Valid stopped vehicle can complete parking once per interaction cycle.
- Leaving the zone resets/cancels the interaction correctly.
- Android build installs and launches successfully.
- No blocking crash, exception, or obvious continuous GC/performance issue occurs during the validation session.

## Commit Guidance

Use concise, intent-based commit messages. Examples:

```text
chore: bootstrap Unity URP project
feat: add vehicle controller prototype
feat: add smooth vehicle camera
feat: add parking interaction
feat: add mobile driving controls
fix: stabilize low-speed steering
perf: reduce prototype scene allocations
test: document phase 0 device validation
```

Avoid combining unrelated gameplay, documentation, and large asset changes in one commit when practical.

## Pull Request Guidance

A Phase 0 implementation PR should explain:
- What was built
- How to test it in editor
- How to test it on Android
- Vehicle tuning defaults
- Known handling limitations
- Performance observations
- Validation result (PASS / PASS WITH KNOWN ISSUES / FAIL)
- Shareable artifact name/version and location
- Validated commit SHA
- Device(s) used for validation
- Optional screenshot/video evidence when useful
- Explicit confirmation that Phase 1+ scope was not introduced

Do not mark the Phase PR ready for review until the shareable build has been produced and the validation report is complete.

## Phase Progression Rule

Do not advance simply because the code exists. The current phase must be playable, validated against its exit criteria, and available as a shareable build artifact before the next phase begins.
