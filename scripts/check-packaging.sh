#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail
source "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/common.sh"

for tool in desktop-file-validate xmllint; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for packaging metadata validation." >&2
        exit 1
    fi
done

desktop="$REPOSITORY_ROOT/packaging/linux/$PACKAGE_NAME.desktop"
icon="$REPOSITORY_ROOT/packaging/linux/$PACKAGE_NAME.svg"
desktop-file-validate "$desktop"
xmllint --noout "$icon"
grep -Fxq "Exec=$PACKAGE_NAME %f" "$desktop"
grep -Fxq "Icon=$PACKAGE_NAME" "$desktop"
grep -Fq "pkgname=$PACKAGE_NAME" "$REPOSITORY_ROOT/packaging/arch/PKGBUILD"
grep -Eq '^Name:[[:space:]]+suikoden-hd-remaster-save-editor$' \
    "$REPOSITORY_ROOT/packaging/rpm/suikoden-hd-remaster-save-editor.spec"

publisher="$SCRIPT_DIRECTORY/gitea-publish-package.sh"
grep -Fq 'upload_url="$server/api/packages/$owner/rpm/upload?sign=true"' "$publisher"
grep -Fq 'upload_url="$server/api/packages/$owner/arch/$registry"' "$publisher"
if grep -Fq 'arch/$registry?sign=true' "$publisher"; then
    echo "Arch package uploads must remain byte-for-byte and must not request server signing." >&2
    exit 1
fi
grep -Fq ': "${GITEA_SERVER_URL:?GITEA_SERVER_URL is required for RPM signature verification}"' "$publisher"
grep -Fq '"$public_server/api/v1/version"' "$publisher"
grep -Fq '"$public_server/api/packages/$owner/rpm/repository.key"' "$publisher"
grep -Fq '"$public_server/api/packages/$owner/rpm/package/$package_name/$registry_version/$rpm_arch/$filename"' "$publisher"
grep -Fq '%|SIGPGP?{SIGPGP}:{missing}|:%|RSAHEADER?{RSAHEADER}:{missing}|' "$publisher"
grep -Fq 'rpm --dbpath "$rpm_database" --checksig "$stored_rpm"' "$publisher"
grep -Fq 'rpm_content_identity "$stored_rpm"' "$publisher"

mapfile -t shell_scripts < <(find "$SCRIPT_DIRECTORY" -maxdepth 1 -type f -name '*.sh' -print | sort)
bash -n "${shell_scripts[@]}" "$REPOSITORY_ROOT/packaging/arch/PKGBUILD"
if command -v shellcheck >/dev/null 2>&1; then
    # These exclusions cover intentional dynamic sourcing/common-variable use,
    # the established CDPATH subshell idiom, and the quoted smoke-test program.
    shellcheck --external-sources --exclude=SC1007,SC1091,SC2034,SC2016 "${shell_scripts[@]}"
fi
if command -v rpmspec >/dev/null 2>&1; then
    rpmspec --parse --define "app_version $PROJECT_VERSION" \
        "$REPOSITORY_ROOT/packaging/rpm/suikoden-hd-remaster-save-editor.spec" >/dev/null
fi

if grep -RIl -E '192\.168\.1\.75|PACKAGE_PUBLISH_TOKEN' \
    "$REPOSITORY_ROOT/README.md" "$REPOSITORY_ROOT/packaging" | grep -q .; then
    echo "Internal package infrastructure leaked into end-user documentation or metadata." >&2
    exit 1
fi

if "$SCRIPT_DIRECTORY/project-version.sh" invalid >/dev/null 2>&1; then
    echo "Invalid release tags must be rejected." >&2
    exit 1
fi
test "$("$SCRIPT_DIRECTORY/project-version.sh" "v$PROJECT_VERSION")" = "$PROJECT_VERSION"
