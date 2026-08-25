# Repository guidance

- Layout: production code lives in `src/`, tests in `tests/`, distro metadata in `packaging/`, reusable automation in `scripts/`, documentation in `docs/`, and distributable third-party terms in `LICENSES/`. `reference/` is ignored research material only.
- Build with `./scripts/restore.sh` and `./scripts/build.sh`; verify with `./scripts/check-format.sh`, `./scripts/check-packaging.sh`, and `./scripts/test.sh`. Release, package, and smoke-test entry points are documented in `scripts/README.md` and `docs/PACKAGING.md`.
- Treat every preexisting file under `saves/` as private and irreplaceable. Never modify, rename, move, delete, stage, commit, log, or distribute it. Inspect and test only temporary copies, and confirm original SHA-256 hashes after integration tests.
- Preserve unknown JSON exactly at the semantic/document level. Do not guess undocumented fields, meanings, ranges, IDs, or constraints. Expose only mappings supported by upstream code, multiple saves, or controlled evidence; otherwise leave data read-only in Advanced Data and document the limitation.
- Original project code is SPDX `0BSD`. Retain upstream copyright and license notices on copied or substantially ported code, update `THIRD_PARTY_NOTICES.md`, and include required terms in release archives.
- GitHub and Gitea Actions must call the same checked-in build scripts. Gitea jobs must remain usable on the isolated Linux Docker-in-Docker runner without GitHub-hosted actions.
- Arch and Fedora packages use the established `suikoden-hd-remaster-save-editor` identity and immutable Gitea publication helpers. Never change the `Robert/robert` registry identity, signing-key documentation, authentication split, or internal package endpoint without explicit owner approval.
- Mirror/server/runner configuration and signing-key management remain external administration work; do not mutate them from this repository.
- Never commit a broken build, private save, `reference/`, generated artifact, credential, token, or secret.
