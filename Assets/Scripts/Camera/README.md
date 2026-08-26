# Phase 0 Smooth Vehicle Camera

The Phase 0 gameplay camera is intentionally lightweight and independent from missions, UI, save data, or vehicle internals.

## Runtime Component

`CameraFollow.cs` follows a target `Transform` in `LateUpdate`, after normal vehicle movement has completed for the frame.

## Phase 0 candidate final tuning

Issue #10 uses the following candidate baseline for final Android validation:

| Setting | Candidate | Intent |
| --- | ---: | --- |
| Follow Distance | 6.8 m | Slightly more road context around the vehicle |
| Follow Height | 3.6 m | Improved forward visibility on mobile screens |
| Lateral Offset | 0 m | Retains centered driving view |
| Look Ahead Distance | 2.2 m | Slightly stronger forward framing |
| Look At Height | 1.15 m | Keeps the focus point above the chassis origin |
| Position Smooth Time | 0.18 s | Slightly gentler positional damping |
| Rotation Damping | 7.5 | Slightly softer rotation response |
| Heading Damping | 9 | Reduces harsh left/right heading changes |
| Target Up Influence | 0.12 | Reduces camera roll contribution from chassis pitch/roll |
| Max Position Speed | 60 m/s | Retained catch-up limit |

The structural camera validator checks this baseline so CI does not silently produce a differently tuned Phase 0 build.

## Editor Setup

After the prototype environment and vehicle have been generated, run:

```text
Beyond The Beat > Phase 0 > Build Smooth Vehicle Camera
```

Then run:

```text
Beyond The Beat > Phase 0 > Validate Smooth Vehicle Camera
```

The validator checks:

- Prototype vehicle exists.
- Gameplay camera exists.
- `CameraFollow` is attached.
- Target reference points to the prototype vehicle.
- Candidate Issue #10 camera tuning matches.
- Camera has the `MainCamera` tag.
- Enabled `AudioListener` exists.
- Temporary reference camera is gone.
- Exactly one enabled scene camera remains.

## Final Phase 0 playtest checklist

- [ ] Camera starts at a useful position without a visible first-frame jump.
- [ ] Normal acceleration does not create uncomfortable lag.
- [ ] Braking does not cause excessive overshoot.
- [ ] Slalom steering remains readable without obvious jitter.
- [ ] Rapid left/right steering does not make the camera snap harshly.
- [ ] Reverse remains understandable without an automatic camera flip.
- [ ] Vehicle pitch/roll does not create uncomfortable camera roll.
- [ ] Camera does not clip into the prototype vehicle during normal driving.

Real-device results belong in `Docs/Validation/PHASE_0_VALIDATION.md`. These values remain a candidate baseline until Android validation passes.

## Performance

The runtime implementation:

- Uses no LINQ in gameplay code.
- Does not allocate arrays/lists each frame.
- Uses cached smoothing state.
- Uses `Vector3.SmoothDamp` and quaternion/vector interpolation only.
- Has one `LateUpdate` on the active gameplay camera.

## Scope Boundary

Phase 0 does not add camera orbit controls, cinematic modes, mission cameras, camera shake, persistence, or obstacle avoidance unless a concrete blocker is found during validation.
