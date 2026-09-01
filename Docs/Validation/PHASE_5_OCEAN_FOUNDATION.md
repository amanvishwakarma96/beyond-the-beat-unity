# Phase 5 — Ocean Foundation Validation

## Milestone

Issue #64 — Ocean ZoneContext + reusable WaterVolume + mobile-friendly static ocean surface.

This is the first Phase 5 milestone only. It does **not** include swimming, diving, oxygen, underwater camera behavior or the Exploration mission type.

## Automated CI gate

The exact PR head must pass the single **Current Android Test Build** pipeline. Before the APK is built, CI must:

- deterministically rebuild and validate the complete Phase 4 prerequisite;
- preserve the serialized numeric values of `Urban=0`, `OffRoad=1`, `Forest=2`, `Restricted=3`;
- add `Ocean=4` additively;
- generate `Assets/Scenes/Phase5/Phase5_Ocean.unity` from the complete Phase 4 scene;
- create exactly one `ocean` `ZoneContext` using `WorldZoneType.Ocean`;
- create one `WaterVolume` wired to that ZoneContext and trigger collider;
- verify deterministic surface/depth queries;
- verify `WaterVolume` introduces no per-frame `Update()` loop;
- verify the ocean surface uses a single opaque lightweight material and adds no camera/reflection/refraction system;
- retain the Phase 4 free-roam activities, mission system, restricted-area puzzle, economy/job system and mobile HUD/input roots;
- build only the Phase 5 scene;
- publish only the single `TEST-THIS-BUILD-<run>` current test artifact.

## Physical Android validation — required separately

CI green is not device sign-off.

Record:

| Check | Result / Notes |
| --- | --- |
| Device model | PENDING |
| Android version | PENDING |
| APK artifact/run | PENDING |
| Install + launch | PENDING |
| LEFT / RIGHT / GO / REV / ACTION regression | PENDING |
| Actual vehicle movement/steering/braking/reverse | PENDING |
| Ocean surface visible at shoreline | PENDING |
| No obvious flicker/missing material | PENDING |
| Camera/control behavior near ocean unchanged | PENDING |
| Parking regression | PENDING |
| Cook regression | PENDING |
| Repair regression | PENDING |
| Mechanic Job + credits regression | PENDING |
| Existing mission/puzzle/save regressions | PENDING |
| FPS before ocean | PENDING |
| FPS looking across ocean | PENDING |
| Stutter/GC observations | PENDING |
| Thermal/battery observations | PENDING |

## Performance implications

The milestone intentionally avoids real-time reflections, refraction, planar cameras, screen captures, wave simulation and buoyancy. The prototype ocean uses a static opaque surface so the first device test can measure the incremental cost of the expanded scene/visible water area before more expensive ocean features are considered.

## Exit decision

Status: **AUTOMATED CI PENDING / PHYSICAL DEVICE PENDING**.

The next Phase 5 milestone (swim/dive controller) may be developed by explicit project-owner direction, but this milestone must not be described as physically validated until the Android evidence above is recorded.
