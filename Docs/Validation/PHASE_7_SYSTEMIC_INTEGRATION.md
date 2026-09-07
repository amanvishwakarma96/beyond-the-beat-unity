# Phase 7 — Systemic Integration & Vehicle Upgrades Loop Validation

## Goal

Connect the existing vehicle, world context, economy, repair, HUD and local-persistence systems into one repeatable wear → degraded handling → paid repair → restored handling loop without adding external assets or backend dependencies.

Phase 7 is rebased on the merged Phase 6 UI/code-health baseline. The Phase 7 digital cluster therefore extends the existing TextMeshPro + `HudPanel` presentation architecture rather than reintroducing legacy `UnityEngine.UI.Text` components.

## Automated fast gate

`BeyondTheBeat.Editor.Phase7IntegrationFastValidation.Validate` must pass on the exact PR head.

It composes all existing Phase 5/6 fast contracts, then verifies:

- healthy vehicle condition produces stronger acceleration than damaged condition;
- health/tire degradation reduces effective braking;
- tire wear reduces traction;
- off-road wear is zero below the configured speed threshold and positive above it;
- sub-threshold collisions cause no condition damage while severe impacts do;
- a 50% health / 50% tire repair quote is deterministic;
- insufficient credits produce a proportional partial repair and do **not** fire `RepairCompleted`;
- adding the exact remaining credits completes the repair, spends the balance and fires `RepairCompleted` once;
- Phase 7 health/tire values survive `SaveManager` JSON round-trip;
- older version-1 saves without Phase 7 fields normalize to healthy condition and new tires.

## Generated scene integration

The full Android build calls `Phase7IntegrationBuilder.PrepareAndValidateOrThrow()` after the existing Phase 6 store/device-matrix preparation.

The builder must:

- keep `PrototypeVehicle` as the single `VehicleController` source;
- attach/configure `VehicleWearZoneAdapter` against existing `OffRoad` and `Forest` `ZoneContext` instances;
- wire the existing Phase 4 `RepairStation` to the existing `CreditWallet` and vehicle;
- seed 300 test credits so the first damaged-vehicle repair is not deadlocked;
- preserve `RepairStation.RepairCompleted` as a **full repair only** event so existing mechanic-job reward semantics remain valid;
- augment the existing programmatic speed panel with non-raycasting DAMAGE and TRACTION meters using `TMP_Text` + `Image` while retaining the shared `HudPanel` transition contract;
- use the tactical `MobileUiTheme` palette anchored on charcoal `#1E222A` and preserve the Phase 6 chamfered/hairline procedural sprites.

## Vehicle-condition model

`VehicleController` owns bounded `healthCondition` and `tireWear` state in the `0..1` range.

The condition model intentionally remains deterministic and conservative:

- acceleration scales down with lost health but does not fall below a usable floor;
- braking combines health and tire condition;
- wheel friction stiffness scales with tire traction;
- high tire wear introduces only a small steering drift;
- off-road wear is applied only when the vehicle is grounded, inside an observed off-road/forest context, and above the configured speed threshold;
- collision damage is applied only above the severe-impact threshold;
- all public evaluation helpers clamp/normalize their output so fast CI can validate the balance envelope without requiring physics playback.

## Repair-economy contract

`RepairStation` quotes repair cost from missing health and tire condition.

If the wallet cannot cover the full quote:

- all available credits may be consumed;
- health/tire state is repaired proportionally to the amount paid;
- the station does **not** emit the legacy `RepairCompleted` event;
- the existing mechanic-job completion/reward loop therefore does not complete prematurely.

When the remaining repair is fully funded, the vehicle returns to full health/new tires and `RepairCompleted` fires once.

## Persistence contract

Phase 7 extends existing save data additively with:

- `HasPhase7VehicleConditionState`
- `VehicleHealthCondition`
- `VehicleTireWear`

Older save payloads remain valid. When those fields are absent, normalization falls back to healthy condition and new tires without rejecting the save.

## Physical Android validation required

CI GREEN IS NOT PHASE 7 GAMEPLAY-FEEL SIGN-OFF.

On at least one representative mid-range Android phone, verify:

1. Drive on normal road at speed and confirm condition does not visibly drain from normal traversal alone.
2. Enter an OffRoad or Forest context and drive above ~45 km/h; confirm health/tire wear increases gradually rather than instantly.
3. Confirm worn tires produce noticeable but controllable steering drift and reduced traction.
4. Confirm damaged condition reduces acceleration and braking effectiveness without making the vehicle unusable.
5. Perform a meaningful collision above the severe-impact threshold; confirm DAMAGE increases once and no runtime exception occurs.
6. Enter the repair station with enough credits; confirm the quoted repair restores both health and tire condition and deducts credits.
7. Repeat with insufficient credits; confirm only a proportional repair occurs and the mechanic-job full-repair event/reward does not trigger early.
8. Complete the remaining repair after earning/adding credits; confirm full-repair semantics and mechanic-job compatibility.
9. Quit/relaunch after creating vehicle wear; confirm health/tire state restores from local save.
10. Confirm the TMP digital cluster and inherited HudPanel transition remain readable, safe-area compliant and non-blocking over LEFT/RIGHT/GO/REV/ACTION and swim controls.
11. Run the existing mission, survival, puzzle, mechanic-job, ocean/swim and tutorial regressions.
12. Record FPS/frame-time/thermal observations and confirm the new wear adapter adds no per-frame scene discovery/allocation loop.

## Acceptance boundary

Phase 7 code is ready for review when exact-head fast CI is green and the post-merge Android build successfully regenerates the integrated scene. Final Phase 7 acceptance still requires the physical handling/balance/save/UI checks above.
