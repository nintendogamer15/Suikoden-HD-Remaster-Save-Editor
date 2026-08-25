// SPDX-License-Identifier: 0BSD
using System.Reflection;
using System.Text;

namespace SuikodenHdSaveEditor.App;

internal static class EmbeddedLegalNotices
{
    private const string ResourcePrefix = "Legal/";

    public static string Load()
    {
        Assembly assembly = typeof(EmbeddedLegalNotices).Assembly;
        string[] resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (resources.Length == 0)
        {
            return "Embedded legal notices could not be loaded.";
        }

        StringBuilder content = new();
        content.AppendLine("EMBEDDED LICENSE AND NOTICE TEXTS");
        foreach (string resource in resources)
        {
            using Stream stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded legal resource is missing: {resource}");
            using StreamReader reader = new(stream);
            content.AppendLine();
            content.Append("===== ");
            content.Append(resource[ResourcePrefix.Length..]);
            content.AppendLine(" =====");
            content.AppendLine(reader.ReadToEnd().TrimEnd());
        }

        return content.ToString();
    }
}
