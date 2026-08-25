#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(CDPATH= cd -- "$SCRIPT_DIRECTORY/.." && pwd)"
SOLUTION="$REPOSITORY_ROOT/SuikodenHdSaveEditor.slnx"
APP_PROJECT="$REPOSITORY_ROOT/src/SuikodenHdSaveEditor.App/SuikodenHdSaveEditor.App.csproj"
ARTIFACTS_DIRECTORY="$REPOSITORY_ROOT/artifacts"
PACKAGE_NAME="suikoden-hd-remaster-save-editor"
APP_EXECUTABLE="SuikodenHdSaveEditor.App"
LINUX_BUNDLE_NAME="SuikodenHdSaveEditor-linux-x64"
WINDOWS_BUNDLE_NAME="SuikodenHdSaveEditor-windows-x64"
LINUX_PUBLISH_DIRECTORY="$ARTIFACTS_DIRECTORY/publish/$LINUX_BUNDLE_NAME"
WINDOWS_PUBLISH_DIRECTORY="$ARTIFACTS_DIRECTORY/publish/$WINDOWS_BUNDLE_NAME"
PROJECT_VERSION="$("$SCRIPT_DIRECTORY/project-version.sh")"
APP_VERSION="${APP_VERSION:-$PROJECT_VERSION}"
if [[ ! $APP_VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "APP_VERSION must be X.Y.Z, got: $APP_VERSION" >&2
    exit 2
fi

if [[ -x "$REPOSITORY_ROOT/.dotnet/dotnet" ]]; then
    export DOTNET_ROOT="$REPOSITORY_ROOT/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "The .NET 10 SDK is required but dotnet was not found." >&2
    exit 1
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$REPOSITORY_ROOT/.tools/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$REPOSITORY_ROOT/.tools/nuget-packages}"
export TMPDIR="${TMPDIR:-$REPOSITORY_ROOT/.tools/test-tmp}"

mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$TMPDIR" "$ARTIFACTS_DIRECTORY"

require_commands() {
    local command_name
    for command_name in "$@"; do
        if ! command -v "$command_name" >/dev/null 2>&1; then
            echo "$command_name is required but was not found." >&2
            exit 1
        fi
    done
}

assert_single_file_publish() {
    local directory="$1"
    local expected_name="$2"
    local -a entries

    [[ -d $directory ]] || { echo "Publish directory does not exist: $directory" >&2; exit 1; }
    mapfile -t entries < <(find "$directory" -mindepth 1 -maxdepth 1 -printf '%f\n' | sort)
    if [[ ${#entries[@]} -ne 1 || ${entries[0]:-} != "$expected_name" ]]; then
        echo "Expected exactly one publish output named $expected_name in $directory." >&2
        printf 'Found: %s\n' "${entries[*]:-<nothing>}" >&2
        exit 1
    fi
    [[ -f $directory/$expected_name && ! -L $directory/$expected_name ]] || {
        echo "Single-file publish output is not a regular file: $directory/$expected_name" >&2
        exit 1
    }
}
