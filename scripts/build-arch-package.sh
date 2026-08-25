#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ ${EUID} -eq 0 ]]; then
    echo "makepkg must run as a non-root user." >&2
    exit 1
fi
if [[ $# -ne 3 ]]; then
    echo "Usage: $0 <vX.Y.Z|X.Y.Z> <linux-publish-directory> <output-directory>" >&2
    exit 2
fi

repository_root="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
version="${1#v}"
bundle="$(realpath "$2")"
output_directory="$(realpath -m "$3")"
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid package version: $version" >&2; exit 2; }
[[ -d $bundle ]] || { echo "Linux publish directory does not exist: $bundle" >&2; exit 2; }
"$repository_root/scripts/validate-package-input.sh" "$bundle"
for tool in install makepkg mktemp realpath tar; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required to build the Arch package." >&2
        exit 1
    fi
done

temporary_root="${TMPDIR:-$repository_root/.tools/package-tmp}"
mkdir -p "$temporary_root" "$output_directory"
work_directory="$(mktemp -d "$temporary_root/suikoden-arch-package.XXXXXXXX")"
trap 'rm -rf -- "$work_directory"' EXIT

install -m 0644 "$repository_root/packaging/arch/PKGBUILD" "$work_directory/PKGBUILD"
install -m 0644 "$repository_root/packaging/linux/suikoden-hd-remaster-save-editor.desktop" \
    "$work_directory/suikoden-hd-remaster-save-editor.desktop"
install -m 0644 "$repository_root/packaging/linux/suikoden-hd-remaster-save-editor.svg" \
    "$work_directory/suikoden-hd-remaster-save-editor.svg"
"$repository_root/scripts/stage-package-files.sh" "$bundle" "$work_directory/app-bundle"
tar --directory "$work_directory" --create --gzip --file "$work_directory/app-bundle.tar.gz" app-bundle

(
    cd "$work_directory"
    PKGVER="$version" PKGDEST="$output_directory" makepkg --cleanbuild --clean --noconfirm --nodeps
)

package="$output_directory/suikoden-hd-remaster-save-editor-$version-1-x86_64.pkg.tar.zst"
[[ -f $package ]] || { echo "Expected Arch package was not produced: $package" >&2; exit 1; }
printf '%s\n' "$package"
