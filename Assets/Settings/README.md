# Generated Unity Settings

`ProjectBootstrap.cs` creates the Phase 0 mobile URP assets in this folder the first time the project is opened in the pinned Unity editor.

Expected generated assets:

- `BeyondTheBeat_MobileURP.asset`
- `BeyondTheBeat_MobileRenderer.asset`

After the first successful Unity Editor open:

1. Let Unity finish resolving packages and importing assets.
2. Run **Beyond The Beat > Project > Run Bootstrap** if the automatic delayed bootstrap did not run.
3. Run **Beyond The Beat > Project > Validate Bootstrap** and confirm all checks pass.
4. Confirm Android Build Support is installed through Unity Hub and that Android can be selected as the build target.
5. Commit the generated URP assets and all Unity-generated `.meta` files.
6. Do not start Phase 0 gameplay implementation until the project opens without compile errors.

The Android application identifier is currently a prototype placeholder: `com.beyondthebeat.prototype`. It can be replaced before store/distribution setup.
