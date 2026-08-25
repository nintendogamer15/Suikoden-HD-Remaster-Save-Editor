#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

"$SCRIPT_DIRECTORY/check-licenses.sh"
if ! command -v zip >/dev/null 2>&1; then
    echo "zip is required to create the Windows archive." >&2
    exit 1
fi

linux_archive="$ARTIFACTS_DIRECTORY/$LINUX_BUNDLE_NAME.tar.gz"
windows_archive="$ARTIFACTS_DIRECTORY/$WINDOWS_BUNDLE_NAME.zip"
find "$ARTIFACTS_DIRECTORY" -maxdepth 1 -type f \( -name '*.tar.gz' -o -name '*.zip' \) -delete

tar --directory "$ARTIFACTS_DIRECTORY/publish" --create --gzip --file "$linux_archive" "$LINUX_BUNDLE_NAME"
(
    cd "$ARTIFACTS_DIRECTORY/publish"
    zip -q -r "$windows_archive" "$WINDOWS_BUNDLE_NAME"
)
