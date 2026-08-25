#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

SCRIPT_DIRECTORY="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(CDPATH= cd -- "$SCRIPT_DIRECTORY/.." && pwd)"
SOLUTION="$REPOSITORY_ROOT/SuikodenHdSaveEditor.slnx"
APP_PROJECT="$REPOSITORY_ROOT/src/SuikodenHdSaveEditor.App/SuikodenHdSaveEditor.App.csproj"
ARTIFACTS_DIRECTORY="$REPOSITORY_ROOT/artifacts"
LINUX_BUNDLE_NAME="SuikodenHdSaveEditor-linux-x64"
WINDOWS_BUNDLE_NAME="SuikodenHdSaveEditor-windows-x64"
LINUX_PUBLISH_DIRECTORY="$ARTIFACTS_DIRECTORY/publish/$LINUX_BUNDLE_NAME"
WINDOWS_PUBLISH_DIRECTORY="$ARTIFACTS_DIRECTORY/publish/$WINDOWS_BUNDLE_NAME"

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

stage_legal_files() {
    local destination="$1"
    mkdir -p "$destination"
    cp "$REPOSITORY_ROOT/LICENSE" "$destination/LICENSE"
    cp "$REPOSITORY_ROOT/README.md" "$destination/README.md"
    cp "$REPOSITORY_ROOT/THIRD_PARTY_NOTICES.md" "$destination/THIRD_PARTY_NOTICES.md"
    cp -R "$REPOSITORY_ROOT/LICENSES" "$destination/LICENSES"
}
