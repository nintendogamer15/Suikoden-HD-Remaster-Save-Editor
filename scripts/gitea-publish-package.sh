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
if [[ $package_type == rpm ]]; then
    : "${GITEA_SERVER_URL:?GITEA_SERVER_URL is required for RPM signature verification}"
    command -v rpm >/dev/null 2>&1 || {
        echo "rpm is required for Gitea RPM publication." >&2
        exit 1
    }
fi

server="${GITEA_PACKAGE_SERVER_URL%/}"
api="$server/api/v1"
package_user="Robert"
filename="$(basename "$package_file")"
local_sha="$(sha256sum "$package_file" | cut -d ' ' -f 1)"
registry_version="$version-1"
work_directory="$(mktemp -d "${TMPDIR:-/tmp}/gitea-package-publish.XXXXXXXX")"
trap 'rm -rf -- "$work_directory"' EXIT
rpm_upload_in_progress=false
rpm_arch=""
local_rpm_identity=""
rpm_payload_digest_tag=""
rpm_payload_digest_algo_tag=""
rpm_database="$work_directory/rpmdb"

package_failure() {
    echo "$1" >&2
    if [[ $package_type == rpm && $rpm_upload_in_progress == true ]]; then
        echo "The RPM may now require deletion by the package owner before this immutable version can be published again." >&2
    fi
    exit 1
}

rpm_has_query_tag() {
    rpm --querytags | grep -Fxq "$1"
}

rpm_content_identity() {
    local rpm_file="$1"
    local query_output header_digest payload_digest payload_digest_algorithm
    local -a identity_fields

    query_output="$(rpm --dbpath "$rpm_database" -qp --queryformat \
        "%{SHA256HEADER}\\n%{$rpm_payload_digest_tag}\\n%{$rpm_payload_digest_algo_tag}\\n" \
        "$rpm_file")" || return 1
    mapfile -t identity_fields <<<"$query_output"
    [[ ${#identity_fields[@]} -eq 3 ]] || return 1
    header_digest="${identity_fields[0],,}"
    payload_digest="${identity_fields[1],,}"
    payload_digest_algorithm="${identity_fields[2],,}"
    [[ $header_digest =~ ^[0-9a-f]{64}$ ]] || return 1
    [[ $payload_digest =~ ^[0-9a-f]{64}$ ]] || return 1
    case "$payload_digest_algorithm" in
        8|sha256|sha-256) ;;
        *) return 1 ;;
    esac
    printf '%s:%s:sha256\n' "$header_digest" "$payload_digest"
}

rpm_version_at_least_1_23() {
    local candidate="$1"
    local major minor patch
    [[ $candidate =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)([-+].*)?$ ]] || return 1
    major="$((10#${BASH_REMATCH[1]}))"
    minor="$((10#${BASH_REMATCH[2]}))"
    patch="$((10#${BASH_REMATCH[3]}))"
    ((major > 1 || (major == 1 && (minor > 23 || (minor == 23 && patch >= 0)))))
}

