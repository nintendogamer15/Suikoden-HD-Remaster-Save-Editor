# Changelog

All notable changes will be documented here. The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) principles.

## Unreleased

## 1.0.6 - 2026-08-25

### Changed

- Generic Windows and Linux releases are now genuine self-contained, untrimmed single-file executables with embedded native libraries and no loose runtime files.
- Complete license and notice texts are embedded in the in-app Credits/Licenses view; Arch and RPM packages continue installing their normal license and documentation payloads.

### Fixed

- Packaging no longer creates or inspects obsolete ZIP/tar archives, eliminating the undeclared `unzip` failure on the Gitea Docker-in-Docker runner.
- Linux native validation now launches the final single-file executable under Xvfb instead of requiring a loose `libSkiaSharp.so`.
- GitHub tag releases now build and validate the tagged source directly, then create or update one release without polling another workflow run.
- Linux CI launches the embedded SkiaSharp native stack under Xvfb; the Gitea Debian container installs Fontconfig, X11, Xvfb, and `xauth` first.
- Future GitHub and Gitea releases attach only the standalone executable assets and native packages, without standalone checksum text assets.

## 1.0.5 - 2026-08-25

### Fixed

- Gitea CI/release `dotnet/sdk:10.0` container now installs full Avalonia/Skia runtime deps (`libfontconfig1 libfreetype6 libharfbuzz0b` + more X11 libs) and runs `ldconfig`. This makes the Linux smoke-test succeed instead of crashing on `libSkiaSharp` / `libfontconfig.so.1` DllNotFound during Avalonia init.

## 1.0.2 - 2026-08-25

### Added

- Quantity dropdowns for verified stackable Suikoden II regular items, bounded to each item's reviewed maximum.

### Changed

- Inventory and key-item choices now show user-facing names without numeric item/category codes. Duplicate internal entries remain preserved when untouched.

### Fixed

- Suikoden II hero/save-list renaming now updates the paired `bozu_name` and `bozu_name2` fields together instead of leaving one stale.
- Partially used Suikoden II regular-item stacks now resolve to the correct item name instead of requiring an exact maximum-count match.

## 1.0.1 - 2026-08-25

### Added

- Native Arch Linux and Fedora RPM packaging for the self-contained Linux application.
- Gitea-native CI and tag release workflows that build Linux/Windows archives, validate installed packages, create Gitea releases, and publish immutable packages to the existing repositories.
- Public Arch and Fedora repository installation instructions plus package/release maintenance documentation.

## 1.0.0 - 2026-08-25

### Added

- Native Avalonia desktop editor for encrypted Suikoden I and II HD Remaster saves.
- Upstream-compatible `GR_DATA:` AES encryption/decryption and automatic schema detection.
- Lossless game-specific adapters, reviewed catalogues/restrictions, validation, and read-only Advanced Data.
- Save As, explicit overwrite with timestamped backup, verified atomic writes, undo/redo, recent paths, and slot-folder browsing.
- Searchable, name-first inventory/party/rune/equipment choices, explained recruitment dropdowns, bounded headquarters-level dropdowns, positional bath-item restrictions, and transactional Apply All.
- Confirmation-gated Characters-tab action that maximizes the six active battle characters and equips researched, class-compatible end-game recommendations while preserving fixed weapons, runes, and known locked gear.
- Synthetic CI tests plus opt-in private-save and upstream-oracle compatibility tests.
- Self-contained Linux/Windows publishing, Xvfb smoke testing, archives, checksums, GitHub Actions, and mirror-ready Gitea Actions.

### Limitations

- In-game semantic acceptance still requires the manual checklist.
- Undocumented or insufficiently constrained fields remain read-only. The bulk party action uses documented storage/gameplay caps and labels its equipment choices as researched recommendations rather than official builds.
- The Gitea workflow is statically validated but cannot run until the finished GitHub repository is later mirrored and a runner is configured.
