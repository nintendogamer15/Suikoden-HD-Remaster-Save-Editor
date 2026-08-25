#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <linux-publish-directory>" >&2
    exit 2
fi

bundle="$(realpath "$1")"
[[ -d $bundle ]] || { echo "Linux publish directory does not exist: $bundle" >&2; exit 2; }

for tool in file find grep realpath sort stat; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for Linux package-input validation." >&2
        exit 1
    fi
done

mapfile -t entries < <(find "$bundle" -mindepth 1 -maxdepth 1 -printf '%f\n' | sort)
if [[ ${#entries[@]} -ne 1 || ${entries[0]:-} != SuikodenHdSaveEditor.App ]]; then
    echo "Linux package input must contain only the single-file SuikodenHdSaveEditor.App executable." >&2
    printf 'Found: %s\n' "${entries[*]:-<nothing>}" >&2
    exit 1
fi
test -f "$bundle/SuikodenHdSaveEditor.App"
mode="$(stat -c '%a' "$bundle/SuikodenHdSaveEditor.App")"
if (( (8#$mode & 0111) == 0 )); then
    echo "Linux package input is not marked executable: $bundle/SuikodenHdSaveEditor.App" >&2
    exit 1
fi
file "$bundle/SuikodenHdSaveEditor.App" | grep -q 'ELF 64-bit.*x86-64'

forbidden_path="$(find "$bundle" -mindepth 1 -printf '%P\n' | grep -E '(^|/)(saves|reference|tests|TestResults|\.git|\.github|\.gitea|packaging)(/|$)|(^|/)(Data([0-9]|1[0-6])|_sharetmpsave0)$|\.(bak|pdb|pkg\.tar\.zst|rpm|snupkg|nupkg|trx)$' | head -1 || true)"
if [[ -n $forbidden_path ]]; then
    echo "Development, private, or generated content was found in package input: $forbidden_path" >&2
    exit 1
fi

if grep -aIl -E '192\.168\.1\.75|PACKAGE_PUBLISH_TOKEN' \
    "$bundle/SuikodenHdSaveEditor.App" | grep -q .; then
    echo "An internal package endpoint or secret name was found in package input." >&2
    exit 1
fi
