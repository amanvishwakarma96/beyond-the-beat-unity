# Phase 6 — Store Icon / Assets Validation

## Milestone goal

Prepare Google Play listing graphics and metadata without changing gameplay runtime systems or fabricating gameplay screenshots.

## Generated store package

The full Phase 6 Android preparation creates `build/store-assets/` containing:

- `play-store-icon-512.png`
- `feature-graphic-1024x500.png`
- `STORE-ASSET-MANIFEST.txt`
- `PLAY-STORE-LISTING.md`
- `SCREENSHOT-CAPTURE-REQUIRED.txt`

The icon and feature graphic are generated deterministically from editor-only geometry and the existing Beyond The Beat dark/cyan/amber visual language.

## Play Store icon contract

- exactly 512 x 512 pixels
- RGBA/32-bit PNG output
- maximum 1024 KB
- no ranking, pricing, review, award, Google Play, or misleading badges/text
- source artwork remains recognizable when launcher masks/crops are applied

## Feature graphic contract

- exactly 1024 x 500 pixels
- PNG output with every pixel fully opaque
- no fake gameplay screenshot content
- no device frame or store badge
- localization-neutral artwork so the same base graphic can be reused across listings

## Launcher icon contract

The full editor build renders the same brand mark at the icon sizes Unity reports for Android and assigns those textures through `PlayerSettings.SetIconsForTargetGroup`.

This milestone does not claim final adaptive-icon/play-console acceptance. Final signed release/AAB validation remains a later Phase 6 release-candidate task.

## Listing metadata contract

`Docs/Store/PLAY_STORE_LISTING.md` is the source of truth for the initial English listing draft.

Automated validation checks:

- app title is `Beyond The Beat`
- short description exists and is <= 80 characters
- full description is present and describes only implemented gameplay
- icon and feature alt text are present
- screenshot capture plan explicitly requires real Android gameplay captures
- Google Play requirements/reference are documented

## Screenshot boundary

No generated image in this milestone is presented as a gameplay screenshot.

Physical/device capture must provide at least two actual screenshots for Play Store publishing. For game recommendation surfaces, target at least three 16:9 landscape screenshots at 1920 x 1080 or higher.

Recommended capture set:

1. driving/core controls
2. forest survival
3. restricted-area puzzle
4. ocean exploration
5. mechanic/free-roam activity

## Fast PR validation

Fast CI validates without scene regeneration, PNG file generation, or APK packaging:

- store icon/feature dimensions and file-policy constants
- title and short-description limits
- deterministic brand renderer output (same input produces the same pixels)
- feature renderer remains fully opaque
- listing/capture documentation exists
- store generation is wired into the Phase 6 full build
- current single-APK workflow contract remains intact

## Full post-merge validation

The automatic Current Android Test Build additionally:

- renders the 512 x 512 Play Store icon
- renders the 1024 x 500 feature graphic
- validates PNG dimensions and Play Store icon file-size ceiling
- validates feature alpha is fully opaque
- generates and assigns Android launcher icon sizes
- copies listing metadata and screenshot-capture instructions into `build/store-assets/`
- packages the store-assets directory next to the APK inside the single `TEST-THIS-BUILD-<run>` artifact
- preserves the existing 200 MB APK gate

## Acceptance boundary

Automated validation proves deterministic asset generation, dimensions, metadata limits, package composition, and launcher-icon assignment APIs. It cannot prove final Play Console review, visual quality on every launcher mask, actual screenshot quality, device-matrix stability, signing, content rating, data safety, or final AAB acceptance.

**CI GREEN IS NOT STORE-PUBLISHING SIGN-OFF.** Real screenshots and final Play Console/release-candidate validation remain mandatory.

## Follow-up

After this milestone:

1. broader Android device-matrix testing
2. final release-candidate AAB/APK preparation
3. release notes and Phase 6 exit validation
