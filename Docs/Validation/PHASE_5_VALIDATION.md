# Phase 5 Final Validation — Ocean / Exploration

Phase 5 combines the mobile ocean foundation, swim/dive physics, mobile swim input, single-camera drive/swim handoff, and the data-driven ocean Exploration mission.

## Automated exit gate

The full post-merge Android build must pass the composed Phase 5 validators before Unity is allowed to package the APK:

1. Ocean foundation
   - exactly one `Ocean` ZoneContext (`ocean`)
   - reusable `WaterVolume` depth/surface queries
   - static opaque mobile-friendly ocean surface
   - no reflection/refraction camera or per-frame wave system
2. Swim / Dive
   - Dry → Surface → Underwater → Surface → Dry lifecycle
   - Rigidbody physics in `FixedUpdate`
   - bounded dive depth and gravity restoration
3. Mobile swim + camera
   - same `MobileDrivingCanvas`
   - six swim touch controls
   - direct + legacy touch fallbacks
   - one enabled gameplay camera
   - DRIVE → SWIM TEST → DRIVE target/input ownership round trip
4. Exploration mission
   - stable checkpoints `ocean-cove`, `ocean-reef`, `ocean-wreck`
   - wrong actor and unrelated-zone rejection
   - duplicate visit rejection
   - any-order 0/3 → 3/3 completion
   - Mission HUD normalized progress
   - additive save/relaunch restoration of visited checkpoint IDs
5. Regression boundary
   - Parking
   - Cook
   - Vehicle Repair
   - Mechanic Job
   - Reach Location / Reach + Survive / Reach + Solve
   - forest survival
   - restricted gate / pressure-plate puzzle
6. Build contract
   - exactly one enabled Phase 5 scene in build settings
   - PRs run only Fast Current Milestone Validation
   - PR validation does not package an APK
   - merge/push to `main` automatically starts Current Android Test Build
   - exactly one `TEST-THIS-BUILD-<run>` artifact is intended for device testing

## Physical Android exit gate

**CI GREEN IS NOT DEVICE SIGN-OFF.**

Install only the APK from the latest `TEST-THIS-BUILD-<run>` artifact and record the device model, Android version, run number, commit SHA and observations.

### Driving baseline

- Confirm LEFT / RIGHT / GO / REV / ACTION all respond.
- Drive normally before entering swim mode.
- Check steering, throttle, reverse, interaction and camera behavior.

### Swim / camera handoff

- Tap **SWIM TEST**.
- Driving controls must hide and swim controls must appear.
- The existing gameplay camera must hand off to the swimmer without a second-camera flash or conflict.
- Verify LEFT + SWIM multitouch and RIGHT + SWIM multitouch.
- Verify BACK while swimming.
- Hold **DIVE** and confirm underwater movement/depth response.
- Use **SURFACE** and confirm upward recovery.
- If DIVE and SURFACE are pressed together, SURFACE must safely win.
- Tap DRIVE and confirm vehicle controls and camera target restore cleanly.
- Repeat DRIVE → SWIM TEST → DRIVE several times.

### Exploration mission

- Confirm the HUD begins at 0/3.
- Visit `ocean-cove` and verify progress increments once.
- Leave and re-enter `ocean-cove`; progress must not increment again.
- Visit `ocean-reef` and `ocean-wreck` in either order.
- Confirm 3/3 completes the Exploration mission exactly once and free roam remains available.

### Save / relaunch

- Start the Exploration mission and visit one or two checkpoints.
- Trigger a save, close the app fully, then relaunch.
- Verify save/relaunch restores the active Exploration mission and the previously visited checkpoint count.
- Complete the remaining checkpoint(s) and confirm mission completion still occurs once.

### Regression checks

- Parking interaction still works.
- Cook interaction still starts/cancels/completes/repeats.
- Vehicle Repair still rejects fully repaired vehicles and supports repair after re-damage.
- Mechanic Job still pays exactly one reward for the matching repair.
- Forest survival and restricted gate/puzzle flows remain usable.
- Mission HUD remains readable and does not block touch input.

### Performance / mobile acceptance

Record:

- approximate FPS while driving, at shoreline, surface swimming and underwater
- visible frame-time spikes / stutter
- obvious GC pauses
- memory-related crashes or reloads
- device thermal behavior after sustained ocean/swim play
- battery drain observations during the test session

Target remains a stable 30 FPS baseline on the chosen mid-range Android validation device; 60 FPS is a stretch target where hardware allows it.

## Exit decision

Automated validation may be marked PASS from CI. Physical Phase 5 acceptance remains PENDING until the Android checklist above is performed and evidence is recorded.
