# Packaging and Gitea releases

## Identity and outputs

The source application remains `SuikodenHdSaveEditor.App`. Generic Windows and Linux releases are genuine self-contained single-file executables: there are no loose assemblies, runtime JSON files, native libraries, or framework files. Linux packages install that executable plus legal/documentation files under `/usr/lib/suikoden-hd-remaster-save-editor` and provide `/usr/bin/suikoden-hd-remaster-save-editor` as a symlink to it.

For a valid `vX.Y.Z` tag, the Gitea workflow produces:

- `SuikodenHdSaveEditor-vX.Y.Z-linux-x64`
- `SuikodenHdSaveEditor-vX.Y.Z-windows-x64.exe`
- `suikoden-hd-remaster-save-editor-X.Y.Z-1-x86_64.pkg.tar.zst`
- `suikoden-hd-remaster-save-editor-X.Y.Z-1.x86_64.rpm`

Standalone checksum text files are not public release assets. Package-manager integrity metadata and the private SHA-256 comparisons used to enforce immutable package versions remain in place.

The raw Linux download may need `chmod +x SuikodenHdSaveEditor-vX.Y.Z-linux-x64`. Complete license and notice texts are embedded in the application's Credits/Licenses view. The Arch and RPM packages also install the desktop entry, scalable icon, root license, complete third-party license directory, notices, and README in normal system locations. Neither package depends on a system .NET runtime.

## Native dependency evidence

The `linux-x64` executable is inspected with `file` and `ldd`. Embedded native libraries cannot be validated by looking for loose files, so CI launches the real application under Xvfb; this forces .NET to extract and load SkiaSharp, HarfBuzz, and their dependencies. Debian-based CI retains `libfontconfig1`, the X11 libraries, Xvfb, `xauth`, and DejaVu fonts, and the smoke test rejects missing-library crashes. The dependency names were compared with the working FFIX and FFIV Avalonia packages and are also verified by installing and launching each distro package in a clean job. The packages retain their explicit `fontconfig` runtime dependency and do not add a system .NET dependency.

The self-contained .NET publish also carries the optional diagnostics-only `libcoreclrtraceptprovider.so`, which still declares the retired `liblttng-ust.so.0` ABI. Current Fedora provides `liblttng-ust.so.1`, and the editor does not load the diagnostics provider during normal execution. The RPM therefore excludes only that obsolete automatic requirement; all application startup dependencies remain enforced and are tested by DNF installation plus the Xvfb smoke launch.

## Checked-in package tooling

- `build-arch-package.sh` stages the Linux executable and repository legal files into a real `makepkg` build.
- `build-rpm-package.sh` builds the same staged payload through `rpmbuild` and the checked-in spec.
- `validate-package-input.sh` requires exactly one executable ELF file and rejects private saves, references, tests, build products, package files, debug symbols, and package infrastructure strings.
- `check-linux-native-dependencies.sh` reports unresolved dependencies declared directly by the single-file apphost; the Xvfb launch validates embedded native functionality.
- `validate-installed-package.sh` checks the installed command target, executable, licenses, notices, desktop file, icon, architecture, ELF resolution, and forbidden content. Each assertion is named, so a failing job reports every broken expectation instead of exiting on the first one. The Arch job clears the container image's `NoExtract` rules before installing, because they would otherwise silently discard everything the package installs under `/usr/share/doc`.
- `gitea-release-assets.sh` creates a native Gitea release and treats existing release assets as immutable.
- `gitea-publish-package.sh` authenticates package operations as `Robert`, safely skips byte-identical existing packages, rejects different bytes for an existing version, and treats optional repository linking failures as warnings.

`PACKAGE_PUBLISH_TOKEN` is used only for registry API and upload operations. The built-in `GITEA_TOKEN` is used for source checkout, release API operations, and release assets. The package upload base is supplied separately through `GITEA_PACKAGE_SERVER_URL`; it is intentionally not an end-user URL.

## Workflow behavior

`.gitea/workflows/ci.yml` runs normal CI on `main`, pull requests when available, and manual dispatches. `.gitea/workflows/release.yml` runs three isolated jobs on the `ubuntu-latest` runner label:

