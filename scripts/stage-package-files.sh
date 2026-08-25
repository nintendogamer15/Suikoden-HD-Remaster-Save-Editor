#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <linux-publish-directory> <staging-directory>" >&2
    exit 2
fi

repository_root="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
bundle="$(realpath "$1")"
staging_directory="$(realpath -m "$2")"

for tool in find install realpath; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required to stage Linux package files." >&2
        exit 1
    fi
done

"$repository_root/scripts/check-licenses.sh"
"$repository_root/scripts/validate-package-input.sh" "$bundle"

# This tree is archived verbatim and unpacked into the distro build roots, so
# every mode here reaches the published packages. `mkdir` and `cp` would apply
# the caller's umask, producing 0775 directories and 0664 files under the 002
# umask CI builds with, which rpmlint rejects as non-standard permissions.
install -d -m 0755 "$staging_directory" "$staging_directory/LICENSES"
install -m 0755 "$bundle/SuikodenHdSaveEditor.App" \
    "$staging_directory/SuikodenHdSaveEditor.App"
install -m 0644 "$repository_root/LICENSE" "$staging_directory/LICENSE"
install -m 0644 "$repository_root/README.md" "$staging_directory/README.md"
install -m 0644 "$repository_root/THIRD_PARTY_NOTICES.md" \
    "$staging_directory/THIRD_PARTY_NOTICES.md"
while IFS= read -r -d '' license_file; do
    install -m 0644 "$license_file" "$staging_directory/LICENSES/"
done < <(find "$repository_root/LICENSES" -mindepth 1 -maxdepth 1 -type f -print0)
