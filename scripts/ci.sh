#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

"$SCRIPT_DIRECTORY/restore.sh"
"$SCRIPT_DIRECTORY/check-workflows.sh"
"$SCRIPT_DIRECTORY/check-packaging.sh"
"$SCRIPT_DIRECTORY/check-format.sh"
"$SCRIPT_DIRECTORY/build.sh"
"$SCRIPT_DIRECTORY/test.sh"
"$SCRIPT_DIRECTORY/publish-linux.sh"
"$SCRIPT_DIRECTORY/publish-windows.sh"
"$SCRIPT_DIRECTORY/check-linux-native-dependencies.sh"
"$SCRIPT_DIRECTORY/smoke-test.sh"
"$SCRIPT_DIRECTORY/package.sh"
