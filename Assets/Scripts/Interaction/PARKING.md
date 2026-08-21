# Phase 0 Parking Interaction

Issue #7 adds the single concrete Phase 0 interaction: park the prototype vehicle inside a designated zone, stop the vehicle, and confirm the interaction.

## Runtime flow

```text
Vehicle enters ParkHereZone
        ↓
InteractionTrigger registers ParkingZone
        ↓
ParkingZone checks VehicleController.CurrentSpeedKph
        ↓
Speed > 2 km/h        Speed <= 2 km/h
Prompt hidden         Prompt: Park Here
                           ↓
                    ACTION / E pressed
                           ↓
                ParkingZone completes
                           ↓
                 Parked successfully
                           ↓
             Completion event fires once
                           ↓
             Vehicle fully exits zone
                           ↓
                  Cycle resets
```

## Components

### `ParkingZone`

`ParkingZone` derives from `InteractableObject` and owns only parking-specific rules:

- configurable stop-speed threshold
- parking-cycle completion guard
- success message
- `UnityEvent` completion hook
- C# `ParkingCompleted` event
- reset after the actor fully exits the trigger

It does not depend on missions, saving, economy, inventory, backend, or UI presentation.

### `InteractionTrigger`

The shared trigger now exposes first-enter and final-exit actor events. The existing overlap counter still ensures the vehicle's multiple colliders behave as one actor.

`ParkingZone` uses the final-exit event to reset the completed parking cycle.

## Default tuning

| Setting | Phase 0 value |
| --- | ---: |
| Prompt | `Park Here` |
| Stop threshold | 2 km/h |
| Trigger width | 4.5 m |
| Trigger height | 2.0 m |
| Trigger length | 7.0 m |
| Success message | `Parked successfully` |
| Zone position | `(9, 1, 60)` |

The 2 km/h threshold intentionally allows a very small physics settle velocity while still requiring the player to be effectively stopped.

## Editor setup

Run:

```text
Beyond The Beat > Phase 0 > Build Parking Interaction
```

This creates:

```text
ParkingPrototype
├── ParkHereZone
│   └── ParkingSurface
└── ParkingMarkers
    ├── LeftLine
    ├── RightLine
    └── EndLine
```

Then run:

```text
Beyond The Beat > Phase 0 > Validate Parking Interaction
```

The validator checks:

- interaction-capable prototype vehicle exists
- parking prototype root exists
- `ParkingZone` exists
- `InteractionTrigger` exists
- BoxCollider is a trigger and has the expected size
- prompt is `Park Here`
- stop threshold is 2 km/h
- success feedback text is configured
- parking bay is at the expected prototype position

## Play Mode validation

1. Enter the zone above 2 km/h.
   - `Park Here` must not become eligible yet.
   - Pressing Action must not complete parking.
2. Stop inside the bay below 2 km/h.
   - `Park Here` becomes the active interaction.
3. Press Action or `E`.
   - interaction completes once
   - Console logs `Parked successfully`
   - `onParkingCompleted` / `ParkingCompleted` fires once
4. Press Action again without leaving.
   - completion must not fire again
5. Drive fully out of the zone.
   - parking cycle resets
   - prompt hides cleanly
6. Re-enter, stop, and interact again.
   - one new completion is allowed
7. Repeat with the vehicle entering/exiting at angles so multiple wheel/body colliders cross the trigger independently.
   - registration must not drop until the entire vehicle has left

## Issue #8 hand-off

Issue #7 intentionally exposes feedback as events plus the configured success message. Issue #8 owns the actual minimal HUD presentation and can subscribe to:

- `InteractionController.PromptChanged`
- `ParkingZone.ParkingCompleted`

This keeps parking gameplay independent from a particular UI implementation.
