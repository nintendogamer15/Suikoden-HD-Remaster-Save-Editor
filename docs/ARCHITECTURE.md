# Architecture

## Boundaries

The solution uses .NET 10, Avalonia, and a conventional MVVM split:

- `SuikodenHdSaveEditor.Core` owns the encrypted envelope, lossless JSON document, schema detection, and save-slot discovery. Its `SaveFileService` and `EditHistory` remain in the tree and under test but are no longer the paths the application uses; the framework's write workflow and a snapshot-backed history took over.
- `SuikodenHdSaveEditor.Formats.Suikoden1` owns Suikoden I paths, validation, edits, and reviewable lookup data.
- `SuikodenHdSaveEditor.Formats.Suikoden2` owns Suikoden II paths, restrictions, validation, edits, and the attributed game-data catalogue.
- `SuikodenHdSaveEditor.App` owns the game-specific field definitions, the encrypted-envelope codec, and this editor's save policy. The window shell, theming, dialogs, recent paths, settings, and the file read/write workflow come from `SaveEditor.Ui`, a shared save-editor GUI framework consumed as a submodule under `external/`.

The two game adapters intentionally do not share a character or inventory serialization model. They share only the envelope/document infrastructure and small validation concepts.

## Open and edit flow

1. `SaveCrypto` validates and decrypts the `GR_DATA:` envelope.
2. `SaveDocument` parses a mutable `JsonObject` and `GameDetector` requires one unambiguous verified schema signature.
3. The applicable adapter reads typed values directly from that tree. Unknown properties and array elements stay in the same tree.
4. Each UI operation snapshots the full tree for undo/redo, invokes one validated adapter mutation, and refreshes read-only JSON from the result.
5. A failed operation restores its snapshot. It cannot partially change the current document. This holds for a single field, for Apply All over a whole section, and for the bulk actions: the framework rejects a write that raises, and the editor wraps every write so an adapter that mutated the tree before raising is still rolled back. Apply All runs as one transaction that aborts as a unit.

The UI stores recent file paths only. It has no network client, telemetry, update checker, or save-content database.

## Safe write flow

`SaveEditor.Ui` owns the write path. Save As still refuses to replace an existing destination — that is this editor's own rule, enforced through the framework's write-policy seam rather than being the framework's default, which would take a backup and ask instead. Explicit overwrite creates a verified backup first, and the framework:

1. re-runs validation, because the document was editable since the first check;
2. serializes into bounded memory, so a codec that throws never creates a file;
3. decodes what it is about to write and compares it against the document in memory;
4. copies the original to a backup, re-reads it, and compares its SHA-256 against the hash taken at open;
5. writes a temporary file, preserves the original's permissions, re-verifies nothing else changed the file, and flushes to disk;
6. atomically replaces the destination, then flushes the directory.

The comparison in step 3 uses this editor's document comparer, which compares the whole JSON tree. The encrypted envelope carries a fresh random salt on every write, so the framework's byte-identity check for the unknown-data preservation claim can never match; the codec supplies a round-trip equivalence relation that decrypts both sides and compares documents instead. Pinning the salt to obtain byte identity would mean reusing an AES-CBC key and IV across differing plaintexts.

A failed write leaves the destination's bytes exactly as they were. A successful write reports whether its round trip was verified, and the editor surfaces anything other than a confirmed verification rather than reporting a clean save. Recovery is no longer implicit: the framework creates and verifies backups and reports where they are, and the header's **Restore from backup** action puts one back, verifying it decodes before anything is written.

## Validation boundary

Structural and adapter validation prevents known corruption risks: invalid envelopes, wrong array sizes, missing required party members, invalid reviewed IDs/status values, count drift, incompatible Suikoden II equipment/runes, and HP/MP inconsistencies. Undocumented values are neither normalized nor guessed. The application cannot prove that every structurally valid story-state edit is accepted by the game, so the manual checklist remains part of release verification.

## Build architecture

Both action providers invoke `scripts/ci.sh`. That entry point restores locked dependencies, checks formatting/analyzers, builds and tests, cross-publishes self-contained, untrimmed single-file Linux and Windows executables on Linux, checks the Linux ELF dependencies, smoke-launches the Linux GUI under Xvfb to exercise embedded native libraries, and audits the executable-only outputs.

Gitea tag builds add a packaging layer around the same Linux executable. Packaging stages the repository license and notice files beside it under `/usr/lib/suikoden-hd-remaster-save-editor`; a stable `/usr/bin` symlink and desktop/icon metadata expose it to users, and licenses are also installed in normal distro locations. Arch and Fedora adapters remain separate native package definitions but consume the same validated executable. Gitea release and registry helpers compare SHA-256 before any repeat operation and never replace existing different bytes.
