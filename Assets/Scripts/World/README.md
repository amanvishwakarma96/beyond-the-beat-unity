# World / Zone Context

The project uses one persistent world composed of reusable logical zones. `ZoneContext` exposes world context through trigger events so missions, survival, UI, and later mechanics do not need scene-name checks or biome-specific conditionals in vehicle code.

## `ZoneContext`

`ZoneContext` exposes:

- stable `ZoneId`
- `WorldZoneType`
- `ActorEntered`
- `ActorExited`
- `IsActorInside(actor)`
- current distinct actor count

The component resolves all colliders belonging to the same attached Rigidbody as one actor. This prevents a multi-collider vehicle from producing duplicate enter/exit events while crossing a zone boundary.

Supported world-zone types now are:

```text
Urban
OffRoad
Forest
```

`ZoneContext` itself remains generic. It does not know about missions, UI, saving, vehicle physics, survival, puzzles, or scene names.

## Phase 1 world

Run:

```text
Beyond The Beat > Phase 1 > Build MVP World Foundation
```

The builder produces:

```text
Assets/Scenes/Phase1/Phase1_MVP.unity
```

with the inherited Phase 0 drive/camera/mobile/parking loop plus:

| Zone ID | Type | Purpose |
| --- | --- | --- |
| `urban-road` | Urban | Road/parking and urban traversal context |
| `off-road` | OffRoad | Off-road traversal context for the MVP slice |

The integrated Phase 1 pipeline later adds the Reach Location mission, centralized local save/resume, and mission HUD to that same generated scene.

## Phase 2 forest foundation

Issue #35 extends the integrated Phase 1 MVP into a separate generated scene:

```text
Assets/Scenes/Phase2/Phase2_Forest.unity
```

Run:

```text
Beyond The Beat > Phase 2 > Build Forest Biome Foundation
```

The builder copies the integrated Phase 1 scene and adds:

- `Phase2ForestBiome/ForestZone`
- a drivable forest ground patch east of the existing off-road area
- a visible forest trail
- an off-road-to-forest connector
- 16 deterministic low-cost tree clusters
- one `ForestZoneContext`

The new logical zone is:

| Zone ID | Type | Purpose |
| --- | --- | --- |
| `forest` | Forest | Context trigger for later Phase 2 survival mechanics |

Tree canopies are visual-only; trunks use simple box colliders. Shared materials and primitive geometry keep the milestone mobile-conscious and deterministic.

### Phase 2 architecture boundary

Issue #35 adds **world context only**. It intentionally does not add:

- stamina/resource drain
- environmental survival risk
- Reach + Survive mission logic
- survival HUD
- additional persistence state

Those belong to Issues #36-#38. Later systems must subscribe to the `forest` zone context rather than add forest-specific behavior to `VehicleController` or `ZoneContext`.

## Validation

Phase 1 world validation:

```text
Beyond The Beat > Phase 1 > Validate MVP World Foundation
```

Phase 2 forest validation:

```text
Beyond The Beat > Phase 2 > Validate Forest Biome Foundation
```

The Phase 2 validator checks:

- inherited Phase 1 world/mission/save/HUD/parking roots remain present
- forest roots, ground, trail, connector, and deterministic trees exist
- exactly one stable `forest` ZoneContext exists
- it uses `WorldZoneType.Forest` with the expected trigger bounds
- the generated Phase 2 scene is enabled in Build Settings

The dedicated `Phase 2 Forest Foundation Android` workflow rebuilds and validates the Phase 1 prerequisite first, then generates/validates the forest scene and publishes a development APK with manifest/checksum traceability.

## Validation debt note

Phase 1 Issue #30 still requires physical Android exit evidence. Phase 2 development is proceeding only because the project owner explicitly instructed continuing. Repository/CI success must not be described as Phase 1 physical-device PASS.
