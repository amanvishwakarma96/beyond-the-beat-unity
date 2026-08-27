# Phase 0 Mobile Driving Controls

This folder contains the mobile-first driving input layer for Beyond The Beat Phase 0 and the lightweight mission-status HUD used by the Phase 1 MVP.

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

### `MissionHud`

Phase 1 adds a lightweight, event-driven mission panel to the existing `MobileDrivingCanvas`.

The HUD listens only to `MissionManager.MissionStateChanged`. It does not poll mission or world state in `Update`, and its panel/text graphics have raycast disabled so the overlay cannot intercept the existing touch controls.

View states:

- **Inactive:** `FREE ROAM` / `NO ACTIVE MISSION`
- **Active:** mission display name + mission description / `MISSION ACTIVE`
- **Completed:** `MISSION COMPLETE` / `COMPLETE • FREE ROAM AVAILABLE`
- **Failed:** `MISSION FAILED` / `FAILED • FREE ROAM AVAILABLE`

Mission completion deliberately leaves driving, parking, and world traversal available.

## Control Layout

Landscape reference resolution: `1920 x 1080`.

Left side:

- Steer Left
- Steer Right

Right side:

- Brake / Reverse
- Accelerate
- Action

Phase 1 mission status occupies the upper-left area and does not overlap the bottom driving controls.

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

Phase 0 controls:

```text
Beyond The Beat > Phase 0 > Build Mobile Driving Controls
Beyond The Beat > Phase 0 > Validate Mobile Driving Controls
```

Phase 1 mission HUD:

```text
Beyond The Beat > Phase 1 > Build Mission HUD
Beyond The Beat > Phase 1 > Validate Mission HUD
Beyond The Beat > Phase 1 > Validate MVP Exit Gate
```

`Phase1BuildAutomation` runs these automatically in CI after rebuilding the world, mission, and persistence slices.

## Required Validation

### Structural

- Prototype vehicle exists.
- HUD canvas exists and is screen-space overlay.
- `MobileDrivingInput` is attached.
- All five touch-control references are assigned.
- Input System UI module is active.
- Previous standalone debug adapter is disabled.
- Phase 1 Mission HUD is bound to the generated `MissionManager`.
- Mission HUD graphics do not capture raycasts.

### Editor / CI

- Keyboard fallback still drives the vehicle.
- `E` produces one interaction request per press.
- Holding conflicting acceleration/reverse resolves to brake.
- Mission HUD exposes correct inactive, active, completed, and failed/free-roam states.
- Integrated Phase 1 exit validator confirms mission completion does not disable mobile driving or parking structure.

### Android Device

- Left/right steering buttons are comfortable in landscape.
- Accelerate can be held while steering.
- Brake/reverse can be held while steering.
- Rapidly switching steering directions does not leave a stuck button state.
- Lifting a finger stops that control cleanly.
- Action button can be tapped while another driving control is held.
- Mission HUD does not intercept touch input.
- Mission completion changes the HUD and free roaming remains possible.
- No obvious input delay or missed multi-touch combinations occur.

Final device evidence is recorded in `Docs/Validation/PHASE_1_VALIDATION.md`.

## Scope Boundary

The Phase 1 HUD is intentionally minimal. It does not implement:

- production visual art
- speedometer/minimap
- mission selection menus
- cloud sync/login
- haptics
- customizable control layout

Those remain later-phase work.
