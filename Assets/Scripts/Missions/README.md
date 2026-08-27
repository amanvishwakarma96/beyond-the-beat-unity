# Mission System

The mission layer is data-driven and keeps objective rules out of vehicle, zone, interaction, survival, and UI components.

## `MissionDefinition`

Mission data is stored in ScriptableObjects with:

- stable mission ID
- display name and description
- objective type
- target `ZoneContext` ID
- optional survival duration for timed survival objectives

Supported objective types:

- `ReachLocation` — completes when the configured player enters the configured target zone.
- `ReachAndSurvive` — requires the configured player to enter the target zone and remain under the configured survival pressure for the configured continuous duration.

`ReachLocation` keeps serialized enum value `0`; `ReachAndSurvive` is additive at value `1`.

## `MissionObjectiveEvaluator`

`IsTargetZone(...)` performs the shared actor + stable zone-ID match. `IsSatisfied(...)` remains the completion evaluator for the original Reach Location objective, preserving the Phase 1 behavior unchanged.

## `MissionManager`

The manager owns mission lifecycle and objective progress:

```text
Inactive -> Active -> Completed
                   -> Failed
```

For `ReachLocation`, a matching `ZoneContext.ActorEntered` event completes the mission immediately.

For `ReachAndSurvive`:

```text
Active mission
    ↓
Target ZoneContext entered by player
    ↓
TargetContextActive = true
    ↓
ForestSurvivalController pressure active
    ↓
Timed progress accumulates
    ↓
Required duration reached -> Completed

Target exit -> progress resets to 0
Resource depleted while target active -> Failed
```

The timer runs only while all required objective state is active. It does not discover objects per frame. Zone transitions and depletion are event-driven, while `TickMission(deltaTime)` provides deterministic timed progress and is called by the manager's runtime `Update()`.

`MissionProgressChanged` is throttled for presentation updates so the HUD does not need to poll gameplay objects every frame.

Completing or failing a mission does not disable the vehicle or world; free roam remains available.

## Phase 1 sample

```text
Assets/Settings/Missions/Phase1_ReachOffRoadCheckpoint.asset
```

Build/validate with:

```text
Beyond The Beat > Phase 1 > Build Reach Location Mission
Beyond The Beat > Phase 1 > Validate Reach Location Mission
```

## Phase 2 sample

```text
Assets/Settings/Missions/Phase2_ReachAndSurviveForest.asset
```

The generated sample targets the stable `forest` ZoneContext and requires 8 seconds of continuous survival pressure.

Build/validate with:

```text
Beyond The Beat > Phase 2 > Build Reach + Survive Mission
Beyond The Beat > Phase 2 > Validate Reach + Survive Mission
```

The Phase 2 validator checks:

- ReachAndSurvive ScriptableObject configuration
- MissionManager player/zone/survival references
- original Reach Location regression behavior
- correct target / wrong-zone / wrong-actor matching
- no completion before the survival duration
- completion after the required duration
- continuous-progress reset on target exit
- depletion failure path
- HUD reach-stage and survival-progress presentation

## Architecture boundary

Mission code does not add forest-specific conditionals to `VehicleController`, `ZoneContext`, or `MissionHud`. The manager consumes reusable `ForestSurvivalController`/`SurvivalResource` state and events as an objective source; the HUD consumes only mission state/progress.

Final Phase 2 persistence of in-progress survival timing plus integrated Android/performance/device sign-off belongs to Issue #38.
