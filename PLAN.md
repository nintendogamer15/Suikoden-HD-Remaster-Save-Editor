# Implementation plan

Status values: `pending`, `in progress`, `verified`, or `blocked`.

| Milestone | Status | Verification |
|---|---|---|
| 1. Repository, safety exclusions, references, licensing, credits | verified | Private inputs hashed and ignored; four research clones pinned; 0BSD and notices present |
| 2. Encryption/decryption and upstream compatibility oracle | verified | 29 core tests; all private slot copies; upstream oracle checked both directions |
| 3. Lossless JSON infrastructure and automatic game detection | verified | Schema detection and semantic unknown-field/no-edit round trips pass for both games |
| 4. Suikoden II adapter and verified editor features | verified | 13 tests; all supplied S2 copies validate; MIT data/restrictions ported with attribution |
| 5. Suikoden I adapter and verified field research | verified | 9 tests; all supplied S1 copies validate; inactive-item, empty-party, and headquarters-cap semantics verified |
| 6. Avalonia MVVM UI and editing workflows | verified | Clean Release build; 14 view-model tests; searchable named choices and transactional Apply All verified; Xvfb published-app smoke launch |
| 7. Safe save, backup, undo/redo, and validation | verified | Save-As, refusal, backup, atomic failure, revalidation, history, and UI command tests |
| 8. CI, standalone builds, smoke tests, archives, documentation | verified | Shared `ci.sh`; both RIDs published; Xvfb passed; actionlint 1.7.12 passed; both archives audited |
| 9. Final corruption-risk, licensing, claims, and portability review | verified | 65 tests; private/oracle and every-section UI suite passed; originals unchanged; clones clean; licenses/artifacts and exact Git payload audited |
| 10. Party max-stat and recommended-equipment action; v1.0.0 release | verified | 65 tests, full local/private/oracle verification, and hosted CI run 32824368228 passed; v1.0.0 published from the exact passing commit with audited Linux/Windows artifacts |
| 11. Native Arch/Fedora packaging and Gitea-native releases | verified | Arch and Fedora 44 packages built and installed in isolated distro roots; command/metadata/license/payload checks and Xvfb launches passed; actionlint 1.7.12 passed |

## Known verification boundary

Automated tests can establish cryptographic and structural correctness but cannot prove that every semantic edit is accepted by the games. In-game checks remain explicitly tracked in `docs/MANUAL_GAME_TESTING.md`.
