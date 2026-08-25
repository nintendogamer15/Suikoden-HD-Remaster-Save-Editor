#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

require_commands bash timeout xvfb-run

test -x "$LINUX_PUBLISH_DIRECTORY/$APP_EXECUTABLE"
export XDG_CONFIG_HOME="$REPOSITORY_ROOT/.tools/smoke-config"
export DOTNET_BUNDLE_EXTRACT_BASE_DIR="$REPOSITORY_ROOT/.tools/bundle-extract"
export LIBGL_ALWAYS_SOFTWARE=1
mkdir -p "$XDG_CONFIG_HOME" "$DOTNET_BUNDLE_EXTRACT_BASE_DIR"
cd "$REPOSITORY_ROOT"
# Debian's xvfb-run mishandles an authentication temp path containing spaces.
# A repository-relative path keeps the smoke test local and space-safe.
export TMPDIR=".tools/test-tmp"

set +e
timeout 30s xvfb-run -a -s "-screen 0 1280x800x24 -nolisten tcp" \
    bash -c '
        display_number=${DISPLAY#:}
        for attempt in 1 2 3 4 5; do
            [[ -S "/tmp/.X11-unix/X${display_number}" ]] && break
            sleep 1
        done
        [[ -S "/tmp/.X11-unix/X${display_number}" ]]
        sleep 1
        exec "$1" --smoke-test
    ' smoke-runner "$LINUX_PUBLISH_DIRECTORY/$APP_EXECUTABLE"
status=$?
set -e

case "$status" in
    0)
        echo "Linux single-file GUI smoke test passed."
        ;;
    124)
        echo "Linux GUI remained running until the smoke-test timeout; startup was accepted."
        ;;
    *)
        echo "Linux single-file GUI smoke test failed with exit status $status." >&2
        exit "$status"
        ;;
esac
