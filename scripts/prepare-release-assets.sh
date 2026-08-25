#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <vX.Y.Z|manual-COMMIT> <output-directory>" >&2
    exit 2
fi

source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"
label="$1"
output_directory="$(realpath -m "$2")"
if [[ $label == v* ]]; then
    "$SCRIPT_DIRECTORY/project-version.sh" "$label" >/dev/null
elif [[ ! $label =~ ^manual-[0-9a-fA-F]{7,40}$ ]]; then
    echo "Release label must be vX.Y.Z or manual-COMMIT, got: $label" >&2
    exit 2
fi

linux_source="$ARTIFACTS_DIRECTORY/$LINUX_BUNDLE_NAME.tar.gz"
windows_source="$ARTIFACTS_DIRECTORY/$WINDOWS_BUNDLE_NAME.zip"
test -s "$linux_source"
test -s "$windows_source"
mkdir -p "$output_directory"

linux_asset="$output_directory/SuikodenHdSaveEditor-$label-linux-x64.tar.gz"
windows_asset="$output_directory/SuikodenHdSaveEditor-$label-windows-x64.zip"
install -m 0644 "$linux_source" "$linux_asset"
install -m 0644 "$windows_source" "$windows_asset"

printf '%s\n' "$linux_asset" "$windows_asset"
