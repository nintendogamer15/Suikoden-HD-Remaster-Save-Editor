#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

actionlint_version="1.7.12"
actionlint_checksum="8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8"
actionlint_directory="$REPOSITORY_ROOT/.tools/actionlint"
actionlint_binary="$actionlint_directory/actionlint"

if command -v actionlint >/dev/null 2>&1; then
    actionlint_binary="$(command -v actionlint)"
elif [[ ! -x "$actionlint_binary" ]]; then
    archive="$actionlint_directory/actionlint.tar.gz"
    mkdir -p "$actionlint_directory"
    curl --fail --location --silent --show-error \
        --output "$archive" \
        "https://github.com/rhysd/actionlint/releases/download/v${actionlint_version}/actionlint_${actionlint_version}_linux_amd64.tar.gz"
    printf '%s  %s\n' "$actionlint_checksum" "$archive" | sha256sum --check --status
    tar --extract --gzip --file "$archive" --directory "$actionlint_directory" actionlint
fi

"$actionlint_binary" \
    "$REPOSITORY_ROOT/.github/workflows/ci.yml" \
    "$REPOSITORY_ROOT/.gitea/workflows/ci.yml"
