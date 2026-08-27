# Beyond The Beat — Phase 1 MVP Validation

Status: **REPOSITORY VALIDATION IN PROGRESS / PHYSICAL ANDROID VALIDATION REQUIRED**

Issue: #30  
Scene: `Assets/Scenes/Phase1/Phase1_MVP.unity`  
Application ID: `com.beyondthebeat.mvp`  
Build type: Unity Development APK

## Gate rule

Phase 1 is complete only when all repository-side generation/validation/build checks pass **and** the exact candidate APK is validated on a physical Android device.

GitHub Actions success alone does **not** produce a Phase 1 PASS. Phase 2 should start only after this document records either:

- `PASS`, or
- `PASS WITH ACCEPTED KNOWN ISSUES`.

## Candidate identity

Fill these values from the successful `Phase 1 MVP Exit Android` workflow artifact manifest.

| Field | Value |
| --- | --- |
| Pull request | TBD |
| Source commit SHA | TBD |
| Merge commit SHA | TBD until merge |
| Workflow run | TBD |
| Artifact name | `BeyondTheBeat-Phase1-MVP-<run>` |
| APK file | `BeyondTheBeat-Phase1-MVP-<run>.apk` |
| APK size | TBD |
| SHA-256 | TBD |
| Unity version | From `ProjectSettings/ProjectVersion.txt` / manifest |
| Checksum verified | Yes / No |

## Repository-side validation

The build pipeline regenerates Phase 0 prerequisites, derives the Phase 1 scene, then generates and validates world, mission, persistence, HUD, and the integrated exit gate before building the APK.

| Check | Expected | Result |
| --- | --- | --- |
| Phase 0 prerequisite generation | PASS | CI pending |
| Phase 1 world / Urban + Off-road zones | PASS | CI pending |
| Reach Location mission definition and target zone | PASS | CI pending |
| Event-driven mission objective evaluation | PASS | CI pending |
| Centralized local save/resume | PASS | CI pending |
| Mission HUD structure and source references | PASS | CI pending |
| HUD active/completed/free-roam states | PASS | CI pending |
| Mission start → target zone → completion lifecycle | PASS | CI pending |
| Free roam before and after mission | PASS | CI pending |
| Mobile driving regression structure | PASS | CI pending |
| Parking/interaction regression structure | PASS | CI pending |
| Integrated mission save/resume round-trip | PASS | CI pending |
| Android Development APK build | PASS | CI pending |
| Artifact manifest + SHA-256 | PASS | CI pending |

## Physical Android device

| Field | Value |
| --- | --- |
| Device model | TBD |
| Android version | TBD |
| SoC / CPU | TBD |
| RAM | TBD |
| GPU | TBD |
| Display resolution / refresh rate | TBD |
| Battery / thermal state at start | TBD |
| Tester | TBD |
| Test date | TBD |

## Test A — Install and launch

1. Install the exact APK listed in Candidate identity.
2. Launch from the Android launcher.
3. Confirm the Phase 1 scene loads without crash, black screen, blocking dialog, or missing UI.
4. Confirm the development validation overlay is visible/available for performance observations.
5. Confirm the mission HUD is visible without covering or blocking driving controls.

Result: **TBD — PASS / FAIL**

Notes:

- TBD

## Test B — Mobile driving and free-roam baseline

1. Before completing the mission, verify steering left/right responds to touch.
2. Verify GAS accelerates the vehicle.
3. Verify BRAKE/REV brakes and permits reverse using the existing direction-change behavior.
4. Verify ACTION remains usable.
5. Drive through the Urban/Road area and into the Off-road connector.
6. Confirm the mission HUD does not intercept any driving-control touch region.
7. Confirm there are no blocking vehicle/camera/input regressions.

Result: **TBD — PASS / FAIL**

Notes:

- TBD

## Test C — Reach Location mission and HUD

Expected mission: `phase1-reach-offroad-checkpoint`  
Expected target zone: `phase1-offroad-checkpoint`

1. Launch with a fresh/new-game state.
2. Confirm the HUD presents **Reach the Off-road Checkpoint** as an active mission.
3. Drive into the broad off-road area but stay outside the marked checkpoint; the mission must remain active.
4. Enter the marked checkpoint.
5. Confirm the mission completes once.
6. Confirm the HUD changes to **MISSION COMPLETE** and indicates free roam is available.
7. Continue driving after completion; vehicle controls and world traversal must remain available.

Result: **TBD — PASS / FAIL**

Notes:

- TBD

## Test D — Save and resume active mission

1. Reset progress/new-game state.
2. Start driving toward the mission target but do not complete it.
3. Stop at a clearly different world position.
4. Background the app long enough to trigger application pause/save.
5. Force-stop the app from Android settings or remove it from recents after the save has occurred.
6. Relaunch the app.
7. Confirm the vehicle resumes at the saved world position/orientation, at rest.
8. Confirm the same mission is restored as **Active**.
9. Continue to the checkpoint and complete the mission.

Result: **TBD — PASS / FAIL**

Notes:

- TBD

## Test E — Save and resume completed mission

1. After mission completion, background the app to save.
2. Force-stop/close and relaunch.
3. Confirm the completed mission state is restored.
4. Confirm the HUD presents the completed/free-roam state rather than restarting the mission.
5. Confirm the vehicle can continue free roaming.

Result: **TBD — PASS / FAIL**

Notes:

- TBD

## Test F — Parking regression

1. Drive to the existing parking interaction.
2. Enter the parking zone.
3. Stop the vehicle below the configured threshold (`2 km/h`).
4. Confirm the interaction prompt appears.
5. Use ACTION.
6. Confirm `Parked successfully` feedback appears.
7. Leave/re-enter and confirm the interaction can reset for another visit.
8. Repeat once after the Reach Location mission has completed to confirm parking remains independent of mission state.

Result: **TBD — PASS / FAIL**

Notes:

- TBD

## Test G — Performance / stability observation

Use the Development-build validation overlay during a representative session that includes Urban driving, Off-road driving, mission completion, parking, and resume.

Record:

| Metric | Observation |
| --- | --- |
| Session duration | TBD |
| FPS typical | TBD |
| FPS minimum observed | TBD |
| Worst frame | TBD |
| Stutter count / visible hitches | TBD |
| GC observations | TBD |
| Allocated memory | TBD |
| Reserved memory | TBD |
| Thermal behavior | TBD |
| Camera jitter | TBD |
| Input latency / missed touches | TBD |
| Crash / ANR | None / details |

Performance result: **TBD — PASS / FAIL / PASS WITH KNOWN ISSUE**

## Known issues

Record only observed issues from the exact candidate build.

- TBD

## Final Phase 1 sign-off

Repository-side CI/build: **TBD — PASS / FAIL**  
Physical Android install/launch: **TBD — PASS / FAIL**  
Mobile driving: **TBD — PASS / FAIL**  
Reach Location mission: **TBD — PASS / FAIL**  
Mission HUD: **TBD — PASS / FAIL**  
Save/resume: **TBD — PASS / FAIL**  
Parking regression: **TBD — PASS / FAIL**  
Free roam after completion: **TBD — PASS / FAIL**  
Performance/stability: **TBD — PASS / FAIL / ACCEPTED KNOWN ISSUE**

**Final result: NOT YET SIGNED OFF**

Ready to proceed to Phase 2: **No**

Sign-off tester: TBD  
Sign-off date: TBD
