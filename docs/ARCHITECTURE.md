# Architecture

## Boundaries

The solution uses .NET 10, Avalonia, and a conventional MVVM split:

- `SuikodenHdSaveEditor.Core` owns the encrypted envelope, lossless JSON document, schema detection, save-slot discovery, edit history, and crash-safe file writing.
- `SuikodenHdSaveEditor.Formats.Suikoden1` owns Suikoden I paths, validation, edits, and reviewable lookup data.
- `SuikodenHdSaveEditor.Formats.Suikoden2` owns Suikoden II paths, restrictions, validation, edits, and the attributed game-data catalogue.
- `SuikodenHdSaveEditor.App` owns Avalonia views, view models, native dialogs, recent paths, confirmations, and application state.

The two game adapters intentionally do not share a character or inventory serialization model. They share only the envelope/document infrastructure and small validation concepts.

## Open and edit flow

1. `SaveCrypto` validates and decrypts the `GR_DATA:` envelope.
2. `SaveDocument` parses a mutable `JsonObject` and `GameDetector` requires one unambiguous verified schema signature.
3. The applicable adapter reads typed values directly from that tree. Unknown properties and array elements stay in the same tree.
4. Each UI operation snapshots the full tree for undo/redo, invokes one validated adapter mutation, and refreshes read-only JSON from the result.
5. A failed operation restores its snapshot. It cannot partially change the current document.

The UI stores recent file paths only. It has no network client, telemetry, update checker, or save-content database.

## Safe write flow

Save As refuses to replace an existing destination. Explicit overwrite first copies the source to a uniquely timestamped `SuikodenSaveEditor Backups` directory. Both modes then:

1. encrypt the intended JSON with a new cryptographic salt;
2. write to a unique same-directory temporary file using write-through and a disk flush;
3. decrypt, parse, detect, and semantically compare that temporary output with the intended tree;
4. atomically move the temporary file to the destination;
5. decrypt and compare the committed file again.

If post-commit verification fails during overwrite, the timestamped backup is restored. A Save As post-commit failure removes the invalid new destination. Temporary files are cleaned in all handled paths.

## Validation boundary

Structural and adapter validation prevents known corruption risks: invalid envelopes, wrong array sizes, missing required party members, invalid reviewed IDs/status values, count drift, incompatible Suikoden II equipment/runes, and HP/MP inconsistencies. Undocumented values are neither normalized nor guessed. The application cannot prove that every structurally valid story-state edit is accepted by the game, so the manual checklist remains part of release verification.

## Build architecture

Both action providers invoke `scripts/ci.sh`. That entry point restores locked dependencies, checks formatting/analyzers, builds and tests, cross-publishes self-contained Linux and Windows bundles on Linux, smoke-launches the Linux GUI under Xvfb, audits licenses, archives outputs, and writes checksums.
