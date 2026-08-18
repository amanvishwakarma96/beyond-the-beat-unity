# Beyond The Beat

Beyond The Beat is an offline open-world mobile duty game built with Unity, URP, and C#. The player takes on a cop/duty-role character, travels across a persistent world, completes location-driven missions, and can free-roam between activities.

## Current Status

**Phase 0 — Prototype**

Current focus:
- Vehicle physics and handling feel
- Smooth vehicle-follow camera
- Mobile-friendly driving controls
- Reusable interaction foundation
- Parking interaction prototype
- Small test environment

Phase 0 intentionally excludes missions, save systems, survival, ocean exploration, economy, and networking.

## Required Delivery Workflow

Every phase must finish with validation and a shareable artifact before the next phase begins.

```text
Implementation
   ↓
Editor Validation
   ↓
Android Device Validation
   ↓
Shareable APK / Build Artifact
   ↓
Validation Report
   ↓
PR Ready for Review
   ↓
Merge
   ↓
Next Phase
```

For Phase 0, the expected minimum shareable artifact is an installable Android APK such as:

```text
BeyondTheBeat-Phase0-<build>.apk
```

Build binaries are not committed to normal Git history. The artifact location, validated commit SHA, device details, validation status, and known issues must be referenced from the Phase PR. See the Development Guide and Phase 0 Validation Report for the full gate.

## Design Pillars

1. **One world, many contexts** — gameplay mechanics activate from world context rather than separate mini-games.
2. **Missions gate content, free roam is the safety net** — the world remains playable independently of mission content.
3. **Physics is the hero, not graphics** — vehicle feel and interaction quality take priority over visual complexity.
4. **Local-first** — v1 is fully offline with no backend, login, or networking requirement.
5. **Ship small, expand in layers** — every phase should be independently playable and demo-able.

## Technology

- Unity 2022 LTS+ target
- Universal Render Pipeline (URP)
- C#
- Unity PhysX / WheelCollider or tuned raycast vehicle physics
- ScriptableObjects for gameplay configuration
- Local JSON persistence in later phases
- Android first, iOS later

## Roadmap

| Phase | Goal |
| --- | --- |
| 0 | Driving + interaction prototype |
| 1 | MVP core loop: urban/off-road map, Reach Location mission, local save, drive + park |
| 2 | Forest biome + survival context |
| 3 | Restricted-area puzzle gate |
| 4 | Cook, repair, mechanic-job free-roam interactions |
| 5 | Ocean/exploration biome |
| 6 | Optimization, UX polish, and soft-launch preparation |

> Do not begin Phase 2 until Phase 1 is fully playable end-to-end.

## Architecture Rules

- Gameplay mechanics must be reusable systems, not mission-specific implementations.
- Missions are data consumed by generic systems.
- Zones use trigger colliders and context components/events rather than hardcoded scene checks.
- Free-roam actions share a common interaction foundation.
- Persistence goes through one SaveManager when introduced.
- Avoid unnecessary per-frame allocations and Update loops; favor events, coroutines, and pooling where appropriate.

## Planned Phase 0 Structure

```text
Assets/
├── Art/
├── Audio/
├── Materials/
├── Prefabs/
│   ├── Vehicles/
│   └── Interactables/
├── Scenes/
│   └── Prototype/
├── Scripts/
│   ├── Core/
│   ├── Vehicle/
│   ├── Camera/
│   ├── Interaction/
│   └── UI/
└── Settings/

Docs/
├── GAME_DESIGN.md
├── ROADMAP.md
├── ARCHITECTURE.md
├── DEVELOPMENT.md
└── Validation/
    └── PHASE_0_VALIDATION.md
```

## Documentation

- [Game Design](Docs/GAME_DESIGN.md)
- [Roadmap](Docs/ROADMAP.md)
- [Architecture](Docs/ARCHITECTURE.md)
- [Development Guide](Docs/DEVELOPMENT.md)
- [Phase 0 Validation Report](Docs/Validation/PHASE_0_VALIDATION.md)

## Platform & Performance Direction

The project targets mobile from the beginning. Open-world content should use LOD/streaming strategies, baked lighting where suitable, and restrained asset budgets. The long-term soft-launch target is stable 30–60 FPS on a mid-range Android device and an install size below roughly 200 MB.

## License

No open-source license has been assigned at this stage.
