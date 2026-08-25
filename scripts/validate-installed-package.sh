#!/usr/bin/env bash
# SPDX-License-Identifier: 0BSD

set -euo pipefail

script_directory="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

package_name="suikoden-hd-remaster-save-editor"
application_directory="/usr/lib/$package_name"
command_path="/usr/bin/$package_name"
desktop_file="/usr/share/applications/$package_name.desktop"
icon_file="/usr/share/icons/hicolor/scalable/apps/$package_name.svg"

test -L "$command_path"
test -x "$command_path"
test "$(readlink -f "$command_path")" = "$application_directory/SuikodenHdSaveEditor.App"
test -x "$application_directory/SuikodenHdSaveEditor.App"
test -s "$desktop_file"
test -s "$icon_file"
test -s "/usr/share/licenses/$package_name/LICENSE"
test -s "/usr/share/licenses/$package_name/LICENSES/Avalonia-MIT.txt"
test -s "/usr/share/doc/$package_name/THIRD_PARTY_NOTICES.md"
test -s "/usr/share/doc/$package_name/README.md"
cmp "$application_directory/LICENSE" "/usr/share/licenses/$package_name/LICENSE"
diff -qr "$application_directory/LICENSES" "/usr/share/licenses/$package_name/LICENSES"
cmp "$application_directory/THIRD_PARTY_NOTICES.md" \
    "/usr/share/doc/$package_name/THIRD_PARTY_NOTICES.md"
cmp "$application_directory/README.md" "/usr/share/doc/$package_name/README.md"

desktop-file-validate "$desktop_file"
xmllint --noout "$icon_file"
file "$application_directory/SuikodenHdSaveEditor.App" | grep -q 'ELF 64-bit.*x86-64'
"$script_directory/check-linux-native-dependencies.sh" "$application_directory"

forbidden_path="$(find "$application_directory" -mindepth 1 -printf '%P\n' | grep -E '(^|/)(saves|reference|tests|TestResults|\.git)(/|$)|(^|/)(Data([0-9]|1[0-6])|_sharetmpsave0)$|\.(bak|pdb|pkg\.tar\.zst|rpm|snupkg|nupkg|trx)$' | head -1 || true)"
if [[ -n $forbidden_path ]]; then
    echo "Forbidden content was installed with the application: $forbidden_path" >&2
    exit 1
fi
