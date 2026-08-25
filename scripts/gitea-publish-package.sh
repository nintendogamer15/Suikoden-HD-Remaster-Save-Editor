#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Adapted from ffix-save-editor.
# Copyright (c) 2026 ffix-save-editor contributors

set -euo pipefail

if [[ $# -ne 6 ]]; then
    echo "Usage: $0 <arch|rpm> <owner> <arch-repository|root> <package-name> <X.Y.Z> <package-file>" >&2
    exit 2
fi

for tool in curl jq mktemp realpath sha256sum; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for Gitea package publication." >&2
        exit 1
    fi
done

package_type="$1"
owner="$2"
registry="$3"
package_name="$4"
version="$5"
package_file="$(realpath "$6")"
[[ $package_type == arch || $package_type == rpm ]] || { echo "Unsupported package type: $package_type" >&2; exit 2; }
[[ $owner == Robert ]] || { echo "This project publishes packages only under the Robert owner." >&2; exit 2; }
[[ $package_name == suikoden-hd-remaster-save-editor ]] || { echo "Unexpected package name: $package_name" >&2; exit 2; }
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid package version: $version" >&2; exit 2; }
[[ -f $package_file ]] || { echo "Package file does not exist: $package_file" >&2; exit 2; }
if [[ $package_type == arch ]]; then
    [[ $registry == robert ]] || { echo "Arch packages must use the existing robert repository." >&2; exit 2; }
else
    [[ $registry == root ]] || { echo "RPM packages must use the root registry." >&2; exit 2; }
fi

: "${PACKAGE_PUBLISH_TOKEN:?PACKAGE_PUBLISH_TOKEN is required}"
: "${GITEA_PACKAGE_SERVER_URL:?GITEA_PACKAGE_SERVER_URL is required}"
: "${REPOSITORY_NAME:?REPOSITORY_NAME is required}"

server="${GITEA_PACKAGE_SERVER_URL%/}"
api="$server/api/v1"
package_user="Robert"
filename="$(basename "$package_file")"
local_sha="$(sha256sum "$package_file" | cut -d ' ' -f 1)"
registry_version="$version-1"
work_directory="$(mktemp -d "${TMPDIR:-/tmp}/gitea-package-publish.XXXXXXXX")"
trap 'rm -rf -- "$work_directory"' EXIT

check_existing() {
    local body status existing_sha
    body="$work_directory/files.json"
    status="$(curl --silent --show-error --output "$body" --write-out '%{http_code}' \
        --user "$package_user:$PACKAGE_PUBLISH_TOKEN" \
        "$api/packages/$owner/$package_type/$package_name/$registry_version/files")"
    if [[ $status == 404 ]]; then
        return 1
    fi
    if [[ $status != 200 ]]; then
        echo "Gitea package lookup failed with HTTP $status:" >&2
        cat "$body" >&2
        exit 1
    fi
    existing_sha="$(jq -r --arg name "$filename" '[.[] | select(.name == $name)][0].sha256 // empty' "$body")"
    if [[ -z $existing_sha ]]; then
        echo "Package $package_type/$package_name $registry_version exists without $filename; refusing to modify it." >&2
        exit 1
    fi
    if [[ $existing_sha != "$local_sha" ]]; then
        echo "Package $filename already exists with a different SHA-256; refusing to replace it." >&2
        echo "existing=$existing_sha built=$local_sha" >&2
        exit 1
    fi
    echo "Package $filename already exists with matching SHA-256; skipping upload."
    return 0
}

if ! check_existing; then
    if [[ $package_type == arch ]]; then
        upload_url="$server/api/packages/$owner/arch/$registry"
    else
        upload_url="$server/api/packages/$owner/rpm/upload"
    fi
    response="$work_directory/upload-response.txt"
    status="$(curl --silent --show-error --output "$response" --write-out '%{http_code}' \
        --request PUT --user "$package_user:$PACKAGE_PUBLISH_TOKEN" \
        --upload-file "$package_file" "$upload_url")"
    if [[ $status != 201 && $status != 409 ]]; then
        echo "Gitea $package_type upload failed with HTTP $status:" >&2
        cat "$response" >&2
        exit 1
    fi
    verified=false
    for _ in 1 2 3 4 5; do
        if check_existing; then
            verified=true
            break
        fi
        sleep 2
    done
    [[ $verified == true ]] || { echo "Uploaded package was not visible through the Gitea package API." >&2; exit 1; }
fi

details="$work_directory/package.json"
status="$(curl --silent --show-error --output "$details" --write-out '%{http_code}' \
    --user "$package_user:$PACKAGE_PUBLISH_TOKEN" \
    "$api/packages/$owner/$package_type/$package_name/$registry_version")"
if [[ $status != 200 ]]; then
    echo "Gitea package detail lookup failed with HTTP $status:" >&2
    cat "$details" >&2
    exit 1
fi
linked_repository="$(jq -r '.repository.full_name // empty' "$details")"
if [[ -z $linked_repository ]]; then
    repository_name="${REPOSITORY_NAME#*/}"
    response="$work_directory/link-response.txt"
    status="$(curl --silent --show-error --output "$response" --write-out '%{http_code}' \
        --request POST --user "$package_user:$PACKAGE_PUBLISH_TOKEN" \
        "$api/packages/$owner/$package_type/$package_name/-/link/$repository_name")"
    if [[ $status == 201 || $status == 204 ]]; then
        echo "Linked $package_type/$package_name to $REPOSITORY_NAME."
    else
        echo "WARNING: package-to-repository linking failed with HTTP $status; publication remains successful:" >&2
        cat "$response" >&2
        echo "WARNING: Link $package_type/$package_name to $REPOSITORY_NAME manually if desired." >&2
    fi
elif [[ $linked_repository != "$REPOSITORY_NAME" ]]; then
    echo "WARNING: Package is already linked to $linked_repository; leaving that optional link unchanged." >&2
fi
