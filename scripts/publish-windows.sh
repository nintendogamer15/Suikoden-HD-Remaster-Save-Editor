#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"
require_commands find sort

mkdir -p "$ARTIFACTS_DIRECTORY/publish"
if [[ -d "$WINDOWS_PUBLISH_DIRECTORY" ]]; then
    find "$WINDOWS_PUBLISH_DIRECTORY" -mindepth 1 -delete
fi

dotnet publish "$APP_PROJECT" \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --no-restore \
    --output "$WINDOWS_PUBLISH_DIRECTORY" \
    -p:Version="$APP_VERSION" \
    -p:SelfContained=true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false \
    -p:DebugType=None \
    -p:DebugSymbols=false

find "$WINDOWS_PUBLISH_DIRECTORY" -type f -name '*.pdb' -delete
assert_single_file_publish "$WINDOWS_PUBLISH_DIRECTORY" "$APP_EXECUTABLE.exe"
