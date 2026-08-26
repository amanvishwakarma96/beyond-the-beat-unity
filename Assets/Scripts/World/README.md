# Phase 1 World / Zone Context

Issue #27 introduces the first persistent-world expansion for Phase 1 while keeping the Phase 0 validation scene unchanged.

## Generated Phase 1 scene

Run:

```text
Beyond The Beat > Phase 1 > Build MVP World Foundation
```

The builder regenerates the complete Phase 0 foundation first when used through CI, copies that proven scene to:

```text
Assets/Scenes/Phase1/Phase1_MVP.unity
```

and then adds a lightweight Phase 1 world layer containing:

- an Urban/Road context around the existing driving strip and parking interaction
- a separate Off-road context connected by a simple branch road
- simple urban building blocks for spatial readability
- a lightweight off-road surface and bumps for traversal testing
- reusable `ZoneContext` trigger components

The Phase 0 scene remains the Phase 0 validation baseline and is not converted into the Phase 1 scene in-place.

## `ZoneContext`

`ZoneContext` is a generic world-context trigger. It exposes:

- stable `ZoneId`
- `WorldZoneType`
- `ActorEntered`
- `ActorExited`
- `IsActorInside(actor)`
- current distinct actor count

The component resolves all colliders belonging to the same attached Rigidbody as one actor. This prevents a multi-collider vehicle from producing duplicate enter/exit events while crossing a zone boundary.

Phase 1 starts with:

| Zone ID | Type | Purpose |
| --- | --- | --- |
| `urban-road` | Urban | Existing road, parking and future first mission start context |
| `off-road` | OffRoad | Separate traversal context for the MVP open-map slice |

## Architecture boundary

`ZoneContext` does not know about missions, UI, saving, vehicle physics, survival, puzzles, or scene names. Future systems subscribe to its events or reference a configured zone instead of adding zone-specific conditionals to vehicle/world code.

Issue #28 will consume this foundation for the first data-driven Reach Location mission.

## Validation

Run:

```text
Beyond The Beat > Phase 1 > Validate MVP World Foundation
```

The validator checks that:

- the inherited drive/camera/mobile/parking loop is present
- the Phase 1 world root exists
- urban/off-road content is present
- both zone contexts have the expected IDs/types/trigger sizes
- the Phase 1 scene is enabled in Build Settings

GitHub Actions also runs the dedicated `Phase 1 World Foundation Android` workflow and produces a development APK for this milestone. That artifact validates reproducible generation/build only; it is not the final Phase 1 exit artifact.
