# Phase 6 — Android Build Size / Stripping Validation

## Purpose

This milestone reduces Android package overhead without changing gameplay runtime behavior.

The repository asset audit at the start of this slice found no production texture/audio payload under `Assets/Art` or `Assets/Audio` (only placeholder files). Because there is no meaningful source-media payload to optimize yet, this milestone deliberately does **not** add blanket texture/audio importer overrides.

## Build policy

The generated `Phase6_MobileBuildOptimization` profile currently requires:

- Unity engine code stripping: **enabled**
- Android managed stripping: **Low**
- Build data compression: **LZ4HC**
- Android architecture selection: **preserve the existing project value**
- BuildReport largest-file entries: **15**

`Low` managed stripping is intentional for this first pass. The project contains event wiring, serialized fields and generic Unity component discovery patterns; moving directly to Medium/High before physical launch/regression validation would add avoidable runtime-stripping risk.

## Why ABI/backend are unchanged

This milestone does not force ARM64-only, switch Mono/IL2CPP, or otherwise alter the scripting backend/ABI mask. Those can materially affect package size, but they can also change build time, native plugin compatibility and the set of Android devices that can install the test package.

A later release-candidate milestone may tighten ABI/backend policy after the device matrix is established.

## Automated PR validation

The fast PR gate verifies without packaging an APK:

- build optimization profile is valid;
- engine stripping policy is enabled;
- managed stripping policy is explicitly Low;
- architecture-preservation policy is enabled;
- LZ4HC composes with the existing Development build options;
- the BuildReport diagnostics implementation compiles;
- prior Phase 6 performance/render-quality contracts remain intact.

## Post-merge Android build

The automatic `Current Android Test Build` now runs:

1. Phase 5 gameplay generation/regression
2. Phase 6 performance instrumentation
3. Phase 6 mobile render-quality optimization
4. Phase 6 build-size/stripping preparation
5. Android `BuildPipeline.BuildPlayer`
6. Unity BuildReport size diagnostics
7. existing 200 MB APK ceiling
8. one `TEST-THIS-BUILD-<run>` artifact

The build writes `build/phase6-build-size-report.txt` containing:

- Unity BuildResult
- total BuildReport bytes / MiB
- build duration
- engine stripping state
- managed stripping level
- current Android architecture mask
- LZ4HC state
- largest generated build files

The report is included with CI diagnostics and the successful device-test artifact.

## Physical Android regression gate

Stripping can fail only at runtime even when compilation and packaging succeed. On the generated APK verify:

- application installs and launches normally;
- startup scene has no missing-script/type initialization errors;
- LEFT / RIGHT / GO / REV / ACTION still respond;
- DRIVE -> SWIM TEST -> DRIVE round trip still works;
- DIVE / SURFACE and swim controls still work;
- Exploration mission progresses 0/3 -> 3/3;
- save/relaunch restores mission progress;
- Parking, Cook, Repair and Mechanic Job still run;
- Forest survival and restricted puzzle/gate still run;
- performance overlay still samples in Development builds;
- FPS/frame-time does not regress due to compression/loading changes;
- startup/load time is compared with the prior test build;
- APK size is recorded and compared with the previous Phase 6 build.

## Asset compression status

**No texture/audio importer compression change is applied in this milestone.**

Reason: `Assets/Art` and `Assets/Audio` contain no production payload yet. Applying broad rules now would create policy without measurable benefit and could silently degrade future assets.

When real source media is introduced, importer optimization should be scoped by asset class, for example:

- environment/albedo max texture sizes;
- normal-map compression;
- UI sprite exceptions;
- music streaming vs SFX compressed-in-memory;
- Android ASTC/ETC2 choices based on target device matrix.

## Acceptance boundary

Automated PASS proves the packaging configuration and diagnostics are deterministic.

It does **not** prove runtime stripping safety.

**CI GREEN IS NOT DEVICE BUILD-SIZE SIGN-OFF.**

Physical install/launch/regression evidence is required before this optimization is considered accepted.
