#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

mkdir -p "$ARTIFACTS_DIRECTORY/publish"
if [[ -d "$LINUX_PUBLISH_DIRECTORY" ]]; then
    find "$LINUX_PUBLISH_DIRECTORY" -mindepth 1 -delete
fi

dotnet publish "$APP_PROJECT" \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --no-restore \
    --output "$LINUX_PUBLISH_DIRECTORY" \
    -p:DebugType=None \
    -p:DebugSymbols=false

find "$LINUX_PUBLISH_DIRECTORY" -type f -name '*.pdb' -delete
stage_legal_files "$LINUX_PUBLISH_DIRECTORY"
test -x "$LINUX_PUBLISH_DIRECTORY/SuikodenHdSaveEditor.App"
