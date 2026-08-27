# Current Android Test Build

For Phase 2 development, install **only** the artifact named:

```text
TEST-THIS-BUILD-<run-number>
```

Inside it, install:

```text
BeyondTheBeat-TEST-<run-number>.apk
```

Do not install Phase 0 or Phase 1 artifacts when validating a Phase 2 pull request. Those phases are prerequisite/regression concerns and are rebuilt/validated by the Phase 2 automation chain.

The `INSTALL-THIS-BUILD.txt` file packaged beside the APK records the source commit, checksum, and device acceptance checklist.
