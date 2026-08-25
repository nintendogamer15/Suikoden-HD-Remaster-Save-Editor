# Implementation plan

Status values: `pending`, `in progress`, `verified`, or `blocked`.

| Milestone | Status | Verification |
|---|---|---|
| 1. Repository, safety exclusions, references, licensing, credits | verified | Private inputs hashed and ignored; four research clones pinned; 0BSD and notices present |
| 2. Encryption/decryption and upstream compatibility oracle | verified | 29 core tests; all private slot copies; upstream oracle checked both directions |
| 3. Lossless JSON infrastructure and automatic game detection | verified | Schema detection and semantic unknown-field/no-edit round trips pass for both games |
| 4. Suikoden II adapter and verified editor features | in progress | Adapter/category tests and documented mappings |
| 5. Suikoden I adapter and verified field research | pending | Adapter/category tests and evidence/confidence documentation |
| 6. Avalonia MVVM UI and editing workflows | pending | View-model tests and interactive smoke check |
| 7. Safe save, backup, undo/redo, and validation | pending | Atomicity, backup, revalidation, and history tests |
| 8. CI, standalone builds, smoke tests, archives, documentation | pending | Local Linux/Windows publish; Xvfb; workflow lint; archive audit |
| 9. Final corruption-risk, licensing, claims, and portability review | pending | Clean tests/builds; original hashes unchanged; Git status reviewed |

## Known verification boundary

Automated tests can establish cryptographic and structural correctness but cannot prove that every semantic edit is accepted by the games. In-game checks remain explicitly tracked in `docs/MANUAL_GAME_TESTING.md`.
