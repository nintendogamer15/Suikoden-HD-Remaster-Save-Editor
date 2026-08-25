# Packaging and Gitea releases

## Identity and outputs

The source application remains `SuikodenHdSaveEditor.App`. Linux packages install its complete self-contained bundle under `/usr/lib/suikoden-hd-remaster-save-editor` and provide `/usr/bin/suikoden-hd-remaster-save-editor` as a symlink to the real executable. This preserves the established .NET assembly/executable identity while providing the requested distro command.

For a valid `vX.Y.Z` tag, the Gitea workflow produces:

- `SuikodenHdSaveEditor-vX.Y.Z-linux-x64.tar.gz`
- `SuikodenHdSaveEditor-vX.Y.Z-windows-x64.zip`
- `SHA256SUMS.txt`
- `suikoden-hd-remaster-save-editor-X.Y.Z-1-x86_64.pkg.tar.zst`
- `suikoden-hd-remaster-save-editor-X.Y.Z-1-x86_64.pkg.tar.zst.sha256`
- `suikoden-hd-remaster-save-editor-X.Y.Z-1.x86_64.rpm`
- `suikoden-hd-remaster-save-editor-X.Y.Z-1.x86_64.rpm.sha256`

The Arch and RPM packages also install the desktop entry, scalable icon, root license, complete third-party license directory, notices, and README in normal system locations. Neither package depends on a system .NET runtime.

## Native dependency evidence

The `linux-x64` publish was inspected with `file`, `readelf`, and `ldd`, including its bundled .NET, SkiaSharp, and HarfBuzz native libraries. Its direct ELF requirements cover glibc, libgcc/libstdc++, fontconfig and its image/font stack, while .NET and Avalonia dynamically load ICU, OpenSSL, Kerberos, X11, ICE/SM, timezone, and zlib facilities. The dependency names were then compared with the working FFIX and FFIV Avalonia packages and are verified by installing and launching each package in a clean distro job. Although `Avalonia.Fonts.Inter` is bundled, clean-system startup still needs a discoverable default system font; the packages therefore install DejaVu Sans. They do not add a system .NET dependency.

The self-contained .NET publish also carries the optional diagnostics-only `libcoreclrtraceptprovider.so`, which still declares the retired `liblttng-ust.so.0` ABI. Current Fedora provides `liblttng-ust.so.1`, and the editor does not load the diagnostics provider during normal execution. The RPM therefore excludes only that obsolete automatic requirement; all application startup dependencies remain enforced and are tested by DNF installation plus the Xvfb smoke launch.

## Checked-in package tooling

- `build-arch-package.sh` stages the existing Linux publish into a real `makepkg` build.
- `build-rpm-package.sh` builds the same publish through `rpmbuild` and the checked-in spec.
- `validate-package-input.sh` rejects private saves, references, tests, build products, package files, debug symbols, and package infrastructure strings.
- `validate-installed-package.sh` checks the installed command target, application bundle, licenses, desktop file, icon, architecture, ELF resolution, and forbidden content.
- `gitea-release-assets.sh` creates a native Gitea release and treats existing release assets as immutable.
- `gitea-publish-package.sh` authenticates package operations as `Robert`, safely skips byte-identical existing packages, rejects different bytes for an existing version, and treats optional repository linking failures as warnings.

`PACKAGE_PUBLISH_TOKEN` is used only for registry API and upload operations. The built-in `GITEA_TOKEN` is used for source checkout, release API operations, and release assets. The package upload base is supplied separately through `GITEA_PACKAGE_SERVER_URL`; it is intentionally not an end-user URL.

## Workflow behavior

`.gitea/workflows/ci.yml` runs normal CI on `main`, pull requests when available, and manual dispatches. `.gitea/workflows/release.yml` runs three isolated jobs on the `ubuntu-latest` runner label:

1. the .NET 10 container tests, cross-publishes Windows/Linux, smoke-tests Linux, audits payloads, and creates release assets;
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

Published tags must never be moved or reused. For another release, choose a new unused version and:

1. Pull `main` with `git pull --ff-only` and confirm `git status --short` is empty.
2. Update `Directory.Build.props` and `CHANGELOG.md` to the chosen `X.Y.Z`, commit, push `main`, and wait for GitHub CI plus mirrored Gitea CI to pass. The RPM changelog obtains the version from the build and does not need a duplicated manual edit.
3. Confirm `git ls-remote --tags origin refs/tags/vX.Y.Z` returns no existing tag.
4. Create one annotated tag with `git tag -a vX.Y.Z -m "Suikoden I & II HD Remaster Save Editor vX.Y.Z"`.
5. Push only that tag with `git push origin vX.Y.Z`; do not force-push.
6. Ask Gitea to synchronize the pull mirror if an immediate run is desired, then verify the tag-triggered workflow before announcing the release.

The workflow derives package version `X.Y.Z` from the tag and fixes Arch/RPM release to `1`. If any same-version package or release asset already exists with different bytes, publication stops instead of replacing it.

## Live-test boundary

Static workflow checks, local builds, isolated clean-distro package construction/installation, and manual non-publishing workflow behavior can be tested without permanent state. Native Gitea release creation and real registry publication require a new approved release tag, so that final boundary is deliberately not exercised during ordinary development.
