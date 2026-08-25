# Build scripts

All scripts resolve the repository from their own location and support paths containing spaces. They use the local `.dotnet/` SDK when present, keep transient CLI/NuGet data under ignored `.tools/`, and write release output under ignored `artifacts/`.

Run `restore.sh`, `check-workflows.sh`, `check-format.sh`, `build.sh`, and `test.sh` for normal verification. `check-workflows.sh` uses actionlint 1.7.12 and verifies the official Linux binary checksum when it must download the tool. `publish-linux.sh` and `publish-windows.sh` create self-contained framework-dependent-free bundles from one Linux machine. `smoke-test.sh` launches the published Linux GUI under Xvfb. `archive.sh` and `checksums.sh` are reusable release steps; `package.sh` audits required notices and invokes both before inspecting archive contents.

`ci.sh` is the single CI entry point used by both GitHub Actions and Gitea Actions. Its system prerequisites on a Debian/Ubuntu runner are the .NET 10 SDK, `xvfb`, and `zip`.
