// SPDX-License-Identifier: 0BSD
using System.Text.Json;

namespace SuikodenHdSaveEditor.App.Services;

public sealed class RecentFileStore
{
    private readonly string settingsPath;

    public RecentFileStore(string? settingsPath = null)
    {
        this.settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SuikodenHdSaveEditor",
            "recent-files.json");
    }

    public IReadOnlyList<string> Load()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return [];
            }

            string[]? paths = JsonSerializer.Deserialize<string[]>(File.ReadAllText(settingsPath));
            return paths?.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray() ?? [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    public IReadOnlyList<string> Add(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string[] paths = Load()
            .Prepend(fullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        try
        {
            string directory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(directory);
            string temporary = settingsPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(paths));
            File.Move(temporary, settingsPath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Recent-file persistence is optional and never blocks save editing.
        }

        return paths;
    }
}
