# Changelog

All notable changes will be documented here. The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) principles.

## Unreleased

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
