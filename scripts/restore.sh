#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

# SaveEditor.Ui is consumed as a submodule, so a checkout that has not initialised it has an
# empty directory where the ProjectReference points. Gitea runners check out by hand and never
# fetch submodules, so this cannot be left to the checkout step.
if git -C "$REPOSITORY_ROOT" rev-parse --git-dir >/dev/null 2>&1; then
    git -C "$REPOSITORY_ROOT" submodule update --init --depth 1
fi

dotnet restore "$SOLUTION" --locked-mode
