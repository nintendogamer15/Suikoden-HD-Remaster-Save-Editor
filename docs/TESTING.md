# Testing and verification

## Normal local and CI checks

Install the .NET 10 SDK, `xvfb`, and `zip`, then run:

```bash
./scripts/restore.sh
./scripts/check-workflows.sh
./scripts/check-format.sh
./scripts/build.sh
./scripts/test.sh
./scripts/publish-linux.sh
./scripts/publish-windows.sh
./scripts/smoke-test.sh
./scripts/package.sh
```

`./scripts/ci.sh` runs that sequence. The tests use only synthetic fixtures in CI. No real or sanitized derivative save is committed.

The suites cover envelope compatibility/error handling, schema detection, lossless unknown nodes, no-edit round trips, character/stat/MP/inventory/recruitment/party edits, count synchronization, restrictions, bulk-safe items, backups, atomic failure behavior, output revalidation, slot browsing, edit history, recent-path privacy, and view-model behavior.

## Opt-in private-save integration

Private integration is disabled when its environment variable is absent. Point it at an existing save root only on a trusted local machine:

```bash
SUIKODEN_PRIVATE_SAVE_ROOT=/absolute/path/to/private/saves \
SUIKODEN_UPSTREAM_ORACLE_DLL=/absolute/path/to/SuikodenSaveDecrypter.dll \
DOTNET_HOST_PATH=/absolute/path/to/dotnet \
./scripts/test.sh
```

The test enumerates recognized `Data0`–`Data16` files plus `_sharetmpsave0`, hashes originals, copies each into the test temporary directory, and performs all writes there. It opens/detects each game-slot copy, checks semantic no-edit output, makes a single controlled Potch edit, reopens it, asks the upstream tool to decrypt representative output, and then verifies original hashes. The shared temporary file decrypts and parses as JSON but deliberately is not passed to a game adapter because it lacks either verified game signature. Adapter and app tests also open temporary copies for each game.

Keep `TMPDIR` inside an ignored project directory when the working-scope policy requires all temporary work to remain in this repository. The checked-in scripts do that by default.

## GUI smoke test

`--smoke-test` starts the real Avalonia desktop lifetime, constructs the main window and compiled XAML, pumps the dispatcher, then exits successfully. `smoke-test.sh` runs the self-contained Linux publish under Xvfb with software rendering. It is a startup/resource-binding smoke check, not an interactive behavioral test.

## Workflow validation

Both workflow YAML files are checked with `actionlint`. GitHub Actions can execute after the repository is published. The Gitea workflow cannot be live-tested until a mirror and compatible self-hosted runner exist; it deliberately has no release publication or credentials and calls the same `scripts/ci.sh` entry point. Change its single `runs-on` label if the runner uses a label other than `ubuntu-latest`.

## Release artifact audit

`package.sh` requires the complete legal-file set in both publish directories, rejects private-save filenames, creates the `.tar.gz` and `.zip`, confirms license entries in each archive, and generates `SHA256SUMS.txt`. Manual game acceptance remains separate; see [MANUAL_GAME_TESTING.md](MANUAL_GAME_TESTING.md).
