# Phase 1 Mission Foundation

Issue #28 introduces the first data-driven mission flow for the Phase 1 MVP.

## Runtime responsibilities

### `MissionDefinition`

A ScriptableObject containing mission data only:

- stable mission ID
- display name and description
- objective type
- target zone ID

The first supported objective type is `ReachLocation`.

### `MissionObjectiveEvaluator`

Evaluates whether a reusable world event satisfies the configured mission objective. For Reach Location it requires:

- the event actor is the configured player actor
- the entered `ZoneContext.ZoneId` matches the mission's target zone ID

This keeps mission-specific conditionals out of `ZoneContext`, the vehicle controller, camera, and interaction systems.

### `MissionManager`

Owns the mission lifecycle:

```text
Inactive -> Active -> Completed
                   -> Failed
```

It subscribes to configured `ZoneContext.ActorEntered` events. There is no per-frame mission polling.

Completing or failing a mission does not disable the vehicle or world. Free roam therefore remains available with no active mission and after completion.

## Generated sample mission

Run:

```text
Beyond The Beat > Phase 1 > Build Reach Location Mission
```

The builder creates/updates:

```text
Assets/Settings/Missions/Phase1_ReachOffRoadCheckpoint.asset
```

and adds `Phase1MissionSystem` to:

```text
Assets/Scenes/Phase1/Phase1_MVP.unity
```

The sample mission starts automatically in Play Mode and targets the dedicated off-road checkpoint zone:

```text
phase1-offroad-checkpoint
```

The target is represented by a visible cyan marker inside the off-road area. Entering the dedicated checkpoint with `PrototypeVehicle` completes the active mission and leaves driving/free roam enabled.

## Validation

Run:

```text
Beyond The Beat > Phase 1 > Validate Reach Location Mission
```

The validator checks:

- ScriptableObject mission configuration
- `MissionManager` mission/player/zone references
- dedicated target `ZoneContext`
- visible target marker
- correct target-zone objective match
- rejection of the broad off-road zone as the mission target
- rejection of a non-player actor
- start and clear lifecycle transitions

The Phase 1 Android workflow rebuilds and validates both Issue #27 world context and Issue #28 mission configuration before producing the milestone APK.

## Scope boundary

Issue #28 intentionally does not add:

- local save/resume (`SaveManager`) — Issue #29
- final mission HUD/status presentation — Issue #30
- rewards/economy
- inventory
- survival or puzzle mechanics
- backend/login/networking

Those systems should consume mission events/state rather than moving their responsibilities into `MissionManager`.
