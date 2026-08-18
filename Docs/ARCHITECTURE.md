# Beyond The Beat — Architecture

## Architectural Intent

Beyond The Beat is one persistent open-world game composed of reusable gameplay systems. Mechanics are activated by player/world context and mission data rather than implemented as separate mini-games or one-off mission scripts.

## Core Rules

### 1. Reusable gameplay systems

Do not place mission-specific logic inside vehicle, zone, interaction, or UI systems. Missions should orchestrate existing systems using data and events.

### 2. Data-driven missions

When missions are introduced in Phase 1, mission definitions should be represented with ScriptableObjects or equivalent configuration data. A generic MissionManager should consume that data.

Mission configuration may describe:
- Objective type
- Target location/zone
- Success condition
- Failure condition
- Rewards/progression

### 3. Event-driven zone context

Zones should use trigger colliders and a ZoneContext-style component. Other systems subscribe to zone-enter/zone-exit events rather than checking scene names or embedding biome-specific conditionals throughout gameplay code.

### 4. Shared interaction foundation

Parking, cooking, repairing, and mechanic jobs must eventually reuse one interaction foundation. Individual interactions can provide their own validation and effects but should not duplicate prompt, activation, timing, or completion infrastructure.

### 5. Centralized persistence

When persistence is introduced in Phase 1, all durable gameplay state goes through one SaveManager and one local save representation. Avoid scattered PlayerPrefs usage.

Planned saved state includes:
- Player/world position
- Current mission and progress
- Unlocked zones/content
- Vehicle condition/fuel state
- Inventory/resources

### 6. Mobile performance first

Avoid:
- Per-frame managed allocations
- Unnecessary Update methods
- Large always-loaded world detail
- Unbounded Instantiate/Destroy cycles for repeated objects

Prefer:
- Events
- Coroutines where appropriate
- Object pooling
- LOD and zone/scene streaming strategies
- Baked lighting where suitable
- Reused materials and mobile-conscious assets

## Phase 0 Script Responsibilities

### VehicleController

Responsible for vehicle movement and physics only.

Expected inspector-tunable values include:
- Motor torque
- Brake torque
- Steering angle
- Suspension-related tuning
- Speed/steering response parameters if needed

It should not contain parking, missions, UI, save logic, or biome behavior.

### CameraFollow

Responsible only for smooth vehicle camera movement/rotation and follow behavior.

### InteractableObject

Reusable interaction base/foundation responsible for common interaction contract and interaction events.

### ParkingZone

Phase 0 example interaction. Validates that the vehicle is inside the required area and sufficiently stopped before completing the parking action.

### UIManager

Responsible for minimal prompt/interaction feedback needed by Phase 0. It should not become a gameplay-state manager.

### Mobile Input

Input should expose normalized steering, throttle, brake, and interaction intent to the vehicle/interaction systems rather than coupling gameplay code directly to specific on-screen button implementations.

## Planned Project Structure

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
```

The structure may evolve with the project, but new folders/systems should reflect real architectural responsibilities rather than future-scope speculation.

## Phase Boundary

Phase 0 must not contain:
- MissionManager
- SaveManager
- Inventory/economy systems
- Forest survival
- Restricted-area puzzle systems
- Ocean/swimming systems
- Authentication/networking/backend integrations

The only exception is a tiny abstraction strictly needed to keep a Phase 0 component reusable for Phase 1 without implementing Phase 1 behavior itself.
