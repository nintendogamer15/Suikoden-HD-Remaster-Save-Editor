#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <package-file>" >&2
    exit 2
fi

for tool in realpath sha256sum; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for package checksum validation." >&2
        exit 1
    fi
done

package="$(realpath "$1")"
[[ -f $package ]] || { echo "Package does not exist: $package" >&2; exit 2; }
checksum="$package.sha256"
(
    cd "$(dirname "$package")"
    sha256sum "$(basename "$package")" > "$(basename "$checksum")"
)
printf '%s\n' "$checksum"
