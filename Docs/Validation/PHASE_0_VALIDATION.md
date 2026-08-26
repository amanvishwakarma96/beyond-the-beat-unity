# Beyond The Beat — Phase 0 Validation Report

> Complete this report before Phase 0 is declared complete and before Phase 1 begins.

## Build Information

- Phase: Phase 0 — Prototype
- Build name: `BeyondTheBeat-Phase0-<build>.apk`
- Build/version number:
- Commit SHA:
- Unity version:
- Render pipeline: URP
- Build date:
- GitHub Actions run URL:
- Artifact name:
- Artifact location:
- APK SHA-256:
- APK size (bytes):

## Test Environment

### Unity Editor

- Operating system:
- Editor version:
- Graphics API:

### Android Device

- Device model:
- Android version:
- Chipset/SoC:
- RAM:
- GPU:
- Screen resolution/refresh rate:

## Validation Result

Choose one:

- [ ] PASS
- [ ] PASS WITH KNOWN ISSUES
- [ ] FAIL

## Functional Validation

| Test | Expected Result | Editor | Android | Notes |
| --- | --- | --- | --- | --- |
| Project opens/compiles | No compile errors | ⬜ | N/A | |
| Prototype scene loads | No missing references/errors | ⬜ | ⬜ | |
| Vehicle acceleration | Vehicle accelerates predictably | ⬜ | ⬜ | |
| Braking | Vehicle slows/stops reliably | ⬜ | ⬜ | |
| Reverse | Reverse behavior is controllable | ⬜ | ⬜ | |
| Steering | Steering is predictable at low/moderate speed | ⬜ | ⬜ | |
| Vehicle stability | No major tipping/physics instability in normal use | ⬜ | ⬜ | |
| Camera follow | Camera remains smooth/readable | ⬜ | ⬜ | |
| Touch controls | Steering + throttle/brake work with multi-touch | N/A | ⬜ | |
| Parking eligibility | Prompt appears only in valid parking context | ⬜ | ⬜ | |
| Moving vehicle rejection | Parking cannot complete above stop threshold | ⬜ | ⬜ | |
| Parking completion | Stopped vehicle completes parking successfully | ⬜ | ⬜ | |
| Interaction reset | Leaving/re-entering zone resets correctly | ⬜ | ⬜ | |
| APK install | Build installs successfully | N/A | ⬜ | |
| APK launch | Build launches without blocking error/crash | N/A | ⬜ | |
| Validation overlay | Development-build telemetry is visible and updating | N/A | ⬜ | |
| APK checksum | SHA-256 matches uploaded manifest | N/A | ⬜ | |

## Performance Observations

Use the development-build validation overlay plus tester observations. Record values after representative acceleration, braking, slalom/cornering, reversing, and parking activity rather than immediately after launch.

- Typical/approximate FPS range:
- Overlay FPS now/min/max snapshot:
- Worst observed frame time (ms):
- Stutter frames at/above 50 ms:
- GC collections G0/G1/G2 during test:
- Approx. allocated/reserved memory (MB):
- Noticeable stutter:
- Noticeable GC-related pauses:
- Input latency concerns:
- Physics/camera jitter:
- Thermal concerns during test:
- ADB/logcat evidence captured: Yes / No
- Other observations:

## Vehicle Tuning Snapshot

Record the final Phase 0 baseline values used for validation.

- Motor torque:
- Brake torque:
- Max steer angle:
- Suspension spring:
- Suspension damper:
- Wheel friction/grip notes:
- Center-of-mass adjustment:
- Camera distance:
- Camera height:
- Camera damping:

## Known Issues / Limitations

1. 
2. 
3. 

## Scope Validation

Confirm that no future-phase systems were introduced:

- [ ] No MissionManager / mission implementation
- [ ] No SaveManager / persistence implementation
- [ ] No forest/survival gameplay
- [ ] No restricted-area puzzle gameplay
- [ ] No ocean/swimming gameplay
- [ ] No economy/inventory implementation
- [ ] No backend/networking/login

## Shareable Artifact Checklist

- [ ] APK generated successfully
- [ ] APK filename includes phase/build identity
- [ ] APK checksum matches `SHA256SUMS.txt`
- [ ] `Phase0-Artifact-Manifest.txt` matches the APK and commit SHA
- [ ] APK installs on validation device
- [ ] APK launches successfully
- [ ] Development-build validation overlay is visible
- [ ] Artifact is shared outside normal Git history
- [ ] GitHub Actions run/artifact location is recorded above
- [ ] Artifact location is added to the Phase 0 issue/PR evidence
- [ ] Commit SHA in this report matches the validated build

## Final Sign-Off

- Validator:
- Date:
- Final status:
- Ready to proceed to Issue #10: Yes / No
- Notes:
