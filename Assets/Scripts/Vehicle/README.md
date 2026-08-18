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

Temporary editor/development adapter used only to drive the controller before Issue #5 provides the mobile input layer.

Controls:

- `W` / Up Arrow: throttle
- `S` / Down Arrow: reverse
- `A` / Left Arrow: steer left
- `D` / Right Arrow: steer right
- Space: brake

The debug adapter feeds `VehicleController.SetInput(...)`, which is the same normalized boundary later touch controls should use.

## Starting tuning values

| Setting | Start value | Reason |
| --- | ---: | --- |
| Vehicle mass | 1250 kg | Mid-size passenger vehicle baseline |
| Motor torque | 1800 Nm total axle request | Strong enough for a responsive prototype without targeting arcade acceleration |
| Brake torque | 3500 Nm | Deliberately stronger than drive torque for controllable stopping tests |
| Max forward speed | 110 km/h | Enough room to evaluate low/moderate/high-speed steering on the Phase 0 strip |
| Max reverse speed | 25 km/h | Keeps reverse controllable on touch-oriented gameplay |
| Max steer angle | 32 degrees | Useful low-speed maneuverability |
| High-speed steering multiplier | 0.45 | Reduces twitchiness as speed rises |
| Wheel radius | 0.34 m | Typical passenger-car scale |
| Wheel mass | 28 kg | Within Unity's documented typical WheelCollider range |
| Suspension distance | 0.22 m | Short road-car suspension travel |
| Suspension spring | 35000 | Firm starting point for a 1250 kg prototype |
| Suspension damper | 4500 | Dampens repeated bouncing while retaining visible suspension response |
| Center of mass offset | Y = -0.45 m | Lowers rollover tendency during initial handling tests |
| Forward friction stiffness | 1.35 | Moderate longitudinal grip |
| Sideways friction stiffness | 1.55 | Slightly stronger lateral grip for predictable steering |
| Downforce coefficient | 18 | Mild speed-dependent stability aid, not an aerodynamic simulation |

These values are starting points only. Issue #10 owns the final Phase 0 tuning pass.

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
- Rigidbody is present with the expected starting mass
- `VehicleController` is attached
- debug input adapter is attached
- exactly four WheelColliders exist
- all controller wheel/visual references are assigned
- the prototype vehicle prefab exists

## Playtest checklist for Issue #3

- [ ] Accelerates from rest without severe wheel hop.
- [ ] Steers left/right predictably at low speed.
- [ ] Steering becomes less sensitive as speed increases.
- [ ] Space brake stops the vehicle reliably.
- [ ] Pressing reverse while still moving forward brakes before applying reverse torque.
- [ ] Pressing forward while still reversing brakes before applying forward torque.
- [ ] Reverse speed remains controllable.
- [ ] Vehicle does not roll over during normal slalom testing.
- [ ] Vehicle remains stable across the simple test obstacles.
- [ ] Visual wheels track WheelCollider position/rotation.
- [ ] No repeated managed allocations are introduced by the controller's `FixedUpdate` loop.

## Phase boundary

Do not add the following under Issue #3:

- gameplay follow camera
- touch/mobile driving controls
- parking interaction
- vehicle damage/fuel
- missions
- persistence
- production biome behavior
