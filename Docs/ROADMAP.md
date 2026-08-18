# Beyond The Beat — Development Roadmap

This roadmap is intentionally phased. Each phase must produce a playable, validated, shareable result before the project advances.

## Phase Delivery Gate

Every phase must complete this sequence:

```text
Implementation → Validation → Shareable Artifact → Review → Merge → Next Phase
```

A phase is not considered complete merely because its code exists. It must satisfy its exit criteria, pass validation on target hardware where applicable, and produce a shareable build/artifact with documented results.

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

Validation:
- Editor compile and scene-reference validation
- Functional driving/camera/parking checks
- Android device test of the complete drive + stop + park loop
- Basic FPS/stutter/GC/input observations
- Validation results documented in `Docs/Validation/PHASE_0_VALIDATION.md`

Shareable artifact:
- Installable Android APK named using the Phase 0/build identity
- Artifact location referenced from the Phase 0 pull request
- APK is not committed directly to Git

Exit criteria:
- Vehicle handling feels satisfying enough to build the rest of the game on top of it.
- Parking interaction works reliably.
- Prototype is playable on target mobile hardware.
- Shareable APK installs and launches successfully.
- Phase 0 validation report is complete and passing (or explicitly documents accepted known issues).

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

Validation:
- End-to-end mission flow
- Save/load persistence checks
- Free-roam continuation after mission completion
- Android device regression/performance checks

Shareable artifact:
- Validated Android MVP build plus phase validation notes

Exit criteria:
- Player can launch the game, receive/select a mission, drive to the objective, complete it, and continue free-roaming.
- Validated shareable build is available for review/testing.

## Phase 2 — Forest Biome + Survival Trigger

**Goal:** Add the first contextual biome mechanic.

Scope:
- Forest zone
- Lightweight stamina/resource meter
- Basic environmental risk
- Reach + Survive mission type

Validation:
- Zone enter/exit activation and reset behavior
- Survival mechanic functional checks
- Regression of Phase 1 mission/free-roam loop
- Android device performance check in forest content

Shareable artifact:
- Validated Android build demonstrating both mission types and forest context

Exit criteria:
- Entering/leaving the forest correctly changes gameplay context.
- Two mission types work through shared systems.
- Validated shareable build is available.

## Phase 3 — Restricted Area Puzzle Gate

**Goal:** Add reusable mission gating.

Scope:
- One physics-based puzzle pattern
- Gate/door unlock behavior
- Reach + Solve mission type

Validation:
- Locked/unlocked state correctness
- Puzzle reset/retry reliability
- Mission completion integration
- Regression test of existing mission types

Shareable artifact:
- Validated Android build demonstrating the restricted-area mission flow

Exit criteria:
- Puzzle reliably blocks and unlocks access.
- Three mission types work without mission-specific architectural duplication.
- Validated shareable build is available.

## Phase 4 — Free Roam Expansion

**Goal:** Expand non-mission activities through the shared interaction system.

Scope:
- Cook
- Repair
- Mechanic job
- Existing parking interaction retained

Validation:
- Each interaction activates/completes/cancels correctly
- Shared interaction foundation is reused without duplicated plumbing
- Regression check for parking and mission gameplay

Shareable artifact:
- Validated Android build demonstrating all four free-roam interactions

Exit criteria:
- Four free-roam activities share one reusable interaction foundation.
- Validated shareable build is available.

## Phase 5 — Ocean / Exploration

**Goal:** Add water exploration only after the earlier systems prove engaging.

Scope:
- Swim/dive controller
- Lightweight water rendering suitable for URP/mobile
- Exploration mission type

Validation:
- Water enter/exit and controller-state transitions
- Exploration mission behavior
- Mobile rendering/performance checks in water areas
- Regression of existing world/mission systems

Shareable artifact:
- Validated Android build demonstrating ocean exploration

Exit criteria:
- Water zone is playable and performant.
- Mission variety reaches four types.
- Validated shareable build is available.

## Phase 6 — Polish & Soft Launch Preparation

**Goal:** Stabilize, optimize, and prepare for store testing.

Scope:
- Performance optimization
- Tutorial/onboarding
- UI/UX polish
- Store icon/assets
- Build-size optimization
- Device testing

Validation:
- Broader device matrix testing
- Regression of core progression/free-roam flows
- Performance/build-size checks
- Store-readiness smoke testing

Shareable artifact:
- Release-candidate Android build (AAB/APK as appropriate), release notes, and final validation summary

Exit criteria:
- Stable 30–60 FPS on a representative mid-range Android device
- Target install size below approximately 200 MB
- Core progression and free-roam loop stable enough for soft launch testing
- Release-candidate artifact has passed the defined validation gate

## Project Rule

Do not start Phase 2 until Phase 1 is fully playable end-to-end. More generally, do not pull future-phase systems forward unless they are strictly required to make the current phase work.

No phase may advance without both:
1. documented validation against its exit criteria, and
2. a shareable build/artifact suitable for review or testing.
