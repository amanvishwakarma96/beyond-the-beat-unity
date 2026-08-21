# Phase 0 Smooth Vehicle Camera

Issue #4 adds the lightweight gameplay follow camera used to evaluate the Phase 0 driving prototype.

## Runtime Component

`CameraFollow.cs` follows a target `Transform` and intentionally has no dependency on missions, UI, save data, mobile controls, or `VehicleController` internals.

The camera runs in `LateUpdate` so the target has already completed its normal frame/physics-driven movement before the camera applies its follow transform.

## Starting Tuning

| Setting | Default | Purpose |
| --- | ---: | --- |
| Follow Distance | 6.5 m | Distance behind vehicle heading |
| Follow Height | 3.4 m | Camera height above vehicle |
| Lateral Offset | 0 m | Optional shoulder offset |
| Look Ahead Distance | 2.0 m | Looks slightly ahead of the vehicle |
| Look At Height | 1.1 m | Raises focus point above vehicle origin |
| Position Smooth Time | 0.16 s | Position damping |
| Rotation Damping | 8 | Rotation responsiveness |
| Heading Damping | 10 | Smooths abrupt vehicle heading changes |
| Target Up Influence | 0.15 | Small allowance for vehicle pitch/roll |
| Max Position Speed | 60 m/s | Prevents pathological camera catch-up spikes |

These are prototype values. Issue #10 remains responsible for final driving/camera feel tuning.

## Editor Setup

After the prototype environment and vehicle have been generated, run:

```text
Beyond The Beat > Phase 0 > Build Smooth Vehicle Camera
```

The command:

1. Opens `Assets/Scenes/Prototype/Phase0_Prototype.unity`.
2. Finds `PrototypeVehicle`.
3. Removes the old `PrototypeReferenceCamera` and any previously generated `GameplayCamera`.
4. Creates one `GameplayCamera` tagged `MainCamera`.
5. Adds `Camera`, `AudioListener`, and `CameraFollow`.
6. Assigns `PrototypeVehicle` as the follow target.
7. Saves the scene.

Then run:

```text
Beyond The Beat > Phase 0 > Validate Smooth Vehicle Camera
```

The structural validator checks:

- Prototype vehicle exists.
- Gameplay camera exists.
- `CameraFollow` is attached.
- Target reference points to the prototype vehicle.
- Camera has the `MainCamera` tag.
- Enabled `AudioListener` exists.
- Temporary reference camera is gone.
- Exactly one enabled scene camera remains.

## Playtest Checklist

Structural validation is not enough. In Play Mode verify:

- Camera starts at a useful position without a visible first-frame jump.
- Normal acceleration does not create uncomfortable lag.
- Braking does not cause excessive camera overshoot.
- Slalom steering remains readable without obvious jitter.
- Rapid left/right steering does not make the camera snap harshly.
- Reverse remains understandable because the camera stays behind the vehicle heading instead of flipping around automatically.
- Basic bumps/vehicle pitch do not introduce excessive camera roll.
- Camera does not clip into the simple prototype vehicle under normal driving.

Obstacle/camera collision avoidance is intentionally not part of Issue #4 because the Phase 0 test environment is open and lightweight. It can be introduced later only if world geometry proves it necessary.

## Performance

The runtime implementation:

- Uses no LINQ in gameplay code.
- Does not allocate arrays/lists each frame.
- Uses cached smoothing state.
- Uses `Vector3.SmoothDamp` and quaternion/vector interpolation only.
- Has one `LateUpdate` on the active gameplay camera.

## Scope Boundary

Issue #4 does not add:

- Touch/mobile input
- Camera orbit controls
- Cinematic camera modes
- Mission camera behavior
- Camera shake
- Obstacle avoidance
- UI dependencies
- Save/persistence behavior

Those should be added only when a later milestone demonstrates a concrete need.
