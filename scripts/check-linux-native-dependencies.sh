#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

script_directory="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
repository_root="$(CDPATH= cd -- "$script_directory/.." && pwd)"
default_bundle="$repository_root/artifacts/publish/SuikodenHdSaveEditor-linux-x64"
app_executable="SuikodenHdSaveEditor.App"

if [[ $# -gt 1 ]]; then
    echo "Usage: $0 [linux-publish-directory]" >&2
    exit 2
fi

bundle="${1:-$default_bundle}"
bundle="$(realpath "$bundle")"
[[ -d $bundle ]] || { echo "Linux publish directory does not exist: $bundle" >&2; exit 2; }

for tool in file ldd grep realpath; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for Linux executable dependency validation." >&2
        exit 1
    fi
done

check_elf_dependencies() {
    local path="$1"
    local description="$2"
    local output

    test -s "$path" || { echo "$description is missing: $path" >&2; exit 1; }
    file "$path"
    if ! output="$(ldd "$path" 2>&1)"; then
        printf '%s\n' "$output" >&2
        echo "Unable to inspect native dependencies for $description: $path" >&2
        exit 1
    fi
    printf '%s\n' "$output"
    if grep -Eq '(^|[[:space:]])[^[:space:]]+[[:space:]]+=>[[:space:]]+not found([[:space:]]|$)' <<<"$output"; then
        echo "$description has an unresolved native dependency: $path" >&2
        exit 1
    fi
}

check_elf_dependencies "$bundle/$app_executable" "Linux application executable"
