// SPDX-License-Identifier: 0BSD
using System.Security.Cryptography;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;

namespace SuikodenHdSaveEditor.Suikoden1.Tests;

public sealed class PrivateSaveIntegrationTests
{
    [Fact]
    public void OptInValidatesEveryCopiedSuikoden1SaveWithoutChangingOriginals()
    {
        string? root = Environment.GetEnvironmentVariable("SUIKODEN_PRIVATE_SAVE_ROOT");
        string source = Path.Combine(root ?? string.Empty, "gsd1");
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(source))
        {
            return;
        }

        string temporary = Path.Combine(Path.GetTempPath(), $"suikoden1-private-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            foreach (string original in Directory.EnumerateFiles(source, "Data*").Where(path => SlotDetector.FromPath(path).HasValue))
            {
                byte[] before = SHA256.HashData(File.ReadAllBytes(original));
                string copy = Path.Combine(temporary, Path.GetFileName(original));
                File.Copy(original, copy);

                SaveDocument document = SaveDocument.OpenEncrypted(copy);
                Suikoden1Adapter adapter = new(document);
                Assert.DoesNotContain(adapter.Validate(), issue => issue.Severity == ValidationSeverity.Error);
                string output = copy + ".roundtrip";
                new SaveFileService().SaveAs(document.DeepClone(), output);
                Assert.True(SaveDocument.SemanticallyEquals(document.Root, SaveDocument.OpenEncrypted(output).Root));

                SaveDocument optimized = document.DeepClone();
                PartyOptimizationResult result = new Suikoden1Adapter(optimized).MaximizeAndEquipParty();
                Assert.True(result.CharactersUpdated > 0);
                Assert.DoesNotContain(new Suikoden1Adapter(optimized).Validate(), issue => issue.Severity == ValidationSeverity.Error);
                string optimizedOutput = copy + ".optimized";
                new SaveFileService().SaveAs(optimized, optimizedOutput);
                Assert.True(SaveDocument.SemanticallyEquals(optimized.Root, SaveDocument.OpenEncrypted(optimizedOutput).Root));
                Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(original)));
            }
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }
}
