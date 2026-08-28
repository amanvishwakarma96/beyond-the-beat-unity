# Android Device Feedback — 2026-08-27

## Status

**BLOCKING / NOT ACCEPTED**

The current Android build was installed and run on a physical device. Device feedback identified three release-blocking problems:

1. **Driving controls do not respond to touch.**
2. **HUD/control presentation is visually poor and reads as debug/prototype UI rather than a mobile game.**
3. **World presentation looks like stock Unity/template geometry rather than an authored game environment.**
4. **The CI page exposes multiple APK artifacts (Phase 0, Phase 1, Phase 2), making the intended install target unclear.**

## Corrective acceptance criteria

A replacement device-test build is not acceptable until all of the following are true:

- Left and right steering respond on Android.
- GO + steering work simultaneously.
- BRAKE/REV + steering work simultaneously.
- ACTION works while another driving control is held.
- Releasing touches never leaves controls stuck.
- Mobile input does not rely on EventSystem pointer callbacks as a single point of failure.
- Touch controls have visible press feedback and large landscape hit targets.
- HUD includes a compact speed display and readable mission/status presentation.
- Reach + Survive exposes visible progress rather than debug-only text.
- Presentation graphics do not raycast/block driving controls.
- Final Phase 2 scene has an authored atmosphere/palette, road markings, reflectors, and zone signage.
- A Phase 2 pull request exposes only one installable current-test APK artifact.
- The artifact and APK are explicitly labeled `TEST THIS BUILD` / `INSTALL THIS BUILD`.

## CI versus device validation

Editor/CI validation can prove scene wiring, deterministic input mapping, UI references, presentation structure, and APK generation. It cannot prove the physical Android touch path or subjective visual acceptance.

Therefore the corrective PR remains device-unaccepted until the replacement `TEST-THIS-BUILD-<run>` APK is installed and the controls/UI/world are rechecked on hardware.
