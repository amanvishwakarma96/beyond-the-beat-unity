# Phase 0 Prototype Environment

The Phase 0 test environment is intentionally generated from an Editor tool so its geometry stays deterministic and easy to rebuild while the project is still in prototype mode.

## Generate the scene

Open the project in the pinned Unity editor version, then use:

```text
Beyond The Beat > Phase 0 > Build Prototype Environment
```

This creates and saves:

```text
Assets/Scenes/Prototype/Phase0_Prototype.unity
```

It also creates the lightweight prototype materials under `Assets/Materials` and enables the scene in Build Settings.

## Environment contents

The generated scene contains:

- `Ground_200x200` — flat 200 x 200 test surface
- `Road_TestStrip` — 12-unit-wide, 160-unit-long driving strip
- Road-edge visual markers
- Six-object slalom section for steering response
- Braking start/target markers plus a safety barrier
- Three basic collision-test obstacles
- `VehicleSpawnMarker` near the start of the road
- One directional light
- One temporary reference camera for scene inspection

The reference camera is not the gameplay camera. Issue #4 owns the real vehicle-follow camera.

## Validate the scene

Run:

```text
Beyond The Beat > Phase 0 > Validate Prototype Environment
```

Expected checks:

- Scene asset exists
- Ground is 200 x 200
- Road dimensions match the prototype specification
- Vehicle spawn marker exists
- Slalom, braking, and collision-test sections exist
- Scene is enabled in Build Settings

Commit the generated `.unity`, `.mat`, and Unity `.meta` files after validation.

## Scope boundary

This environment contains no vehicle controller, parking interaction, mission logic, save system, biome systems, economy, or networking.
