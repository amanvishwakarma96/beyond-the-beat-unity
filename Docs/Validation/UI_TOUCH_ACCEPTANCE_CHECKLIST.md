# Android UI + Touch Acceptance Checklist

Use this checklist with the latest `TEST-THIS-BUILD-<run>` artifact only.

## Controls

- [ ] Steer Left responds while stationary.
- [ ] Steer Right responds while stationary.
- [ ] GO accelerates the vehicle.
- [ ] BRAKE/REV brakes/reverses as intended.
- [ ] GO + Steer Left works simultaneously.
- [ ] GO + Steer Right works simultaneously.
- [ ] BRAKE/REV + steering works simultaneously.
- [ ] ACTION works while another control is held.
- [ ] Sliding/releasing a finger never leaves a button stuck.
- [ ] Press feedback visibly changes while a control is held.

## HUD

- [ ] Speed panel is readable without blocking the road view.
- [ ] Mission card is readable in landscape.
- [ ] Interaction prompt is compact and readable.
- [ ] Reach + Survive progress is visible while surviving.
- [ ] HUD elements do not block any driving input area.

## Presentation

- [ ] Sky/lighting/fog feel authored rather than Unity-default.
- [ ] Main road has visible lane/edge treatment.
- [ ] Zone signage is visible and useful.
- [ ] Urban/off-road/forest areas read as distinct spaces.
- [ ] No obvious debug/test labels dominate the gameplay view.

## Artifact clarity

- [ ] Only one current installable APK is presented for the Phase 2 PR.
- [ ] Artifact name begins with `TEST-THIS-BUILD-`.
- [ ] APK name begins with `BeyondTheBeat-TEST-`.
