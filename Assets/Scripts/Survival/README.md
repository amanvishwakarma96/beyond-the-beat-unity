# Phase 2 Survival Resource and Forest Pressure

Issue #36 adds the first reusable contextual gameplay mechanic on top of the Phase 2 forest `ZoneContext`.

## Runtime responsibilities

### `SurvivalResource`

Owns only the resource value and value-state events.

It provides:

- configurable maximum value
- configurable starting value
- current and normalized value
- `Drain(amount)`
- `Recover(amount)`
- reset-to-start and reset-to-max operations
- `ValueChanged`
- `Depleted`
- `Recovered`

All values are clamped to `[0, MaxValue]`.

`SurvivalResource` does not know about forests, zones, missions, UI, saving, vehicles, or scenes.

### `ForestSurvivalController`

Connects one configured `Forest` `ZoneContext`, the player actor, and one `SurvivalResource`.

It subscribes to:

- `ZoneContext.ActorEntered`
- `ZoneContext.ActorExited`

Only the configured player entering the configured forest zone activates environmental pressure.

Current milestone defaults:

- max resource: `100`
- starting resource: `100`
- forest drain: `4 / second`
- exit mode: `RecoverOverTime`
- recovery: `12 / second`

The controller stops pressure immediately on forest exit or component disable. In the configured recovery mode, resource recovery begins only after forest exit and clamps at the maximum.

## Architecture boundary

This milestone intentionally contains no mission-specific logic.

In particular:

- `ZoneContext` does not know about survival.
- `VehicleController` does not know about survival.
- `SurvivalResource` does not know about forest context.
- `ForestSurvivalController` does not complete/fail missions.
- No UI is introduced yet.
- No additional persistent save field is introduced yet because #36 does not require survival state to survive restart; final Phase 2 persistence requirements are handled by #38.

Issue #37 may consume survival state/events through the reusable public contract for Reach + Survive mission orchestration.

## Scene generation

Run:

```text
Beyond The Beat > Phase 2 > Build Forest Survival Resource
```

This adds one root to the generated Phase 2 scene:

```text
Phase2SurvivalSystem
```

containing:

- `SurvivalResource`
- `ForestSurvivalController`

The controller references the existing stable `forest` zone and `PrototypeVehicle` actor.

## Validation

Run:

```text
Beyond The Beat > Phase 2 > Validate Forest Survival Resource
```

The validator proves behavior rather than only checking object presence:

- resource/controller/forest/player references are assigned
- configured drain/recovery values are correct
- Off-road context cannot activate forest pressure
- a non-player actor cannot activate forest pressure
- forest entry drains the resource deterministically
- forest exit stops draining and begins configured recovery
- recovery clamps at the maximum
- disabling the controller stops pressure processing
- value/depleted/recovered events fire correctly

The Phase 2 CI pipeline rebuilds and validates the complete Phase 1 prerequisite, forest biome foundation, and this survival milestone before creating the Android artifact.

## Scope boundary

Still deferred:

- Reach + Survive mission orchestration (#37)
- survival HUD (#38)
- durable Phase 2 survival persistence if required by the final resume contract (#38)
- physical Android Phase 2 exit validation (#38)
