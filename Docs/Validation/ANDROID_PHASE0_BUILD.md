# Beyond The Beat — Phase 0 Android Build & Device Validation

This document is the execution runbook for **Issue #9 — Produce Android prototype build and performance check**.

The Android milestone is complete only when a generated APK is installed and validated on a real Android device. A successful GitHub Actions build alone is **not** a Phase 0 validation pass.

## Unity version used by CI

Phase 0 CI is pinned to:

`Unity 2022.3.62f1 (4af31df58517)`

This is intentionally kept on the standard Unity 2022.3 LTS line that works with Unity Personal. Later 2022.3 Extended/3-year LTS releases such as 2022.3.76f1 require Unity Enterprise or Unity Industry and must not be used by this Personal-license workflow.

## What the CI workflow does

Workflow:

`Phase 0 Android APK` (`.github/workflows/phase0-android-apk.yml`)

The workflow:

1. Uses the Unity version pinned in `ProjectSettings/ProjectVersion.txt`.
2. Runs `Phase0BuildAutomation.BuildAndroid`.
3. Runs the existing project bootstrap.
4. Generates the Phase 0 prototype environment.
5. Generates the vehicle, camera, mobile controls, interaction foundation, parking zone, and minimal HUD.
6. Runs every Phase 0 structural validator.
7. Builds a Unity **Development** Android APK.
8. Creates a SHA-256 checksum and artifact manifest.
9. Uploads the APK as a GitHub Actions artifact for 14 days.

The generated Unity scene/assets are CI workspace output. They are not fabricated or manually committed as Unity YAML by this workflow.

## Artifact naming

The GitHub Actions artifact is named:

`BeyondTheBeat-Phase0-<run-number>`

The APK is named:

`BeyondTheBeat-Phase0-<run-number>.apk`

The uploaded artifact also contains:

- `Phase0-Artifact-Manifest.txt`
- `SHA256SUMS.txt`

The manifest records:

- APK filename
- APK byte size
- SHA-256
- exact commit SHA
- Git ref
- GitHub Actions run URL
- Unity version
- build type
- UTC generation time

## One-time GitHub Actions setup

GameCI v5 needs repository Actions secrets for both account authentication and license activation.

### Unity Personal

Configure all three:

- `UNITY_LICENSE` — complete contents of the locally activated `Unity_lic.ulf`
- `UNITY_EMAIL` — Unity ID email
- `UNITY_PASSWORD` — direct Unity ID password

If the Unity account normally uses Google sign-in, create/reset a direct Unity ID password for that same email and store it only in GitHub Actions secrets.

### Unity serial-based paid license

Configure:

- `UNITY_SERIAL`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Do not commit license contents, account credentials, keystores, or passwords to the repository.

## Run the build

After this workflow exists on the default branch:

1. Open the repository on GitHub.
2. Open **Actions**.
3. Select **Phase 0 Android APK**.
4. Choose **Run workflow**.
5. Select the commit/ref intended for validation.
6. Run the workflow.
7. Confirm the `Build shareable Phase 0 APK` job succeeds.
8. Download the generated `BeyondTheBeat-Phase0-<run-number>` artifact.

Do not mark Issue #9 complete only because the workflow is green.

## Artifact integrity check

After downloading and extracting the artifact, verify the checksum before device testing.

Linux/macOS:

```bash
sha256sum -c SHA256SUMS.txt
```

On platforms without `sha256sum`, calculate the SHA-256 using a trusted local tool and compare it with both:

- `SHA256SUMS.txt`
- `Phase0-Artifact-Manifest.txt`

## Android installation

Enable installation from the source you are using, then install the APK.

ADB example:

```bash
adb install -r BeyondTheBeat-Phase0-<run-number>.apk
```

A successful install alone is not sufficient; continue through the full functional test.

## Required device validation

Record all results in `Docs/Validation/PHASE_0_VALIDATION.md`.

### Launch

- APK installs successfully.
- App launches without a blocking crash.
- Prototype scene is visible.
- No obvious missing-reference or broken-render-pipeline behavior is present.

### Touch driving

- Steer left while accelerating.
- Steer right while accelerating.
- Steer while braking/reversing.
- Release each control and confirm it does not remain stuck.
- Rapidly alternate steering directions.
- Confirm touch input remains responsive.

### Vehicle handling

- Accelerate through the test road.
- Brake from moderate speed.
- Reverse from a stopped state.
- Drive the slalom section.
- Check normal collision behavior.
- Watch for severe rollover, suspension oscillation, or uncontrollable steering.

### Camera

- Normal acceleration remains readable.
- Braking does not create excessive camera overshoot.
- Slalom steering does not create obvious jitter.
- Reverse remains understandable.
- Vehicle pitch/roll does not create uncomfortable camera roll.

### Parking

1. Enter the parking zone above the configured stop threshold.
2. Confirm the parking prompt is not available yet.
3. Stop at or below the threshold.
4. Confirm `ACTION / E • Park Here` becomes visible.
5. Tap **ACTION**.
6. Confirm `Parked successfully` appears.
7. Confirm repeated Action taps do not complete again during the same visit.
8. Leave the zone fully.
9. Re-enter, stop, and confirm one new parking completion is possible.

## Performance observations

Record at minimum:

- device model
- Android version
- chipset/SoC if known
- RAM if known
- screen resolution/refresh rate
- approximate FPS range
- obvious frame drops/stutter
- obvious GC-related pauses
- touch/input latency concerns
- physics/camera jitter
- thermal behavior during the test

For Phase 0, the target is usable prototype performance on a representative mid-range Android device. Final tuning is handled in Issue #10.

## Required evidence before Issue #9 closes

- [ ] GitHub Actions workflow completed successfully
- [ ] Shareable APK artifact exists
- [ ] Artifact filename follows the Phase 0 naming convention
- [ ] Artifact manifest exists
- [ ] SHA-256 exists and verifies
- [ ] Exact commit SHA is recorded
- [ ] APK installed on a real Android device
- [ ] APK launched successfully
- [ ] Touch drive loop validated
- [ ] Parking loop validated
- [ ] Performance observations recorded
- [ ] Artifact location/run URL added to `PHASE_0_VALIDATION.md`
- [ ] Blocking device issues fixed or explicitly recorded

## Hand-off to Issue #10

Issue #9 proves that the Phase 0 build is distributable and runnable on device.

Issue #10 remains responsible for:

- final vehicle-feel tuning
- final camera tuning
- re-validation after tuning
- final Phase 0 PASS / PASS WITH KNOWN ISSUES decision
- confirming the final APK exactly matches the validated commit SHA

Do not start Phase 1 until Issue #10 completes the formal Phase 0 exit gate.
