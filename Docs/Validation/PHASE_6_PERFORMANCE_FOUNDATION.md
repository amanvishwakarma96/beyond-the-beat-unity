# Phase 6 — Mobile Performance Foundation Validation

## Purpose

This milestone starts Phase 6 by turning the soft-launch performance targets into executable project contracts before tutorial, UI polish, store assets or deeper content optimization are added.

## Budget

The single `MobilePerformanceBudget` asset owns the current targets:

- baseline frame rate: **30 FPS**
- stretch frame rate: **60 FPS**
- frame-time warning: **37 ms**
- allocated-memory warning: **1024 MB**
- Android APK ceiling: **200 MB**
- runtime sample interval: **1 second**

The 30 FPS target is the acceptance baseline. The 60 FPS value is a stretch target, not a promise for every device.

## Automated PR validation

`Fast Current Milestone Validation` must:

- compile the Unity project;
- retain the Phase 5 swim/camera/exploration gameplay contracts;
- validate the 30/60 FPS data contract;
- validate FPS, frame-time and allocated-memory warning classification;
- confirm the diagnostics overlay does not own an `Update` loop;
- avoid historical scene regeneration;
- avoid Android APK packaging.

## Full post-merge validation

A merge/push to `main` automatically starts `Current Android Test Build`.

The full build must:

1. regenerate/validate the integrated Phase 5 gameplay slice;
2. create one `Phase6Performance` monitor root;
3. create one non-raycasting development diagnostics overlay;
4. validate the shared performance budget asset;
5. build one Android `TEST-THIS-BUILD-*` APK;
6. read the APK ceiling from `Phase6_MobilePerformanceBudget.asset`;
7. fail packaging if the APK exceeds that configured ceiling;
8. publish APK size, SHA-256 and device checklist in the artifact manifest.

## Runtime architecture

`MobilePerformanceMonitor` performs only lightweight frame counting in its frame loop. Expensive/formatting work is not done every frame:

- memory is sampled only at the configured interval;
- the overlay formats text only when a sample event is emitted;
- no scene-wide runtime search is used;
- gameplay systems do not depend on profiling code;
- the diagnostics overlay disables itself in non-debug/non-development players.

## Physical Android validation — required

CI measurements do **not** replace target-device profiling.

For each tested device record:

- device model;
- Android version;
- chipset/RAM when known;
- APK build/run number;
- APK size;
- driving FPS and frame time;
- forest/restricted-area FPS and frame time;
- shoreline FPS and frame time;
- surface-swim FPS and frame time;
- underwater FPS and frame time;
- allocated-memory observations;
- visible stutter or GC spikes;
- thermal/throttling behavior;
- battery drop over a representative session;
- crashes or ANRs.

Minimum functional regression during the profile session:

- LEFT / RIGHT / GO / REV / ACTION;
- Parking;
- Cook;
- Repair;
- Mechanic Job;
- forest survival;
- restricted pressure-plate/gate;
- SWIM TEST / DIVE / SURFACE / DRIVE handoff;
- ocean-cove / ocean-reef / ocean-wreck exploration;
- save/relaunch.

## Acceptance boundary

Automated milestone acceptance requires the budget/profile wiring and Android package-size gate to pass.

Physical performance acceptance still requires a representative mid-range Android device to demonstrate a stable **30 FPS baseline** with acceptable stutter, thermal and battery behavior.

**CI GREEN IS NOT DEVICE PERFORMANCE SIGN-OFF.**

## Deferred Phase 6 scope

The following remain separate milestones:

- deeper render/physics/content optimization based on measured hotspots;
- tutorial/onboarding;
- broader UI/UX polish;
- texture/audio/build-content size optimization;
- store icon/assets and listing readiness;
- broader device matrix;
- release-candidate AAB/APK and final soft-launch validation.
