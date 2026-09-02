# Phase 5 Swim / Dive Controller Foundation Validation

## Scope

This milestone adds the aquatic locomotion controller foundation on top of the Phase 5 Ocean/WaterVolume scene. It does **not** add the exploration mission, oxygen/stamina, underwater post-processing, animation, vehicle buoyancy, or final player/mobile camera-control handoff.

The repository currently has no general on-foot PlayerController. `SwimController` therefore remains a reusable aquatic movement component with external input commands rather than being incorrectly coupled to vehicle controls or mission code.

## Automated CI gate

The exact PR head must pass all of the following before the Android candidate is produced:

- Phase 4 final automated exit regression.
- Phase 5 Ocean Foundation generation and validation.
- One configured `WaterVolume` / `WorldZoneType.Ocean` context.
- `Phase5SwimPrototype/SwimPrototypeActor` with Rigidbody + CapsuleCollider + `SwimController`.
- `SwimController` references the existing `WaterVolume` and does not add a second camera/light stack.
- Aquatic movement is processed through `FixedUpdate`; the controller declares no `Update` polling loop.
- Dry → Surface entry disables gravity and uses the water surface target.
- Horizontal surface input produces forward swim target velocity.
- Dive request transitions Surface → Underwater.
- Requested dive depth is clamped below the WaterVolume bottom boundary.
- Underwater target velocity moves downward when the actor is above the requested dive depth.
- Surface request transitions Underwater → Surface and produces upward velocity when the actor is too deep.
- Water exit restores Dry state and the Rigidbody gravity baseline.
- Re-entry returns to Surface cleanly.
- Phase 4 activities, parking, mission system, restricted-area puzzle, mechanic job/economy, and mobile driving canvas remain present.
- Exactly one Phase 5 scene remains in build settings.
- Current Android workflow continues to publish only `TEST-THIS-BUILD-*` as the current device-test artifact.

## Mobile / performance implications

- No scene-wide runtime searches are used by `SwimController`.
- Water enter/exit is event-driven through the existing `ZoneContext` when integrated physically.
- The controller performs only Rigidbody target-velocity work in `FixedUpdate`.
- No extra water camera, reflection/refraction pass, underwater post-process, particle field, or per-frame wave simulation is added.
- The prototype actor is intentionally lightweight and exists to validate the controller contract before mobile controls/camera handoff is added.

## Physical Android checks for this milestone

CI green does not prove target-device behavior. On the single current APK, record:

| Check | Result | Notes |
| --- | --- | --- |
| App installs/launches | PENDING | |
| Existing driving LEFT/RIGHT/GO/REV/ACTION regression | PENDING | |
| Ocean area still renders without obvious stalls | PENDING | |
| Phase 5 swim prototype exists in the ocean scene without extra-camera regressions | PENDING | |
| Extended ocean-area FPS/stutter/GC observation | PENDING | |
| Thermal behavior during extended water-area session | PENDING | |
| Phase 4 Parking/Cook/Repair/Mechanic Job smoke regression | PENDING | |
| Existing mission/puzzle/save-resume smoke regression | PENDING | |

Direct mobile swim control and swim camera handoff are explicitly deferred to the next Phase 5 integration milestone, so they are not falsely marked PASS here.

## Exit status

Automated status: **PENDING exact-head CI**  
Physical Android status: **PENDING**

Do not treat this document or CI success as final Phase 5 acceptance. The later mobile swim integration, exploration mission, and final Phase 5 exit validation remain required.
