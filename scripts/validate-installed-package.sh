#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

script_directory="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

for tool in cmp desktop-file-validate diff file find grep readlink xmllint; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is required for installed-package validation." >&2
        exit 1
    fi
done

package_name="suikoden-hd-remaster-save-editor"
application_directory="/usr/lib/$package_name"
command_path="/usr/bin/$package_name"
desktop_file="/usr/share/applications/$package_name.desktop"
icon_file="/usr/share/icons/hicolor/scalable/apps/$package_name.svg"

failed_checks=0

# Every assertion reports the check it represents so a failing installation
# names the broken expectation instead of ending the run with a bare exit code.
# Checks are independent, so validation reports all failures in one CI run.
pass() {
    printf 'ok: %s\n' "$1"
}

fail() {
    failed_checks=$((failed_checks + 1))
    printf 'FAILED: %s\n' "$1" >&2
    if [[ -n ${2:-} ]]; then
        while IFS= read -r detail_line; do
            printf '    %s\n' "$detail_line" >&2
        done <<<"$2"
    fi
}

check_command() {
    local description="$1"
    shift
    local output
    if output="$("$@" 2>&1)"; then
        pass "$description"
    else
        fail "$description" "$output"
    fi
}

check_installed_file() {
    local description="$1"
    local path="$2"
    if [[ ! -e $path ]]; then
        fail "$description" "$path was not installed"
    elif [[ ! -s $path ]]; then
        fail "$description" "$path is empty"
    else
        pass "$description"
    fi
}

check_symlink_target() {
    local description="$1"
    local link="$2"
    local expected="$3"
    local resolved
    if [[ ! -L $link ]]; then
        fail "$description" "$link is not a symbolic link"
        return
    fi
    resolved="$(readlink -f "$link" 2>/dev/null || true)"
    if [[ $resolved != "$expected" ]]; then
        fail "$description" "$link resolves to '${resolved:-<nothing>}' instead of '$expected'"
        return
    fi
    if [[ ! -x $link ]]; then
        fail "$description" "$link is not executable"
        return
    fi
    pass "$description"
}

check_executable() {
    local description="$1"
    local path="$2"
    if [[ ! -f $path ]]; then
        fail "$description" "$path was not installed"
    elif [[ ! -x $path ]]; then
        fail "$description" "$path is not executable"
    else
        pass "$description"
    fi
}

check_symlink_target "the launcher command targets the installed executable" \
    "$command_path" "$application_directory/SuikodenHdSaveEditor.App"
check_executable "the application executable is installed and executable" \
    "$application_directory/SuikodenHdSaveEditor.App"
check_installed_file "the desktop entry is installed" "$desktop_file"
check_installed_file "the scalable icon is installed" "$icon_file"
check_installed_file "the project license is installed" \
    "/usr/share/licenses/$package_name/LICENSE"
check_installed_file "the third-party license directory is installed" \
    "/usr/share/licenses/$package_name/LICENSES/Avalonia-MIT.txt"
check_installed_file "the third-party notices are installed" \
    "/usr/share/doc/$package_name/THIRD_PARTY_NOTICES.md"
check_installed_file "the README is installed" \
    "/usr/share/doc/$package_name/README.md"

check_command "the installed and application-directory licenses are identical" \
    cmp "$application_directory/LICENSE" "/usr/share/licenses/$package_name/LICENSE"
check_command "the installed and application-directory license directories are identical" \
    diff -qr "$application_directory/LICENSES" "/usr/share/licenses/$package_name/LICENSES"
check_command "the installed and application-directory notices are identical" \
    cmp "$application_directory/THIRD_PARTY_NOTICES.md" \
    "/usr/share/doc/$package_name/THIRD_PARTY_NOTICES.md"
check_command "the installed and application-directory READMEs are identical" \
    cmp "$application_directory/README.md" "/usr/share/doc/$package_name/README.md"

check_command "the installed desktop entry is valid" desktop-file-validate "$desktop_file"
check_command "the installed icon is well-formed XML" xmllint --noout "$icon_file"

architecture="$(file "$application_directory/SuikodenHdSaveEditor.App" 2>&1 || true)"
printf '%s\n' "$architecture"
if grep -q 'ELF 64-bit.*x86-64' <<<"$architecture"; then
    pass "the installed executable is a 64-bit x86-64 ELF binary"
else
    fail "the installed executable is a 64-bit x86-64 ELF binary" "$architecture"
fi

if "$script_directory/check-linux-native-dependencies.sh" "$application_directory"; then
    pass "the installed executable resolves its native dependencies"
else
    fail "the installed executable resolves its native dependencies"
fi

forbidden_path="$(find "$application_directory" -mindepth 1 -printf '%P\n' | grep -E '(^|/)(saves|reference|tests|TestResults|\.git)(/|$)|(^|/)(Data([0-9]|1[0-6])|_sharetmpsave0)$|\.(bak|pdb|pkg\.tar\.zst|rpm|snupkg|nupkg|trx)$' | head -1 || true)"
if [[ -n $forbidden_path ]]; then
    fail "no forbidden content is installed with the application" \
        "Forbidden content was installed with the application: $forbidden_path"
else
    pass "no forbidden content is installed with the application"
fi

if [[ $failed_checks -ne 0 ]]; then
    echo "$failed_checks installed-package validation check(s) failed." >&2
    exit 1
fi

echo "All installed-package validation checks passed."
