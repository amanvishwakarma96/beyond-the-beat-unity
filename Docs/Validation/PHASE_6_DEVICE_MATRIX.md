# Phase 6 — Android Device Matrix Validation

## Milestone goal

Turn device testing into a repeatable release gate instead of an informal checklist.

This milestone defines the representative Android coverage that must be exercised before the Phase 6 release candidate can be called ready. CI prepares and validates the matrix contract, but **physical-device evidence remains mandatory**.

## Current Android / Google Play boundary

As of 31 August 2026, new Google Play apps and app updates must target Android 16 / API level 36 or higher.

The Phase 6 full Android build therefore explicitly sets:

- target platform: Android
- target SDK: Android 16 / API 36
- release-candidate device matrix includes a real Android 16 physical phone

Repository automation validates the target-SDK configuration before APK packaging.

## Required physical lanes

The structured source of truth is `Docs/Validation/PHASE_6_DEVICE_MATRIX.json`.

Three lanes are required:

1. **Legacy / entry — Android 11 / API 30**
   - approximately 4 GB RAM class
   - 720p-class landscape phone
   - catches low-memory, older-platform and baseline 30 FPS regressions
2. **Mainstream mid-range — Android 13 / API 33**
   - approximately 6 GB RAM class
   - 1080p-class 60/90 Hz phone
   - primary soft-launch quality lane
3. **Current platform — Android 16 / API 36**
   - approximately 8 GB RAM class
   - 1080p+ high-refresh phone with modern cutout/gesture navigation
   - validates the current Google Play target platform and modern runtime behavior

Before release-candidate sign-off:

- at least **3 physical phones** must pass;
- at least **2 manufacturers/OEMs** must be represented;
- emulator-only evidence never satisfies the gate.

The lanes describe the required coverage class, not a permanently hard-coded phone model. Actual phones should be chosen from hardware available to the team and, once a Play build is uploaded, cross-checked against Play Console's **Device catalog**.

## Required scenario suite

Each physical lane must execute all 12 stable scenarios:

- `DM-01-COLD-LAUNCH` — install and cold launch
- `DM-02-ONBOARDING` — tutorial progression, Skip and persistence
- `DM-03-DRIVE-MULTITOUCH` — LEFT/RIGHT + GO/REV touch combinations
- `DM-04-FREE-ROAM-INTERACTIONS` — parking, cook and repair
- `DM-05-MECHANIC-JOB` — job completion, credits and HUD
- `DM-06-MISSIONS-SAVE` — mission/exploration progress plus force-close/relaunch restore
- `DM-07-FOREST-SURVIVAL` — contextual survival behavior
- `DM-08-RESTRICTED-PUZZLE` — puzzle gate lock/unlock/reset
- `DM-09-SWIM-CAMERA` — swim/dive controls and camera handoff
- `DM-10-SAFE-AREA-UI` — cutout, rounded-corner, gesture-nav and HUD overlap/readability
- `DM-11-STORE-SCREENSHOTS` — capture truthful real-gameplay store screenshots
- `DM-12-SOAK-THERMAL` — at least 15 minutes of mixed sustained gameplay

The JSON file owns the complete pass criteria for every scenario.

## Evidence captured per test row

The post-merge Android build generates a CSV with one row per `(device lane × scenario)` combination. Every row begins as `PENDING`.

Record:

- lane ID and scenario ID;
- manufacturer and exact device model;
- Android version and API level;
- RAM class;
- resolution/aspect ratio and refresh rate;
- cutout/notch and gesture/navigation configuration;
- SoC/GPU notes where available;
- install and cold-launch result;
- observed average FPS and p95 frame time where measured;
- thermal result;
- touch/multitouch result;
- safe-area/readability result;
- scenario PASS/FAIL;
- evidence link or screenshot/log reference;
- tester, UTC date and notes.

A blank/PENDING row is evidence that the test still needs to happen, not a pass.

## CI behavior

### Fast PR validation

PR validation does **not** build an APK and does **not** claim device compatibility.

It validates:

- three required device lanes exist;
- API 30, 33 and 36 coverage exists;
- 12 stable scenarios exist with non-empty pass criteria;
- release gate requires at least 3 real phones and 2 OEMs;
- emulator evidence is explicitly disallowed for sign-off;
- Android API 36 is available in the Unity editor API used by the project;
- full-build code explicitly prepares the device matrix before build-size packaging;
- workflow packaging keeps one `TEST-THIS-BUILD-*` artifact.

### Post-merge Current Android Test Build

The full build:

1. runs inherited Phase 5 and Phase 6 validators;
2. prepares store assets;
3. sets/validates Android target SDK 36;
4. validates the device-matrix configuration;
5. generates the device test plan, evidence CSV and status file;
6. applies build-size/stripping optimization;
7. builds the Android test APK;
8. packages `DEVICE-MATRIX/` inside the existing single test artifact.

## Supplemental Play Console evidence

After an AAB/APK is uploaded to Play Console:

- use **Monitor and improve → Reach and devices → Device catalog** to inspect supported models and export device information such as RAM, SoC/GPU, screen, ABI and Android SDK versions;
- use the **Pre-launch report** as supplemental automated coverage for stability, compatibility, performance and accessibility.

These services improve breadth, but they do not replace the project's manual physical-device touch, thermal, safe-area and gameplay regression evidence.

## Physical sign-off rule

The final Phase 6 release-candidate gate must verify all of the following:

- at least 3 unique physical device models tested;
- at least 2 OEMs represented;
- API 30, API 33 and API 36 lanes covered;
- every required scenario completed on every required lane;
- no blocking install/launch/crash/input/save/camera/UI regression;
- baseline 30 FPS objective assessed on the representative mid-range lane;
- thermal/soak observations recorded;
- real store screenshots captured from the actual game build.

## Phase boundary

This milestone prepares and validates the device-testing system and Android 16 target readiness. It does not fabricate missing physical-device evidence.

After this PR is merged and the physical matrix is executed, the remaining Phase 6 milestone is the **final release-candidate AAB/APK, release notes, Play Console readiness checks, and soft-launch exit validation**.

**CI GREEN IS NOT DEVICE-MATRIX SIGN-OFF.**
