# Build scripts

All scripts resolve the repository from their own location and support paths containing spaces. They use the local `.dotnet/` SDK when present, keep transient CLI/NuGet data under ignored `.tools/`, and write release output under ignored `artifacts/`.

Run `restore.sh`, `check-workflows.sh`, `check-packaging.sh`, `check-format.sh`, `build.sh`, and `test.sh` for normal verification. `check-workflows.sh` uses actionlint 1.7.12 and verifies the official Linux binary checksum when it must download the tool. `publish-linux.sh` and `publish-windows.sh` create self-contained framework-dependent-free bundles from one Linux machine. `check-linux-native-dependencies.sh` checks both the apphost and dynamically loaded Skia library with `ldd`; `smoke-test.sh` then launches the published Linux GUI under Xvfb. `archive.sh` creates standalone archives, and `package.sh` audits required notices before inspecting archive contents.

`prepare-release-assets.sh` gives the generic archives versioned Gitea release names. `build-arch-package.sh` and `build-rpm-package.sh` consume the existing Linux publish directory. The validation and immutable Gitea publication helpers are described in `docs/PACKAGING.md`.

`ci.sh` is the single application CI entry point used by both GitHub Actions and Gitea Actions. Its system prerequisites on a Debian/Ubuntu runner are the .NET 10 SDK, Xvfb, zip, file, desktop-file-utils, libxml2-utils, and ShellCheck. Gitea containers may set `ACTIONLINT_OFFLINE=true` because workflow YAML was already checked before mirroring and a release must not depend on GitHub remaining reachable.
