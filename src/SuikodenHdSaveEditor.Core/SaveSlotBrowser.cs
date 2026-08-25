// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core;

public static class SaveSlotBrowser
{
    public static IReadOnlyList<SaveSlotEntry> Discover(string selectedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedDirectory);
        string directory = Path.GetFullPath(selectedDirectory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(directory);
        }

        List<(string Directory, GameKind Hint)> roots = [];
        AddIfDirectory(roots, Path.Combine(directory, "gsd1"), GameKind.Suikoden1);
        AddIfDirectory(roots, Path.Combine(directory, "gsd2"), GameKind.Suikoden2);

        string leaf = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (leaf.Equals("gsd1", StringComparison.OrdinalIgnoreCase))
        {
            roots.Add((directory, GameKind.Suikoden1));
        }
        else if (leaf.Equals("gsd2", StringComparison.OrdinalIgnoreCase))
        {
            roots.Add((directory, GameKind.Suikoden2));
        }

        return roots
            .DistinctBy(entry => entry.Directory, StringComparer.OrdinalIgnoreCase)
            .SelectMany(entry => Directory.EnumerateFiles(entry.Directory)
                .Select(path => new { Path = path, Slot = SlotDetector.FromPath(path), entry.Hint })
                .Where(value => value.Slot.HasValue)
                .Select(value => new SaveSlotEntry(value.Path, value.Slot!.Value, value.Hint)))
            .OrderBy(entry => entry.GameHint)
            .ThenBy(entry => entry.Slot)
            .ToArray();
    }

    private static void AddIfDirectory(List<(string Directory, GameKind Hint)> roots, string path, GameKind hint)
    {
        if (Directory.Exists(path))
        {
            roots.Add((Path.GetFullPath(path), hint));
        }
    }
}

public sealed record SaveSlotEntry(string Path, int Slot, GameKind GameHint);