1. the .NET 10 container installs the native Avalonia/Skia runtime libraries, tests, cross-publishes single-file Windows/Linux executables, checks the Linux apphost, smoke-tests embedded native functionality, audits payloads, and creates release assets;
2. a clean Arch `base-devel` container builds, installs, validates, and smoke-tests the Arch package;
3. a clean current Fedora container builds, installs, validates, and smoke-tests the RPM.

Only a ref matching `refs/tags/vX.Y.Z` reaches any Gitea release or package-publication step. A branch `workflow_dispatch` builds and validates all three jobs without creating a permanent release or publishing packages. Invalid `v*` versions fail before release creation.

The public pull mirror at `Robert/Suikoden-HD-Remaster-Save-Editor` was verified on 2026-08-25: it tracks the GitHub repository, uses `main`, has Actions/packages/releases enabled, and had synchronized the current branch and `v1.0.0` tag. Repository administration remains external to this source tree.

## Required Gitea settings

- Keep the pull mirror owned by `Robert` and pointed at the GitHub upstream.
- Keep Actions enabled and the `beelink` runner online with label `ubuntu-latest`.
- Allow the built-in workflow token repository content write access so it can create native releases and attach assets.
- Keep the user-level Actions secret named `PACKAGE_PUBLISH_TOKEN`; its `Robert` token needs package read/write permission. Repository-link permission is optional.
- Do not create another Arch or RPM repository. Publication targets the existing `robert` Arch repository and the owner's existing RPM registry.

## Safe release procedure

Published tags must never be moved or reused.

The normal path is the `Release` workflow in `.github/workflows/release.yml`, run from `main` with **Actions → Release → Run workflow**. Record what the release publishes under `## Unreleased` in `CHANGELOG.md` first; the workflow stamps that section with the new version and refuses to publish an empty one. Leave `version` blank to bump the chosen `bump` part of the project version, or set an explicit `X.Y.Z`. The workflow resolves the version, stamps `CHANGELOG.md`, confirms the tag is unused, runs the complete CI and package validation, builds and verifies both executables, commits `release: prepare vX.Y.Z` to `main`, pushes one annotated tag, and creates the non-draft GitHub release with the two assets. `dry_run` performs every check and build without pushing or publishing anything.

The workflow token cannot start another workflow run, so the tag it pushes does not re-trigger the `create-release` job in `ci.yml`; the release job publishes from the executables it just validated instead. Gitea builds and publishes the Arch and RPM packages after it mirrors the tag, which stays external administration.

Releasing by hand remains supported and is the fallback if the workflow is unavailable. Choose a new unused version and:

1. Pull `main` with `git pull --ff-only` and confirm `git status --short` is empty.
2. Update `Directory.Build.props` and `CHANGELOG.md` to the chosen `X.Y.Z`, run the complete local CI/package validation, commit, and push `main`. The RPM changelog obtains the version from the build and does not need a duplicated manual edit.
3. Confirm `git ls-remote --tags origin refs/tags/vX.Y.Z` returns no existing tag.
4. Create one annotated tag with `git tag -a vX.Y.Z -m "Suikoden I & II HD Remaster Save Editor vX.Y.Z"`.
5. Push only that tag with `git push origin vX.Y.Z`; do not force-push.
6. Create the non-draft GitHub release with the two locally verified executable assets. The GitHub tag workflow can create or update the same release without making a duplicate.
7. Ask Gitea to synchronize the pull mirror if an immediate run is desired, then inspect the tag-triggered workflow and package publication.

The workflow derives package version `X.Y.Z` from the tag and fixes Arch/RPM release to `1`. If any same-version package or release asset already exists with different bytes, publication stops instead of replacing it.

## Live-test boundary

Static workflow checks, local builds, isolated clean-distro package construction/installation, and manual non-publishing workflow behavior can be tested without permanent state. Native Gitea release creation and real registry publication require a new approved release tag, so that final boundary is deliberately not exercised during ordinary development.
