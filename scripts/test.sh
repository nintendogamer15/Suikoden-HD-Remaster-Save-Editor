#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

dotnet test "$SOLUTION" --configuration Release --no-restore --logger "console;verbosity=normal"
