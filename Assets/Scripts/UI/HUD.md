# Phase 0 Minimal HUD

Issue #8 adds only the presentation needed to understand and complete the Phase 0 parking interaction.

## Components

### `UIManager`

`UIManager` owns presentation only. It does not decide whether parking is valid and does not read vehicle speed.

It listens to:

- `InteractionController.PromptChanged`
- `ParkingZone.ParkingCompleted`

The interaction layer remains the source of truth for availability, eligibility, completion, and cancellation.

## Prototype presentation

### Interaction prompt

The prompt appears at the bottom-center of the landscape HUD when an interaction is eligible.

For the Phase 0 parking prototype it displays:

```text
ACTION / E  •  Park Here
```

It is hidden when:

- the vehicle is outside the parking zone,
- the vehicle is inside but above the parking stop threshold,
- parking has already completed during the current visit.

### Success feedback

A top-center feedback panel displays:

```text
Parked successfully
```

for approximately 2 seconds after a valid parking completion.

The timer uses unscaled time so presentation cleanup is not affected by gameplay time scale changes.

## Editor setup

After building the previous Phase 0 systems, run:

```text
Beyond The Beat > Phase 0 > Build Minimal HUD
```

Then run:

```text
Beyond The Beat > Phase 0 > Validate Minimal HUD
```

The builder extends the existing `MobileDrivingCanvas`; it does not create another Canvas or EventSystem.

## Structural validation

The validator checks:

- `MobileDrivingCanvas` exists,
- `InteractionController` exists on `PrototypeVehicle`,
- `InteractionHUD` exists,
- `UIManager` is attached,
- prompt panel exists,
- success-feedback panel exists,
- source and view references are assigned,
- prompt and feedback start hidden.

## Play Mode validation

Validate this sequence in Unity:

1. Start outside `ParkHereZone`.
   - No interaction prompt should be visible.
2. Enter the zone above 2 km/h.
   - Prompt should remain hidden.
3. Stop at or below 2 km/h.
   - `ACTION / E • Park Here` should appear.
4. Drive above the threshold again before interacting.
   - Prompt should disappear.
5. Stop again.
   - Prompt should return.
6. Press Action or `E`.
   - Parking should complete.
   - Prompt should disappear.
   - `Parked successfully` should appear temporarily.
7. Wait approximately 2 seconds.
   - Success feedback should hide automatically.
8. Fully leave and re-enter the parking zone.
   - Prompt behavior should reset for the next parking cycle.

## Android validation

On a representative landscape Android phone verify:

- prompt text is readable without covering steering/pedal controls,
- feedback text is readable,
- HUD panels do not consume touch/raycast input,
- Action can still be pressed while the prompt is visible,
- driving controls continue to support simultaneous touch,
- no prompt remains stuck after leaving the parking zone.

## Phase 0 scope boundary

This milestone intentionally does not include:

- speedometer,
- minimap/map,
- mission objective UI,
- inventory,
- economy/currency,
- settings screens,
- pause menus,
- polished final art/animation,
- persistence.

After Issue #8, Phase 0 moves to Issue #9: Android build, performance/device validation, and a shareable APK artifact.
