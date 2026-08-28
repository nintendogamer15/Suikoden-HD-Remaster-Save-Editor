# Changelog

All notable changes will be documented here. The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) principles.

## Unreleased

## 2.1.2 - 2026-08-28

### Fixed

- Gitea RPM uploads now request server signing so normal DNF GPG verification works.

## 2.1.1 - 2026-08-28

### Fixed

- File → Recent now fills in as saves are opened, keeps the newest first, and survives a
  restart. Opening a save never recorded it, so the submenu stayed empty however many were
  opened.
- Save As shows its destination chooser again, and starts in the folder of the save being
  copied. The framework raised the chooser and the overwrite confirmation from a background
  thread, where building them fails; the failure was caught and reported as a failed save, so
  nothing appeared and nothing said why.
- Save As no longer stays greyed out once the operation has finished.
- The section toolbar uses the same card surface and rounded corners as the fields below it,
  instead of a lighter square strip.

## 2.1.0 - 2026-08-27

### Fixed

- Recent, Themes, and Accent menu items now work when populated from data.
- Clicking past a textbox's glyphs now places the caret at the clicked position.
- About and document dialogs now scroll on small screens.
- Destructive buttons remain red while hovered or pressed.
- FieldList cards no longer receive accent selection paint.

### Removed

- Unsupported File → Open Folder and the blank menu item after Help.

## 2.0.1 - 2026-08-27

### Fixed

- Apply did nothing visible. Field readers captured the value the section was built with, and
  the framework re-reads that delegate every time it reports the committed value, so the write
  landed in the document but the field stayed pending, its Apply button never settled, and the
  exit guard believed a saved file still had unapplied work. Readers now query the adapter.
- The Characters section was empty, because it is built for one character and nothing chose
  one. It has its picker back, along with the All / Recruited / Unrecruited / Current party
  filter and the name-or-id search that went missing in the migration.
- Both recruitment sections could refuse an edit as a no-op by comparing it against a value
  read before the section was built rather than the current one.
- Advanced Data was empty. It shows the decrypted document again, read-only and scrollable.

### Removed

- The duplicate Credits section in the sidebar. Help → About and credits already shows the
  embedded licence and notice texts.
- The folder-slot picker in the sidebar.

## 2.0.0 - 2026-08-27

### Changed

- The editor is rebuilt on `SaveEditor.Ui`, a shared save-editor GUI framework consumed as a
  git submodule under `external/`. It supplies the window shell, Catppuccin theming with a
  light/dark toggle, dialogs, recent paths, settings, and the file read/write workflow. The
  save format, validation, and every reviewed field mapping are unchanged.
- Fields are now typed rather than parsed from text on apply, so numbers carry the bounds the
  adapters already enforced and choice lists filter as you type.
- Control styling is no longer hardcoded, which fixes the error banner being unreadable in
  dark mode.
- Backups are created and verified by the framework and no longer use the
  `SuikodenSaveEditor Backups/<timestamp>_<name>` layout. A successful save now reports whether
  its round trip was verified, and anything less is surfaced rather than reported as clean.

### Added

- **Restore from backup**, which puts a verified backup back over the open save. Recovery used
  to happen implicitly inside the writer and otherwise meant copying files by hand.

### Removed

- The read-only "Catalogue · <name>" rows that the Inventory sections appended while searching.
  The item choice control now filters the full catalogue directly, which is what those rows
  were standing in for.

## 1.0.7 - 2026-08-25

### Fixed

- Linux package staging now applies explicit `0755` directory and `0644` file modes instead of umask-dependent ones, so `rpmlint` no longer rejects the packaged `/usr/lib/suikoden-hd-remaster-save-editor/LICENSES` directory as a non-standard `775` directory.
- The Arch package job clears the container image's `NoExtract` rules before installing, so the README and third-party notices the package installs under `/usr/share/doc` are actually present for installed-package validation instead of being silently discarded.
- Installed-package validation now names every assertion and reports the underlying diagnostic, so a failing package job identifies each broken expectation instead of ending on a bare exit code.

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
