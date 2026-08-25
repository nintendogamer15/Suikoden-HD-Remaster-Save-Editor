#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"
require_commands file find grep sort

"$SCRIPT_DIRECTORY/check-licenses.sh"
assert_single_file_publish "$LINUX_PUBLISH_DIRECTORY" "$APP_EXECUTABLE"
assert_single_file_publish "$WINDOWS_PUBLISH_DIRECTORY" "$APP_EXECUTABLE.exe"

file "$LINUX_PUBLISH_DIRECTORY/$APP_EXECUTABLE" | grep -q 'ELF 64-bit.*x86-64'
file "$WINDOWS_PUBLISH_DIRECTORY/$APP_EXECUTABLE.exe" | grep -q 'PE32+ executable.*x86-64'

echo "Verified executable-only Linux and Windows publish outputs."
