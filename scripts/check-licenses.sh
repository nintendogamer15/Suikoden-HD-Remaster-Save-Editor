#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
REPOSITORY_ROOT="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
ARTIFACTS_DIRECTORY="$REPOSITORY_ROOT/artifacts"

for tool in find grep; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for license and publish-content validation." >&2
        exit 1
    fi
done

required_licenses=(
    "Avalonia-MIT.txt"
    "CommunityToolkit.Mvvm-MIT.txt"
    "ANGLE-BSD-3-Clause.txt"
    "HarfBuzzSharp-MIT.txt"
    "Inter-OFL-1.1.txt"
    "MicroCom-MIT.txt"
    "SaveEditor.Ui-0BSD.txt"
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

test -s "$REPOSITORY_ROOT/LICENSE"
test -s "$REPOSITORY_ROOT/README.md"
test -s "$REPOSITORY_ROOT/THIRD_PARTY_NOTICES.md"

if [[ -d $ARTIFACTS_DIRECTORY/publish ]] && find "$ARTIFACTS_DIRECTORY/publish" -type f \( -name 'Data0' -o -name 'Data1' -o -name 'Data2' -o -name 'Data3' -o -name 'Data4' -o -name 'Data5' -o -name 'Data16' -o -name '_sharetmpsave0' \) -print -quit | grep -q .; then
    echo "A private-save filename was found in publish output." >&2
    exit 1
fi
