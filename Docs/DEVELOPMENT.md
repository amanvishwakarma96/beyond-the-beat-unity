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

## Phase 0 Implementation Order

1. Bootstrap the Unity URP mobile project.
2. Create the small prototype test scene.
3. Implement and tune vehicle physics.
4. Add smooth vehicle-follow camera.
5. Add mobile-friendly driving input.
6. Create reusable interaction foundation.
7. Implement the parking-zone example.
8. Add minimal interaction/prompt UI.
9. Produce and test an Android build.
10. Tune handling and validate Phase 0 exit criteria.

## Definition of Done — Phase 0

Phase 0 is complete when:
- The prototype project opens cleanly in the selected Unity LTS editor.
- The test scene is playable without missing references/errors.
- Steering, throttle, braking, suspension, and camera behavior feel coherent.
- Mobile controls can drive the same input abstraction as editor testing.
- The vehicle can enter a parking zone, stop, receive interaction feedback, and complete the interaction.
- The prototype runs acceptably on a representative mid-range Android device.
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
- Explicit confirmation that Phase 1+ scope was not introduced

## Phase Progression Rule

Do not advance simply because the code exists. The current phase must be playable and validated against its exit criteria before the next phase begins.
