# Contributing

Contributions are welcome when they preserve save safety and licensing provenance.

1. Read `AGENTS.md`, the architecture, relevant format document, and upstream notices.
2. Never add a real save, decrypted private JSON, player/Steam ID, credential, reference clone, or generated artifact. Use a minimal synthetic fixture containing no personal or unrelated data.
3. Do not name or expose an undocumented field from intuition. Cite inspected source code, compare multiple independent saves, or provide a controlled reproducible test; otherwise preserve it in Advanced Data.
4. Keep Suikoden I and II serialization rules in their separate adapter projects. Put reviewable catalogues outside UI code.
5. Retain third-party copyright/license notices on copied or substantially ported work and update `THIRD_PARTY_NOTICES.md` and `LICENSES/`.
6. Run `./scripts/ci.sh` on Linux, or at minimum restore, packaging/format checks, build, and test before opening a pull request.
7. Describe save-corruption risk, evidence, tests, and manual in-game results honestly. A structurally passing test is not proof of game acceptance.

Original contributions are accepted under the repository's Zero-Clause BSD (`0BSD`) license unless a file clearly retains another compatible upstream license.

Packaging changes must preserve the package identity and the immutable-publication behavior documented in `docs/PACKAGING.md`. Never test publication with a real version, replace a registry package, or expose a package token.
