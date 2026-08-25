#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

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

temporary_root="${TMPDIR:-$repository_root/.tools/package-tmp}"
mkdir -p "$temporary_root" "$output_directory"
top_directory="$(mktemp -d "$temporary_root/suikoden-rpm-package.XXXXXXXX")"
trap 'rm -rf -- "$top_directory"' EXIT
mkdir -p "$top_directory"/{BUILD,BUILDROOT,RPMS,SOURCES,SPECS,SRPMS,app-bundle}
cp -a "$bundle/." "$top_directory/app-bundle/"
tar --directory "$top_directory" --create --gzip --file "$top_directory/SOURCES/app-bundle.tar.gz" app-bundle
install -m 0644 "$repository_root/packaging/linux/suikoden-hd-remaster-save-editor.desktop" \
    "$top_directory/SOURCES/suikoden-hd-remaster-save-editor.desktop"
install -m 0644 "$repository_root/packaging/linux/suikoden-hd-remaster-save-editor.svg" \
    "$top_directory/SOURCES/suikoden-hd-remaster-save-editor.svg"
install -m 0644 "$repository_root/packaging/rpm/suikoden-hd-remaster-save-editor.spec" \
    "$top_directory/SPECS/suikoden-hd-remaster-save-editor.spec"

rpmbuild -bb --define "_topdir $top_directory" --define "app_version $version" \
    "$top_directory/SPECS/suikoden-hd-remaster-save-editor.spec"

package="$(find "$top_directory/RPMS/x86_64" -maxdepth 1 -type f -name 'suikoden-hd-remaster-save-editor-*.x86_64.rpm' -print -quit)"
[[ -n $package ]] || { echo "Expected RPM package was not produced." >&2; exit 1; }
destination="$output_directory/$(basename "$package")"
install -m 0644 "$package" "$destination"
printf '%s\n' "$destination"
