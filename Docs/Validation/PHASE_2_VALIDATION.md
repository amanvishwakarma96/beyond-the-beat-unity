# Phase 2 Validation — Forest Survival Vertical Slice

## Current status

**REPOSITORY VALIDATION IN PROGRESS / PHYSICAL ANDROID VALIDATION REQUIRED**

Phase 2 is not signed off by CI alone. The repository pipeline can prove deterministic scene generation, structural integration, save-model round trips, mission/survival behavior in editor validation, Android compilation/build, and artifact traceability. A physical Android device is still required for final interaction, presentation, performance, pause/resume, and install/launch acceptance.

## Integrated scope

- Phase 1 urban/off-road world, driving, parking, free roam, Reach Location mission, HUD, and local save/resume remain prerequisites.
- Forest biome and reusable `forest` `ZoneContext`.
- Reusable survival resource with forest pressure, depletion, and configured exit recovery.
- Data-driven Reach + Survive mission using the shared `MissionManager`.
- Mission HUD Reach + Survive progress plus live survival-resource percentage.
- Centralized save/resume of vehicle transform, mission state, Reach + Survive elapsed progress, target-context state, survival resource value, pressure state, and recovery state.
- Additive Phase 2 save fields remain backward-compatible with existing Phase 1 version-1 JSON saves through `HasPhase2SurvivalState=false` when the fields are absent.
- Current Android test artifact retains manifest, source SHA, and SHA-256 traceability.

## Repository/CI validation checklist

| Check | Expected result |
| --- | --- |
| Phase 1 prerequisite regeneration/validation | PASS in Phase 2 build pipeline |
| Forest biome/context generation | PASS |
| Survival-resource structural/behavior validation | PASS |
| Reach Location regression | PASS |
| Reach + Survive target/wrong-zone/wrong-actor evaluation | PASS |
| Reach + Survive continuous timed completion | PASS |
| Target exit resets continuous survival progress | PASS |
| Resource depletion fails active survival objective | PASS |
| Authored mobile HUD/presentation validation | PASS |
| Phase 2 persistence/survival wiring | PASS |
| Phase 2 save JSON round trip | PASS |
| Existing Phase 1 save JSON remains readable | PASS |
| Active Reach + Survive elapsed/resource restore | PASS |
| Mission HUD live resource status | PASS |
| Android development APK build | PASS required before review/merge |
| Artifact manifest + checksum | PASS required before device test |

## Physical Android acceptance checklist

Record the exact artifact/run, source commit, APK checksum, device model, Android version, and results below. Do not mark Phase 2 PASS without this evidence.

### Install / launch

- [ ] Install the single `TEST-THIS-BUILD-<run>` artifact APK.
- [ ] Cold launch succeeds without crash or blocking dialog.
- [ ] Current test build can be identified from its artifact/run/source SHA.

### Driving and Phase 1 regression

- [ ] Left/right steering responds to touch.
- [ ] GO + steering works simultaneously.
- [ ] BRAKE/REV + steering works simultaneously.
- [ ] ACTION remains usable while another control is held.
- [ ] Releasing a touch never leaves a control stuck.
- [ ] Parking interaction still works before and after mission play.
- [ ] Free roam remains available before, during, and after mission completion/failure.
- [ ] Existing Reach Location objective still completes only at its configured target.

### Forest / survival behavior

- [ ] Driving into the forest activates environmental survival pressure.
- [ ] Live resource percentage is readable and decreases while pressure is active.
- [ ] Leaving the forest stops pressure and applies configured recovery behavior.
- [ ] Re-entering the forest reactivates pressure correctly.
- [ ] Resource reaches its expected clamp/depletion behavior without negative values.

### Reach + Survive mission

- [ ] Mission first instructs the player to reach the forest target.
- [ ] Survival timer/progress does not advance before the configured target is active.
- [ ] Survival timer/progress advances while valid forest pressure is active.
- [ ] Leaving the target before completion resets continuous survival progress.
- [ ] Remaining in the target for the required duration completes the mission.
- [ ] Depletion before completion fails the mission.
- [ ] Free roam remains functional after completion/failure.

### Save / resume

- [ ] Pause/background while outside forest, resume, and confirm vehicle/mission state remains correct.
- [ ] Pause/background during active Reach + Survive and record elapsed progress/resource value before pause.
- [ ] Resume and confirm elapsed progress/resource value restore without duplication/reset.
- [ ] Completed mission resumes as completed and does not restart unexpectedly.
- [ ] Corrupt/missing/incompatible save still falls back safely to new-game state.

### Performance / presentation

- [ ] Forest traversal remains responsive on the target Android device.
- [ ] Record average FPS, worst observed frame time/FPS, stutter count/notes, GC observations, and memory overlay values.
- [ ] HUD remains readable without blocking controls.
- [ ] Forest/road/zone presentation is acceptable on the physical screen.
- [ ] No new repeated exceptions/errors are observed during a full loop.

## Device evidence

- Device model: **Pending**
- Android version: **Pending**
- Artifact/run: **Pending**
- Source commit: **Pending**
- APK SHA-256: **Pending**
- Install/launch: **Pending**
- Touch/multitouch: **Pending**
- Forest survival: **Pending**
- Reach Location regression: **Pending**
- Reach + Survive: **Pending**
- Save/resume: **Pending**
- Parking/free roam: **Pending**
- Performance: **Pending**
- Known issues: **Pending**

## Exit decision

**Final Phase 2 result: NOT YET SIGNED OFF**

**Ready for Phase 3: No** until the exact candidate APK has green repository CI plus recorded physical Android acceptance evidence. A final result may be changed to **PASS** or **PASS WITH ACCEPTED KNOWN ISSUES** only after that evidence is committed or recorded on the milestone issue.
