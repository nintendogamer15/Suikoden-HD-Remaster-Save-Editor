#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

required_licenses=(
    "Avalonia-MIT.txt"
    "ANGLE-BSD-3-Clause.txt"
    "HarfBuzzSharp-MIT.txt"
    "Inter-OFL-1.1.txt"
    "MicroCom-MIT.txt"
    "SkiaSharp-MIT.txt"
    "Suikoden-Fix-MIT.txt"
    "SuikodenSaveDecrypter-MIT.txt"
    "Tmds.DBus-MIT.txt"
    "dotnet-MIT.txt"
    "dotnet-THIRD-PARTY-NOTICES.txt"
    "ffix-save-editor-MIT.txt"
    "suisaveeditor-MIT.txt"
)

for license in "${required_licenses[@]}"; do
    test -s "$REPOSITORY_ROOT/LICENSES/$license"
done

for bundle in "$LINUX_PUBLISH_DIRECTORY" "$WINDOWS_PUBLISH_DIRECTORY"; do
    test -s "$bundle/LICENSE"
    test -s "$bundle/README.md"
    test -s "$bundle/THIRD_PARTY_NOTICES.md"
    for license in "${required_licenses[@]}"; do
        test -s "$bundle/LICENSES/$license"
    done
done

if find "$ARTIFACTS_DIRECTORY/publish" -type f \( -name 'Data0' -o -name 'Data1' -o -name 'Data2' -o -name 'Data3' -o -name 'Data4' -o -name 'Data5' -o -name 'Data16' -o -name '_sharetmpsave0' \) -print -quit | grep -q .; then
    echo "A private-save filename was found in publish output." >&2
    exit 1
fi
