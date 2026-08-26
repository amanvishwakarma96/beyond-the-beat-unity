# Phase 1 Local Save / Resume

Issue #29 adds the first local-first persistence layer for the Phase 1 MVP.

## Architecture

Persistence is centralized in `Assets/Scripts/Persistence`:

- `GameSaveData` — one versioned serializable save representation.
- `SaveManager` — the only component that reads/writes/deletes the local save file.
- `Phase1SaveCoordinator` — captures/restores current Phase 1 gameplay state by coordinating existing systems.

No `PlayerPrefs`, backend, account, login, cloud-save, or networking dependency is used.

## Save File

Current format version: `1`

Default file name:

```text
beyond-the-beat-phase1.json
```

The file lives under Unity's `Application.persistentDataPath` and currently stores:

- save version
- Phase 1 scene identity
- vehicle world position
- vehicle world rotation
- current mission ID
- current mission state

Mission persistence stores a stable mission ID + generic `MissionState`; it does not contain Reach-Location-specific save logic.

## Runtime Flow

`MissionManager` starts normally first. `Phase1SaveCoordinator` uses a later execution order and then attempts resume:

1. `SaveManager.Load` reads/classifies the local file.
2. A valid compatible save restores mission state by stable mission ID.
3. Vehicle transform is restored with linear/angular velocity reset to zero.
4. Missing/corrupt/incompatible/unreadable data falls back to the captured new-game spawn and starting mission.

The coordinator exposes explicit operations for later UI integration:

- `SaveNow()`
- `LoadNow()`
- `ResetProgress()`

It also saves when the app is paused/backgrounded and when the application quits.

## Safety / Compatibility

`SaveLoadResult` distinguishes:

- `Success`
- `Missing`
- `Corrupt`
- `Incompatible`
- `IoError`

An incompatible version is never partially applied. The current milestone intentionally does not implement migrations; a future save-version change should add an explicit migration path before increasing `SaveManager.CurrentVersion`.

## Editor Build / Validation

Build the persistence scene integration:

```text
Beyond The Beat > Phase 1 > Build Local Save Resume
```

Validate it:

```text
Beyond The Beat > Phase 1 > Validate Local Save Resume
```

The validator checks:

- centralized scene references
- version/file identity
- vehicle + mission JSON round-trip
- corrupt/incompatible fallback classification
- active mission restoration
- completed mission restoration/free-roam state

The validator performs serialization checks in memory and does not touch the developer/CI runner's real persistent save file.

## Scope Boundary

Issue #29 does not add:

- mission HUD/buttons
- save-slot UI
- multiple save profiles
- cloud sync
- authentication
- inventory/economy persistence
- future biome mechanics

Final Phase 1 UI/integration/device validation remains Issue #30.
