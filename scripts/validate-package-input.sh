#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <linux-publish-directory>" >&2
    exit 2
fi

bundle="$(realpath "$1")"
[[ -d $bundle ]] || { echo "Linux publish directory does not exist: $bundle" >&2; exit 2; }
test -x "$bundle/SuikodenHdSaveEditor.App"
test -s "$bundle/LICENSE"
test -s "$bundle/README.md"
test -s "$bundle/THIRD_PARTY_NOTICES.md"
test -s "$bundle/LICENSES/Avalonia-MIT.txt"
test -s "$bundle/LICENSES/dotnet-THIRD-PARTY-NOTICES.txt"
file "$bundle/SuikodenHdSaveEditor.App" | grep -q 'ELF 64-bit.*x86-64'

forbidden_path="$(find "$bundle" -mindepth 1 -printf '%P\n' | grep -E '(^|/)(saves|reference|tests|TestResults|\.git|\.github|\.gitea|packaging)(/|$)|(^|/)(Data([0-9]|1[0-6])|_sharetmpsave0)$|\.(bak|pdb|pkg\.tar\.zst|rpm|snupkg|nupkg|trx)$' | head -1 || true)"
if [[ -n $forbidden_path ]]; then
    echo "Development, private, or generated content was found in package input: $forbidden_path" >&2
    exit 1
fi

if grep -RIl --exclude='*.dll' --exclude='*.so' --exclude='SuikodenHdSaveEditor.App' \
    -E '192\.168\.1\.75|PACKAGE_PUBLISH_TOKEN' "$bundle" | grep -q .; then
    echo "An internal package endpoint or secret name was found in package input." >&2
    exit 1
fi
