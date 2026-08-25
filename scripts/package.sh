#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

linux_archive="$ARTIFACTS_DIRECTORY/$LINUX_BUNDLE_NAME.tar.gz"
windows_archive="$ARTIFACTS_DIRECTORY/$WINDOWS_BUNDLE_NAME.zip"
"$SCRIPT_DIRECTORY/archive.sh"
"$SCRIPT_DIRECTORY/checksums.sh"

tar --list --file "$linux_archive" "$LINUX_BUNDLE_NAME/LICENSES/Avalonia-MIT.txt" >/dev/null
unzip -t "$windows_archive" "$WINDOWS_BUNDLE_NAME/LICENSES/Avalonia-MIT.txt" >/dev/null
