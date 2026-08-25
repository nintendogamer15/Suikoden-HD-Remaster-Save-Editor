#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

repository_root="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if ! command -v sed >/dev/null 2>&1; then
    echo "sed is required to read the project version." >&2
    exit 1
fi
if [[ $# -gt 1 ]]; then
    echo "Usage: $0 [vX.Y.Z]" >&2
    exit 2
fi

if [[ $# -eq 1 ]]; then
    tag="$1"
    if [[ ! $tag =~ ^v([0-9]+\.[0-9]+\.[0-9]+)$ ]]; then
        echo "Release tag must match vX.Y.Z, got: $tag" >&2
        exit 2
    fi
    printf '%s\n' "${BASH_REMATCH[1]}"
    exit 0
fi

version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$repository_root/Directory.Build.props")"
if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Directory.Build.props must contain one X.Y.Z Version value, got: ${version:-<missing>}" >&2
    exit 2
fi
printf '%s\n' "$version"
