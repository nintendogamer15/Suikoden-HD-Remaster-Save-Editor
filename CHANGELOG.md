# Changelog

All notable changes will be documented here. The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) principles; no stable release has been published yet.

## Unreleased

### Added

- Native Avalonia desktop editor for encrypted Suikoden I and II HD Remaster saves.
- Upstream-compatible `GR_DATA:` AES encryption/decryption and automatic schema detection.
- Lossless game-specific adapters, reviewed catalogues/restrictions, validation, and read-only Advanced Data.
- Save As, explicit overwrite with timestamped backup, verified atomic writes, undo/redo, recent paths, and slot-folder browsing.
- Searchable, name-first inventory/party/rune/equipment choices, explained recruitment dropdowns, bounded headquarters-level dropdowns, positional bath-item restrictions, and transactional Apply All.
- Synthetic CI tests plus opt-in private-save and upstream-oracle compatibility tests.
- Self-contained Linux/Windows publishing, Xvfb smoke testing, archives, checksums, GitHub Actions, and mirror-ready Gitea Actions.

### Limitations

- In-game semantic acceptance still requires the manual checklist.
- Undocumented or insufficiently constrained fields remain read-only; no guessed “max all” values are exposed.
- The Gitea workflow is statically validated but cannot run until the finished GitHub repository is later mirrored and a runner is configured.
