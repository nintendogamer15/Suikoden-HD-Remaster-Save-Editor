// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core.Tests;

internal sealed class TestDirectory : IDisposable
{
    internal TestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"suikoden-editor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

