#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

linux_archive="$ARTIFACTS_DIRECTORY/$LINUX_BUNDLE_NAME.tar.gz"
windows_archive="$ARTIFACTS_DIRECTORY/$WINDOWS_BUNDLE_NAME.zip"
test -s "$linux_archive"
test -s "$windows_archive"

(
    cd "$ARTIFACTS_DIRECTORY"
    sha256sum "$(basename "$linux_archive")" "$(basename "$windows_archive")" > SHA256SUMS.txt
)
