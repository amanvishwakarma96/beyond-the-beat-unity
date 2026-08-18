# Beyond The Beat — Game Design

## Core Concept

Beyond The Beat is a lightweight open-world mobile game centered on a cop/duty-role character. The player travels through one persistent world to complete missions and can free-roam between objectives.

The same world supports multiple contexts rather than separate mini-games. Driving is the core traversal system, while biome and zone context can later activate survival, puzzle, exploration, repair, cooking, parking, and mechanic-job interactions.

## Core Player Loop

1. Enter or resume the persistent world.
2. Receive or select a mission.
3. Travel toward the objective using the shared driving/world systems.
4. Encounter context-sensitive mechanics based on the active zone.
5. Complete the objective and receive progress/rewards.
6. Continue in free roam without needing a mission to remain active.

## Design Pillars

### One World, Many Contexts

Every mechanic should integrate into the same world and shared systems. Avoid building isolated gameplay modes when zone context or data-driven configuration can activate the behavior.

### Missions Gate Content, Free Roam Is the Safety Net

The game should remain playable even when mission content is incomplete. At each milestone, free-roam traversal and available interactions should continue to function.

### Physics Is the Hero

Driving feel, collision response, weight, suspension, steering, and interaction feedback matter more than photorealistic graphics. The visual direction should remain stylized-realistic and mobile-conscious.

### Local First

Version 1 has no backend, login, networking, or mandatory cloud dependency. Persistent state will be stored locally when saving is introduced in Phase 1.

### Ship Small, Expand in Layers

Every phase must end in a playable, demonstrable build before additional systems are added.

## World Contexts

Planned logical zones:
- Urban / Road
- Off-road / Hills
- Forest
- Coastal / Ocean
- Restricted Area

Zones are logical trigger regions, not separate games. Systems should react to enter/exit context events.

## Planned Gameplay Systems

### Driving

The primary gameplay system. Vehicle handling must be tuned before mission or world complexity is layered on top.

### Missions

Planned as data-driven definitions consumed by a generic MissionManager. Initial mission types:
- Reach Location
- Reach + Survive
- Reach + Solve

### Interactions

A shared interaction layer will eventually support:
- Parking
- Cooking
- Vehicle/home repair
- Mechanic side jobs

These should reuse the same interaction foundation and differ through configuration and resulting state changes.

### Contextual Mechanics

- Forest: lightweight stamina/resource pressure and environmental hazards.
- Restricted area: a simple physics-based puzzle gate.
- Ocean: swimming/diving and exploration, deferred until later phases.

## Phase 0 Design Goal

Phase 0 exists only to prove that the drive-and-interact loop feels good.

Required experience:
- Drive around a small test environment.
- Use responsive steering/throttle/braking on mobile-friendly input.
- Follow the vehicle with a smooth camera.
- Enter a parking zone, stop correctly, and trigger successful interaction feedback.

No missions, save system, biome gameplay, economy, backend, or networking belongs in Phase 0.
