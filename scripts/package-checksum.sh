#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <package-file>" >&2
    exit 2
fi

package="$(realpath "$1")"
[[ -f $package ]] || { echo "Package does not exist: $package" >&2; exit 2; }
checksum="$package.sha256"
(
    cd "$(dirname "$package")"
    sha256sum "$(basename "$package")" > "$(basename "$checksum")"
)
printf '%s\n' "$checksum"
