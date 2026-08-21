# Phase 0 Mobile Driving Controls

This folder contains the mobile-first driving input layer for Beyond The Beat Phase 0.

## Components

### `TouchHoldButton`

A lightweight pointer-driven hold control used by the landscape HUD.

Each button tracks its own active pointer, allowing independent controls to remain pressed at the same time. This is required for combinations such as:

- steer left + accelerate
- steer right + accelerate
- steer + brake/reverse

### `MobileDrivingInput`

Reads the touch-control states and sends normalized values to `VehicleController.SetInput(steering, throttle, brake)`.

It also exposes one-shot interaction intent through:

```csharp
InteractionPressed
```

and:

```csharp
ConsumeInteractionRequest()
```

The actual interaction system is intentionally deferred to Issue #6.

## Control Layout

Landscape reference resolution: `1920 x 1080`.

Left side:

- Steer Left
- Steer Right

Right side:

- Brake / Reverse
- Accelerate
- Action

The controls use large touch areas intended for phone testing rather than final production art.

## Brake / Reverse Behavior

The Brake / Reverse control sends negative throttle.

`VehicleController` already handles direction changes safely: when the vehicle is still moving forward, a reverse request applies braking before reverse motor torque is allowed.

If Accelerate and Brake / Reverse are held simultaneously, the input router resolves the conflict to full brake.

## Editor Fallback

The same input router provides keyboard fallback so the scene has only one active component writing to `VehicleController.SetInput`.

Editor controls:

- `W` / Up: accelerate
- `S` / Down: brake then reverse
- `A` / Left: steer left
- `D` / Right: steer right
- `Space`: explicit brake
- `E`: interaction intent

The earlier `VehicleDebugInput` component is disabled in the scene when the mobile-controls builder runs.

## Build the Controls

Open the prototype project and run:

```text
Beyond The Beat > Phase 0 > Build Mobile Driving Controls
```

This creates:

- `MobileDrivingCanvas`
- Canvas Scaler for landscape mobile layout
- Graphic Raycaster
- five touch controls
- `MobileDrivingInput`
- Input System UI EventSystem/module if needed

Then run:

```text
Beyond The Beat > Phase 0 > Validate Mobile Driving Controls
```

## Required Validation

### Structural

- Prototype vehicle exists.
- HUD canvas exists and is screen-space overlay.
- `MobileDrivingInput` is attached.
- All five touch-control references are assigned.
- Input System UI module is active.
- Previous standalone debug adapter is disabled.

### Editor

- Keyboard fallback still drives the vehicle.
- `E` produces one interaction request per press.
- Holding conflicting acceleration/reverse resolves to brake.

### Android Device

- Left/right steering buttons are comfortable in landscape.
- Accelerate can be held while steering.
- Brake/reverse can be held while steering.
- Rapidly switching steering directions does not leave a stuck button state.
- Lifting a finger stops that control cleanly.
- Action button can be tapped while another driving control is held.
- No obvious input delay or missed multi-touch combinations occur.

## Scope Boundary

This milestone does not implement:

- interaction execution
- parking logic
- final HUD styling
- speedometer/minimap
- missions
- save/persistence
- haptics
- customizable control layout

Those belong to later issues/phases.
