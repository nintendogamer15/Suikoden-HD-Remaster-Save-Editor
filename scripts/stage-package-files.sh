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

for tool in cp install mkdir realpath; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required to stage Linux package files." >&2
        exit 1
    fi
done

"$repository_root/scripts/check-licenses.sh"
"$repository_root/scripts/validate-package-input.sh" "$bundle"

mkdir -p "$staging_directory/LICENSES"
install -m 0755 "$bundle/SuikodenHdSaveEditor.App" \
    "$staging_directory/SuikodenHdSaveEditor.App"
install -m 0644 "$repository_root/LICENSE" "$staging_directory/LICENSE"
install -m 0644 "$repository_root/README.md" "$staging_directory/README.md"
install -m 0644 "$repository_root/THIRD_PARTY_NOTICES.md" \
    "$staging_directory/THIRD_PARTY_NOTICES.md"
cp -R "$repository_root/LICENSES/." "$staging_directory/LICENSES/"
