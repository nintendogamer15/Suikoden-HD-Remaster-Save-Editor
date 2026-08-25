// SPDX-License-Identifier: 0BSD
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuikodenHdSaveEditor.Core;

public static partial class SlotDetector
{
    [GeneratedRegex("^Data(?<slot>(?:[0-9]|1[0-6]))$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DataNamePattern();

    public static int? FromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        Match match = DataNamePattern().Match(Path.GetFileName(path));
        return match.Success
            ? int.Parse(match.Groups["slot"].Value, CultureInfo.InvariantCulture)
            : null;
    }
}

