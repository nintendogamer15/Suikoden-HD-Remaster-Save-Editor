#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <vX.Y.Z|manual-COMMIT> <output-directory>" >&2
    exit 2
fi

source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"
require_commands file find grep install realpath sort
label="$1"
output_directory="$(realpath -m "$2")"
if [[ $label == v* ]]; then
    "$SCRIPT_DIRECTORY/project-version.sh" "$label" >/dev/null
elif [[ ! $label =~ ^manual-[0-9a-fA-F]{7,40}$ ]]; then
    echo "Release label must be vX.Y.Z or manual-COMMIT, got: $label" >&2
    exit 2
fi

linux_source="$LINUX_PUBLISH_DIRECTORY/$APP_EXECUTABLE"
windows_source="$WINDOWS_PUBLISH_DIRECTORY/$APP_EXECUTABLE.exe"
assert_single_file_publish "$LINUX_PUBLISH_DIRECTORY" "$APP_EXECUTABLE"
assert_single_file_publish "$WINDOWS_PUBLISH_DIRECTORY" "$APP_EXECUTABLE.exe"
mkdir -p "$output_directory"
if find "$output_directory" -mindepth 1 -print -quit | grep -q .; then
    echo "Release output directory must be empty: $output_directory" >&2
    exit 1
fi

linux_asset="$output_directory/SuikodenHdSaveEditor-$label-linux-x64"
windows_asset="$output_directory/SuikodenHdSaveEditor-$label-windows-x64.exe"
install -m 0755 "$linux_source" "$linux_asset"
install -m 0644 "$windows_source" "$windows_asset"

file "$linux_asset" | grep -q 'ELF 64-bit.*x86-64'
file "$windows_asset" | grep -q 'PE32+ executable.*x86-64'
mapfile -t release_entries < <(find "$output_directory" -mindepth 1 -maxdepth 1 -printf '%f\n' | sort)
[[ ${#release_entries[@]} -eq 2 ]] || {
    echo "Release preparation must produce exactly two executable assets." >&2
    exit 1
}

printf '%s\n' "$linux_asset" "$windows_asset"
