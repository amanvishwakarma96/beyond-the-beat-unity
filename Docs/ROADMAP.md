# Beyond The Beat — Development Roadmap

This roadmap is intentionally phased. Each phase must produce a playable result before the project advances.

## Phase 0 — Prototype

**Goal:** Prove the core drive-and-interact loop feels satisfying.

Scope:
- Small flat test map, roughly 200 x 200 units
- One road strip and a few obstacles
- One drivable vehicle
- Tunable vehicle physics
- Smooth vehicle-follow camera
- One parking interaction prototype
- Minimal prompt UI
- Mobile-friendly steering/throttle/brake input

Exit criteria:
- Vehicle handling feels satisfying enough to build the rest of the game on top of it.
- Parking interaction works reliably.
- Prototype is playable on target mobile hardware.

Explicitly excluded:
- Missions
- Save system
- Biomes
- Economy
- Survival
- Ocean gameplay
- Networking/backend

## Phase 1 — MVP Core Loop

**Goal:** Produce the first end-to-end playable vertical slice.

Scope:
- Small open map with one urban and one off-road zone
- Reach Location mission type
- MissionManager foundation
- Local save system
- Free roam with driving and parking

Exit criteria:
- Player can launch the game, receive/select a mission, drive to the objective, complete it, and continue free-roaming.

## Phase 2 — Forest Biome + Survival Trigger

**Goal:** Add the first contextual biome mechanic.

Scope:
- Forest zone
- Lightweight stamina/resource meter
- Basic environmental risk
- Reach + Survive mission type

Exit criteria:
- Entering/leaving the forest correctly changes gameplay context.
- Two mission types work through shared systems.

## Phase 3 — Restricted Area Puzzle Gate

**Goal:** Add reusable mission gating.

Scope:
- One physics-based puzzle pattern
- Gate/door unlock behavior
- Reach + Solve mission type

Exit criteria:
- Puzzle reliably blocks and unlocks access.
- Three mission types work without mission-specific architectural duplication.

## Phase 4 — Free Roam Expansion

**Goal:** Expand non-mission activities through the shared interaction system.

Scope:
- Cook
- Repair
- Mechanic job
- Existing parking interaction retained

Exit criteria:
- Four free-roam activities share one reusable interaction foundation.

## Phase 5 — Ocean / Exploration

**Goal:** Add water exploration only after the earlier systems prove engaging.

Scope:
- Swim/dive controller
- Lightweight water rendering suitable for URP/mobile
- Exploration mission type

Exit criteria:
- Water zone is playable and performant.
- Mission variety reaches four types.

## Phase 6 — Polish & Soft Launch Preparation

**Goal:** Stabilize, optimize, and prepare for store testing.

Scope:
- Performance optimization
- Tutorial/onboarding
- UI/UX polish
- Store icon/assets
- Build-size optimization
- Device testing

Exit criteria:
- Stable 30–60 FPS on a representative mid-range Android device
- Target install size below approximately 200 MB
- Core progression and free-roam loop stable enough for soft launch testing

## Project Rule

Do not start Phase 2 until Phase 1 is fully playable end-to-end. More generally, do not pull future-phase systems forward unless they are strictly required to make the current phase work.
