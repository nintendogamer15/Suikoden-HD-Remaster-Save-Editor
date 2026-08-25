# Testing and verification

## Normal local and CI checks

Install the .NET 10 SDK, `xvfb`, and `zip`, then run:

```bash
./scripts/restore.sh
./scripts/check-workflows.sh
./scripts/check-packaging.sh
./scripts/check-format.sh
./scripts/build.sh
./scripts/test.sh
./scripts/publish-linux.sh
./scripts/publish-windows.sh
./scripts/smoke-test.sh
./scripts/package.sh
```

`./scripts/ci.sh` runs that sequence. The tests use only synthetic fixtures in CI. No real or sanitized derivative save is committed.

The suites cover envelope compatibility/error handling, schema detection, lossless unknown nodes, no-edit round trips, character/stat/MP/inventory/recruitment/party edits, count synchronization, restrictions, bulk-safe items, named dropdown contents, transactional Apply All (including party and HP dependencies), max-stat/equipment recommendations and lock preservation, undo of the bulk party action, backups, atomic failure behavior, output revalidation, slot browsing, edit history, recent-path privacy, and view-model behavior.

## Opt-in private-save integration

Private integration is disabled when its environment variable is absent. Point it at an existing save root only on a trusted local machine:

```bash
SUIKODEN_PRIVATE_SAVE_ROOT=/absolute/path/to/private/saves \
SUIKODEN_UPSTREAM_ORACLE_DLL=/absolute/path/to/SuikodenSaveDecrypter.dll \
DOTNET_HOST_PATH=/absolute/path/to/dotnet \
./scripts/test.sh
```

The test enumerates recognized `Data0`–`Data16` files plus `_sharetmpsave0`, hashes originals, copies each into the test temporary directory, and performs all writes there. It opens/detects each game-slot copy, checks semantic no-edit output, makes a single controlled Potch edit, runs the max-stat/equipment action on a separate in-memory copy, re-encrypts and reopens the intended optimized document, asks the upstream tool to decrypt representative output, and then verifies original hashes. The shared temporary file decrypts and parses as JSON but deliberately is not passed to a game adapter because it lacks either verified game signature. Adapter and app tests also open temporary copies for each game.

Keep `TMPDIR` inside an ignored project directory when the working-scope policy requires all temporary work to remain in this repository. The checked-in scripts do that by default.

## GUI smoke test

`--smoke-test` starts the real Avalonia desktop lifetime, constructs the main window and compiled XAML, pumps the dispatcher, then exits successfully. `smoke-test.sh` runs the self-contained Linux publish under Xvfb with software rendering. It is a startup/resource-binding smoke check, not an interactive behavioral test.

## Workflow validation

All GitHub and Gitea workflow YAML files are checked with `actionlint`. Gitea performs checkout with native Git and its built-in token rather than GitHub-hosted checkout/setup/artifact actions. Branch and manual CI call the same `scripts/ci.sh` entry point. A manual release-workflow dispatch builds and installs both native packages but publication conditions remain false unless the ref is a valid release tag.

## Native package validation

Arch validation runs in `archlinux:base-devel`: build with `makepkg`, inspect with `namcap` and pacman metadata tools, install with pacman, validate the command/desktop/icon/licenses/bundle, compare the installed application directory byte-for-byte with its input, and run `--smoke-test` under Xvfb. Fedora validation follows the equivalent `rpmbuild`, rpmlint, rpm query, DNF install, installed-payload comparison, and Xvfb launch sequence in a current Fedora container.

Both paths reject private saves, references, tests, fixtures, debug symbols, package outputs, internal package infrastructure, and credentials from their input. Package and release helpers are syntax/static checked locally. Registry immutability and Gitea API calls reach their final live boundary only after an approved new tag; see [PACKAGING.md](PACKAGING.md).

## Release artifact audit

`package.sh` requires the complete legal-file set in both publish directories, rejects private-save filenames, creates the `.tar.gz` and `.zip`, confirms license entries in each archive, and generates `SHA256SUMS.txt`. Manual game acceptance remains separate; see [MANUAL_GAME_TESTING.md](MANUAL_GAME_TESTING.md).
