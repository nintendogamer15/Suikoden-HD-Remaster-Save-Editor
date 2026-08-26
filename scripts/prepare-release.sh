#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <X.Y.Z|patch|minor|major>" >&2
    exit 2
fi

script_directory="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(CDPATH= cd -- "$script_directory/.." && pwd)"
properties="$repository_root/Directory.Build.props"
changelog="$repository_root/CHANGELOG.md"

for tool in date sed sort; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required to prepare a release." >&2
        exit 1
    fi
done

current_version="$("$script_directory/project-version.sh")"
IFS=. read -r current_major current_minor current_patch <<<"$current_version"

case "$1" in
    major) version="$((current_major + 1)).0.0" ;;
    minor) version="$current_major.$((current_minor + 1)).0" ;;
    patch) version="$current_major.$current_minor.$((current_patch + 1))" ;;
    *)
        version="${1#v}"
        if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            echo "Release version must be X.Y.Z, patch, minor, or major, got: $1" >&2
            exit 2
        fi
        ;;
esac

# Published tags are never moved or reused, so a release may only ever move the
# project version forward.
if [[ $version == "$current_version" ]]; then
    echo "Release version $version is already the project version." >&2
    exit 1
fi
if [[ "$(printf '%s\n%s\n' "$current_version" "$version" | sort -V | tail -1)" != "$version" ]]; then
    echo "Release version $version is lower than the current project version $current_version." >&2
    exit 1
fi
if git -C "$repository_root" rev-parse -q --verify "refs/tags/v$version" >/dev/null 2>&1; then
    echo "Tag v$version already exists; choose a new unused version." >&2
    exit 1
fi

mapfile -t changelog_lines < "$changelog"
unreleased_index=-1
next_section_index=${#changelog_lines[@]}
for index in "${!changelog_lines[@]}"; do
    if [[ $unreleased_index -lt 0 ]]; then
        if [[ ${changelog_lines[index]} == "## Unreleased" ]]; then
            unreleased_index=$index
        fi
        continue
    fi
    if [[ ${changelog_lines[index]} == "## "* ]]; then
        next_section_index=$index
        break
    fi
done

if [[ $unreleased_index -lt 0 ]]; then
    echo "$changelog must contain an '## Unreleased' section to release from." >&2
    exit 1
fi

# An empty section would publish a release with no notes, so the entries have to
# exist before the version is stamped onto them.
release_notes_found=false
for ((index = unreleased_index + 1; index < next_section_index; index++)); do
    if [[ -n ${changelog_lines[index]// /} ]]; then
        release_notes_found=true
        break
    fi
done
if [[ $release_notes_found != true ]]; then
    echo "The '## Unreleased' section of $changelog is empty." >&2
    echo "Add the entries this release publishes, then run the release again." >&2
    exit 1
fi

release_date="$(date -u +%F)"
{
    for ((index = 0; index <= unreleased_index; index++)); do
        printf '%s\n' "${changelog_lines[index]}"
    done
    printf '\n## %s - %s\n' "$version" "$release_date"
    for ((index = unreleased_index + 1; index < ${#changelog_lines[@]}; index++)); do
        printf '%s\n' "${changelog_lines[index]}"
    done
} > "$changelog.release"
mv "$changelog.release" "$changelog"

sed -i "s:<Version>[^<]*</Version>:<Version>$version</Version>:" "$properties"
if [[ "$("$script_directory/project-version.sh")" != "$version" ]]; then
    echo "Failed to set the project version to $version in $properties." >&2
    exit 1
fi

echo "Prepared release v$version (was v$current_version) dated $release_date." >&2
printf '%s\n' "$version"
