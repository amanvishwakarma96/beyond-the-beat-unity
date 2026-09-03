# Phase 6 — Mobile Quality Optimization Validation

## Goal

Apply a low-risk mobile rendering profile after the Phase 6 performance instrumentation foundation, then measure the effect on target Android hardware before any physics or gameplay-feel tuning.

## Automated profile

The current `MobileQualityProfile` is intentionally conservative:

- shadow distance: **35 m**
- shadow cascades: **2**
- shadow quality: **Hard Only**
- shadow resolution: **Medium**
- anti-aliasing: **2x MSAA**
- LOD bias: **0.8**
- realtime reflection probes: **Off**
- soft particles: **Off**
- gameplay-camera HDR: **Off**
- gameplay-camera MSAA: **On**

The profile is applied once by `MobileQualityBootstrap`. It does not add `Update`, `LateUpdate`, or `FixedUpdate` polling.

## Scoped renderer optimization

Only known generated decorative renderers are modified:

1. `Phase5OceanArea/OceanSurface`
2. the three `Marker` renderers under `Phase5ExplorationCheckpoints`

For those renderers only:

- shadow casting is disabled;
- shadow receiving is disabled;
- light-probe sampling is disabled;
- reflection-probe sampling is disabled.

Vehicle, swimmer, restricted gate, pressure plate, mission actors and interaction objects are not included in this renderer pass.

## Automated validation

The editor/CI gate must prove:

- exactly one configured mobile quality profile;
- exactly one `MobileQualityBootstrap` on `Phase6Performance`;
- the bootstrap references the single gameplay camera;
- no per-frame quality polling methods exist;
- the gameplay camera is HDR-off / MSAA-on;
- exactly four known decorative renderers are optimized;
- the Phase 6 performance monitor remains present;
- inherited Ocean, Swim, Exploration, Vehicle, Restricted Area and Mission systems remain present;
- exactly one enabled gameplay camera remains;
- exactly one Phase 5 integrated scene remains in Build Settings;
- PR CI remains fast and the full Android build remains merge-to-main triggered;
- the existing 200 MB APK size gate remains active.

## Physical Android validation

Use the newest `TEST-THIS-BUILD-*` artifact produced from `main`.

Record device model, Android version, build/run number and source SHA.

### Performance comparison

Use the development performance overlay and record FPS / frame time / memory for at least:

1. normal driving;
2. dense/obstacle driving area;
3. forest/survival area;
4. restricted puzzle/gate area;
5. shoreline;
6. surface swimming;
7. underwater swimming;
8. exploration checkpoint traversal.

Compare these readings with the previous Phase 6 performance-foundation build when possible.

### Visual regression checks

Verify:

- no unacceptable shadow popping around the vehicle or mission-relevant geometry;
- hard shadows remain readable enough for gameplay;
- 2x MSAA does not introduce distracting aliasing on roads/UI/world edges;
- Ocean surface remains readable with HDR disabled;
- Cove/Reef/Wreck markers remain visible and understandable;
- DRIVE ↔ SWIM TEST camera handoff remains visually stable;
- no unexpected brightness/exposure change appears after switching modes;
- moving gate, vehicle and swimmer visuals were not stripped of required lighting behavior.

### Gameplay regression checks

Regression-test:

- LEFT / RIGHT / GO / REV / ACTION;
- parking;
- Cook;
- Repair;
- Mechanic Job;
- Reach Location;
- Reach + Survive;
- Reach + Solve;
- restricted puzzle/gate;
- SWIM TEST / DRIVE;
- DIVE / SURFACE;
- Exploration 0/3 → 3/3;
- save/relaunch exploration progress.

## Acceptance boundary

This milestone is an optimization configuration pass, not proof of a performance gain by itself.

**CI GREEN IS NOT DEVICE PERFORMANCE SIGN-OFF.**

The optimization is accepted only after target-device evidence shows no unacceptable gameplay/visual regression and provides FPS/frame-time/thermal observations against the Phase 6 performance budget.

## Deferred Phase 6 work

Still separate:

- physics timestep/solver tuning;
- texture/audio compression and build-content stripping;
- tutorial/onboarding;
- broad UI/UX polish;
- store assets;
- broader device matrix;
- release-candidate AAB/APK validation.
