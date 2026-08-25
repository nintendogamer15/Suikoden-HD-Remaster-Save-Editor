#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Adapted from ffix-save-editor.
# Copyright (c) 2026 ffix-save-editor contributors

set -euo pipefail

if [[ $# -lt 2 ]]; then
    echo "Usage: $0 <vX.Y.Z> <asset> [asset ...]" >&2
    exit 2
fi

for tool in curl jq mktemp sha256sum; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for Gitea release publication." >&2
        exit 1
    fi
done

tag="$1"
shift
[[ $tag =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid release tag: $tag" >&2; exit 2; }
: "${GITEA_TOKEN:?GITEA_TOKEN is required}"
: "${GITEA_SERVER_URL:?GITEA_SERVER_URL is required}"
: "${REPOSITORY_NAME:?REPOSITORY_NAME is required}"

api="${GITEA_SERVER_URL%/}/api/v1/repos/$REPOSITORY_NAME"
authorization="Authorization: token $GITEA_TOKEN"
work_directory="$(mktemp -d "${TMPDIR:-/tmp}/gitea-release-assets.XXXXXXXX")"
trap 'rm -rf -- "$work_directory"' EXIT

release_json="$work_directory/release.json"
status="$(curl --silent --show-error --output "$release_json" --write-out '%{http_code}' \
    --header "$authorization" "$api/releases/tags/$tag")"
if [[ $status == 404 ]]; then
    payload="$(jq -n --arg tag "$tag" \
        '{tag_name:$tag,name:$tag,body:"Built independently by Gitea Actions from the mirrored source tag.",draft:false,prerelease:false}')"
    status="$(curl --silent --show-error --output "$release_json" --write-out '%{http_code}' \
        --request POST --header "$authorization" --header 'Content-Type: application/json' \
        --data "$payload" "$api/releases")"
    if [[ $status != 201 ]]; then
        echo "Gitea release creation failed with HTTP $status:" >&2
        cat "$release_json" >&2
        exit 1
    fi
elif [[ $status != 200 ]]; then
    echo "Gitea release lookup failed with HTTP $status:" >&2
    cat "$release_json" >&2
    exit 1
fi

release_id="$(jq -er '.id' "$release_json")"
for asset in "$@"; do
    [[ -f $asset ]] || { echo "Release asset does not exist: $asset" >&2; exit 2; }
    name="$(basename "$asset")"
    [[ $name =~ ^[A-Za-z0-9._-]+$ ]] || { echo "Unsafe release asset name: $name" >&2; exit 2; }
    local_sha="$(sha256sum "$asset" | cut -d ' ' -f 1)"

    assets_json="$work_directory/assets.json"
    status="$(curl --silent --show-error --output "$assets_json" --write-out '%{http_code}' \
        --header "$authorization" "$api/releases/$release_id/assets")"
    if [[ $status != 200 ]]; then
        echo "Gitea release asset lookup failed with HTTP $status:" >&2
        cat "$assets_json" >&2
        exit 1
    fi
    existing_url="$(jq -r --arg name "$name" '[.[] | select(.name == $name)][0].browser_download_url // empty' "$assets_json")"

    if [[ -n $existing_url ]]; then
        existing_file="$work_directory/existing-$name"
        curl --silent --show-error --fail --location --header "$authorization" \
            --output "$existing_file" "$existing_url"
        existing_sha="$(sha256sum "$existing_file" | cut -d ' ' -f 1)"
        if [[ $existing_sha == "$local_sha" ]]; then
            echo "Release asset $name already exists with matching SHA-256; skipping."
            continue
        fi
        echo "Release asset $name already exists with a different SHA-256; refusing to replace it." >&2
        echo "existing=$existing_sha built=$local_sha" >&2
        exit 1
    fi

    response="$work_directory/upload-$name.json"
    status="$(curl --silent --show-error --output "$response" --write-out '%{http_code}' \
        --request POST --header "$authorization" --form "attachment=@$asset" \
        "$api/releases/$release_id/assets?name=$name")"
    if [[ $status != 201 ]]; then
        echo "Gitea release asset upload failed with HTTP $status:" >&2
        cat "$response" >&2
        exit 1
    fi
    echo "Uploaded release asset $name."
done