prepare_rpm_verification() {
    local public_server version_file deployed_version repository_key

    [[ $GITEA_SERVER_URL == https://* ]] || {
        package_failure "GITEA_SERVER_URL must use HTTPS for RPM signature verification."
    }
    public_server="${GITEA_SERVER_URL%/}"

    version_file="$work_directory/gitea-version.json"
    if ! curl --fail --silent --show-error --proto '=https' --output "$version_file" \
        "$public_server/api/v1/version"; then
        package_failure "Could not verify the deployed Gitea version over public HTTPS."
    fi
    deployed_version="$(jq -r '.version // empty' "$version_file")"
    rpm_version_at_least_1_23 "$deployed_version" || {
        package_failure "Gitea 1.23.0 or newer is required for signed RPM uploads; deployed version is ${deployed_version:-unknown}."
    }

    repository_key="$work_directory/repository.key"
    if ! curl --fail --silent --show-error --proto '=https' --output "$repository_key" \
        "$public_server/api/packages/$owner/rpm/repository.key"; then
        package_failure "Could not download the deployed RPM repository key over public HTTPS."
    fi
    mkdir -m 0700 "$rpm_database"
    if ! rpm --dbpath "$rpm_database" --import "$repository_key"; then
        package_failure "Could not import the deployed RPM repository key into the isolated verification database."
    fi

    rpm_has_query_tag SHA256HEADER || package_failure "rpm cannot query the SHA256HEADER content identity tag."
    if rpm_has_query_tag PAYLOADSHA256 && rpm_has_query_tag PAYLOADSHA256ALGO; then
        rpm_payload_digest_tag=PAYLOADSHA256
        rpm_payload_digest_algo_tag=PAYLOADSHA256ALGO
    elif rpm_has_query_tag PAYLOADDIGEST && rpm_has_query_tag PAYLOADDIGESTALGO; then
        rpm_payload_digest_tag=PAYLOADDIGEST
        rpm_payload_digest_algo_tag=PAYLOADDIGESTALGO
    else
        package_failure "rpm cannot query a supported payload SHA-256 content identity."
    fi
    rpm_has_query_tag SIGPGP || package_failure "rpm cannot query the SIGPGP signature tag."
    rpm_has_query_tag RSAHEADER || package_failure "rpm cannot query the RSAHEADER signature tag."

    if ! rpm --checksig --nosignature "$package_file" >/dev/null; then
        package_failure "The locally built RPM failed digest verification."
    fi
    local_rpm_identity="$(rpm_content_identity "$package_file")" || {
        package_failure "Could not derive a SHA-256 content identity from the locally built RPM."
    }
    rpm_arch="$(rpm --dbpath "$rpm_database" -qp --queryformat '%{ARCH}' "$package_file")" || {
        package_failure "Could not read the architecture from the locally built RPM."
    }
    [[ $rpm_arch =~ ^[A-Za-z0-9_]+$ ]] || {
        package_failure "The locally built RPM has an invalid architecture: $rpm_arch"
    }
}

verify_stored_rpm() {
    local api_sha="$1"
    local public_server stored_rpm status downloaded_sha signature_tags stored_identity

    [[ $api_sha =~ ^[0-9a-fA-F]{64}$ ]] || package_failure "Gitea returned an invalid SHA-256 for $filename."
    api_sha="${api_sha,,}"
    public_server="${GITEA_SERVER_URL%/}"
    stored_rpm="$work_directory/stored-$filename"
    if ! status="$(curl --silent --show-error --proto '=https' --output "$stored_rpm" --write-out '%{http_code}' \
        "$public_server/api/packages/$owner/rpm/package/$package_name/$registry_version/$rpm_arch/$filename")"; then
        package_failure "Could not download the stored RPM over public HTTPS."
    fi
    [[ $status == 200 ]] || package_failure "Stored RPM download failed with HTTP $status."

    downloaded_sha="$(sha256sum "$stored_rpm" | cut -d ' ' -f 1)"
    [[ $downloaded_sha == "$api_sha" ]] || {
        package_failure "The stored RPM bytes do not match the SHA-256 reported by the Gitea package API."
    }
    signature_tags="$(rpm --dbpath "$rpm_database" -qp --queryformat \
        '%|SIGPGP?{SIGPGP}:{missing}|:%|RSAHEADER?{RSAHEADER}:{missing}|' "$stored_rpm")" || {
        package_failure "Could not inspect the stored RPM signature tags."
    }
    [[ $signature_tags == SIGPGP:RSAHEADER ]] || {
        package_failure "The stored RPM is missing the Gitea SIGPGP/RSAHEADER signature tags."
    }
    if ! rpm --dbpath "$rpm_database" --checksig "$stored_rpm" >/dev/null; then
        package_failure "The stored RPM signature does not verify with the deployed repository key."
    fi
    stored_identity="$(rpm_content_identity "$stored_rpm")" || {
        package_failure "Could not derive a SHA-256 content identity from the stored RPM."
    }
    [[ $stored_identity == "$local_rpm_identity" ]] || {
        package_failure "Package $filename already exists with different RPM content; refusing to replace it."
    }
}

if [[ $package_type == rpm ]]; then
    prepare_rpm_verification
fi

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
        package_failure "Gitea package lookup did not return a usable file list."
    fi
    existing_sha="$(jq -r --arg name "$filename" '[.[] | select(.name == $name)][0].sha256 // empty' "$body")"
    if [[ -z $existing_sha ]]; then
        echo "Package $package_type/$package_name $registry_version exists without $filename; refusing to modify it." >&2
        package_failure "Refusing to modify an incomplete immutable package version."
    fi
    if [[ $package_type == rpm ]]; then
        verify_stored_rpm "$existing_sha"
        echo "Package $filename already exists with matching content and a valid Gitea signature; skipping upload."
    else
        if [[ $existing_sha != "$local_sha" ]]; then
            echo "Package $filename already exists with a different SHA-256; refusing to replace it." >&2
            echo "existing=$existing_sha built=$local_sha" >&2
            exit 1
        fi
        echo "Package $filename already exists with matching SHA-256; skipping upload."
    fi
    return 0
}

if ! check_existing; then
    if [[ $package_type == arch ]]; then
        upload_url="$server/api/packages/$owner/arch/$registry"
    else
        upload_url="$server/api/packages/$owner/rpm/upload?sign=true"
        rpm_upload_in_progress=true
    fi
    response="$work_directory/upload-response.txt"
    if ! status="$(curl --silent --show-error --output "$response" --write-out '%{http_code}' \
        --request PUT --user "$package_user:$PACKAGE_PUBLISH_TOKEN" \
        --upload-file "$package_file" "$upload_url")"; then
        package_failure "The Gitea package upload request failed before a response was received."
    fi
    if [[ $status != 201 && $status != 409 ]]; then
        echo "Gitea $package_type upload failed with HTTP $status:" >&2
        cat "$response" >&2
        package_failure "Gitea rejected the package upload."
    fi
    verified=false
    for _ in 1 2 3 4 5; do
        if check_existing; then
            verified=true
            break
        fi
        sleep 2
    done
    [[ $verified == true ]] || package_failure "Uploaded package was not visible through the Gitea package API."
    rpm_upload_in_progress=false
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
