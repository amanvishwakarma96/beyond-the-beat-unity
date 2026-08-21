# Phase 0 Interaction Foundation

This folder contains the reusable interaction plumbing introduced for Issue #6.

## Components

### `InteractableObject`

Abstract base for concrete interactions such as the upcoming `ParkingZone`.

It owns the common lifecycle:

1. eligibility check
2. interaction request
3. optional in-progress state
4. completion or cancellation
5. repeat/non-repeat completion state

Concrete interactions only provide their own rules by overriding:

- `CanInteract(GameObject actor)`
- `OnInteractionRequested(GameObject actor)`
- optionally `OnInteractionCancelled(GameObject actor)`

The base exposes events for:

- interaction requested
- interaction completed
- interaction cancelled

### `InteractionTrigger`

Reusable trigger-volume bridge.

When an actor carrying `InteractionController` enters the trigger, the linked `InteractableObject` is registered. When the actor fully exits, it is unregistered and any in-progress interaction is cancelled.

The trigger keeps overlap counts per controller so a vehicle with multiple colliders does not unregister simply because one wheel/body collider exits while another collider is still inside.

### `InteractionController`

Lives on the controllable actor (`PrototypeVehicle` in Phase 0).

Responsibilities:

- tracks interactables currently in range
- re-checks eligibility while the actor stays in range
- selects the nearest eligible interactable
- exposes prompt visibility/text through `PromptChanged`
- listens to `MobileDrivingInput.InteractionPressed`
- forwards the request to the active interactable
- cancels in-progress interaction when the actor exits range

It does not implement parking, missions, UI presentation, rewards, inventory, or persistence.

## Unity setup

After the environment, vehicle, camera, and mobile controls are generated, run:

```text
Beyond The Beat > Phase 0 > Build Interaction Foundation
```

Then run:

```text
Beyond The Beat > Phase 0 > Validate Interaction Foundation
```

This attaches `InteractionController` to `PrototypeVehicle`, assigns the vehicle as the actor, and connects the existing `MobileDrivingInput` action event.

## How Issue #7 uses this

`ParkingZone` should derive from `InteractableObject` and provide only parking-specific behavior, for example:

- confirm the actor is the prototype vehicle
- confirm vehicle speed is below the parking threshold
- complete the interaction when valid
- provide parking-specific success feedback/event

The trigger registration, Action-button routing, prompt contract, request event, and exit cancellation are already handled by this foundation.

## Prompt presentation

`InteractionController.PromptChanged` exposes:

```text
visible: bool
prompt: string
```

Issue #8 can subscribe a UI presenter to this event without making the interaction system depend on a specific Text/Button component.

## Scope boundary

Issue #6 intentionally does not implement:

- parking eligibility or success rules
- parking-zone visuals
- cook/repair/mechanic jobs
- mission progress
- inventory/economy
- rewards
- save/persistence
- backend/networking

## Validation checklist

Structural validation:

- `InteractionController` exists on `PrototypeVehicle`
- controller references `MobileDrivingInput`
- controller actor is `PrototypeVehicle`
- `InteractableObject`, `InteractionTrigger`, and `InteractionController` compile

Play Mode validation to perform together with Issue #7:

- entering an interaction trigger makes an eligible interaction active
- Action requests the active interaction once per press
- prompt state changes only when availability/prompt changes
- leaving the trigger unregisters the interaction
- leaving during an in-progress interaction fires cancellation
- multiple vehicle colliders do not cause premature unregister
- nearest eligible interaction wins when ranges overlap

No Phase 1+ system should be required for any of these checks.
