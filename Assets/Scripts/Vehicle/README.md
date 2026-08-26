# Phase 0 Vehicle Physics

This folder contains the Phase 0 vehicle-physics foundation. The goal is responsive, predictable mobile driving rather than final simulation realism.

## Runtime components

### `VehicleController`

Owns vehicle movement/physics only:

- normalized steering input: `-1 .. 1`
- normalized throttle input: `-1 .. 1`
- normalized brake input: `0 .. 1`
- front-wheel steering
- rear-wheel drive
- forward/reverse speed limits
- direction-change braking
- Rigidbody/chassis tuning
- WheelCollider suspension and tire stiffness
- visual wheel pose synchronization

It intentionally contains no UI, missions, fuel, damage, save, economy, biome, or networking logic.

### `VehicleDebugInput`

Temporary editor/development adapter used only to drive the controller before the mobile input layer is active.

Controls:

- `W` / Up Arrow: throttle
- `S` / Down Arrow: reverse
- `A` / Left Arrow: steer left
- `D` / Right Arrow: steer right
- Space: brake

The debug adapter feeds `VehicleController.SetInput(...)`, which is the same normalized boundary used by touch controls.

## Phase 0 candidate final tuning

Issue #10 promotes the original starting values into a candidate final baseline for Android validation. These values are intentionally conservative and remain subject to real-device confirmation before Phase 0 can pass its exit gate.

| Setting | Candidate value | Change / intent |
| --- | ---: | --- |
| Vehicle mass | 1250 kg | Retained passenger-vehicle baseline |
| Motor torque | 1700 Nm total axle request | Slightly calmer launch response for digital touch input |
| Brake torque | 3800 Nm | More stopping margin before parking and direction changes |
| Direction-change brake torque | 2600 Nm | Stronger transition braking before reverse/forward torque swaps |
| Max forward speed | 110 km/h | Retained validation range |
| Max reverse speed | 25 km/h | Retained touch-friendly reverse cap |
| Max steer angle | 30 degrees | Reduces low-speed twitchiness while preserving maneuverability |
| Steering response | 6 | Slightly softer left/right direction changes |
| Steering reduction range | 5–50 km/h | Full steer is retained only at very low speed; reduction is explicit and bounded |
| High-speed steering multiplier | 0.38 | Lower high-speed steering authority for stability |
| Suspension spring | 35000 | Retained firm road-car spring baseline |
| Suspension damper | 5000 | Increased damping to reduce repeated oscillation |
| Center of mass offset | Y = -0.50 m | Slightly lower rollover tendency |
| Forward friction stiffness | 1.40 | Small increase in longitudinal grip |
| Sideways friction stiffness | 1.60 | Small increase in lateral stability |
| Downforce coefficient | 20 | Slightly stronger speed-dependent stability aid |

The vehicle validator now checks these candidate values so CI cannot silently build a different Phase 0 tuning setup.

## Unity Editor setup

After the Phase 0 environment exists, run:

```text
Beyond The Beat > Phase 0 > Build Prototype Vehicle
```

This generates/adds:

```text
Assets/Prefabs/Vehicles/PrototypeVehicle.prefab
Assets/Materials/Prototype_Vehicle.mat
Assets/Materials/Prototype_Wheel.mat
```

and places the vehicle at `VehicleSpawnMarker` inside:

```text
Assets/Scenes/Prototype/Phase0_Prototype.unity
```

Then run:

```text
Beyond The Beat > Phase 0 > Validate Prototype Vehicle
```

Expected checks:

- prototype vehicle exists in the scene
- Rigidbody is present with the expected mass
- `VehicleController` is attached
- debug input adapter is attached
- exactly four WheelColliders exist
- all controller wheel/visual references are assigned
- candidate final tuning values match the Issue #10 baseline
- the prototype vehicle prefab exists

## Final Phase 0 playtest checklist

- [ ] Accelerates from rest without severe wheel hop or an uncomfortable launch spike.
- [ ] Steers left/right predictably at walking/parking speed.
- [ ] Steering remains controllable at moderate speed.
- [ ] Steering authority reduces progressively between 5 and 50 km/h.
- [ ] Brake input stops the vehicle reliably.
- [ ] Pressing reverse while moving forward brakes before reverse torque is applied.
- [ ] Pressing forward while reversing brakes before forward torque is applied.
- [ ] Reverse speed remains controllable.
- [ ] Vehicle does not roll over during normal slalom testing.
- [ ] Vehicle remains stable across the simple test obstacles.
- [ ] Visual wheels track WheelCollider position/rotation.
- [ ] No repeated managed allocations are introduced by the controller's `FixedUpdate` loop.

Real-device results belong in `Docs/Validation/PHASE_0_VALIDATION.md`. The candidate values are not considered final until that report records a passing Android test.

## Phase boundary

Do not add the following during Phase 0 tuning:

- vehicle damage/fuel
- missions
- persistence
- production biome behavior
- economy/inventory
- networking/backend
