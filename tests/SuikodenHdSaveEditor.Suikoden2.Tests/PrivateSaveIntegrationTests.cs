// SPDX-License-Identifier: 0BSD
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.Suikoden2.Tests;

public sealed class PrivateSaveIntegrationTests
{
    [Fact]
    public void OptInValidatesEveryCopiedSuikoden2SaveWithoutChangingOriginals()
    {
        string? root = Environment.GetEnvironmentVariable("SUIKODEN_PRIVATE_SAVE_ROOT");
        string source = Path.Combine(root ?? string.Empty, "gsd2");
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(source))
        {
            return;
        }

        string temporary = Path.Combine(Path.GetTempPath(), $"suikoden2-private-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            foreach (string original in Directory.EnumerateFiles(source, "Data*").Where(path => SlotDetector.FromPath(path).HasValue))
            {
                byte[] before = SHA256.HashData(File.ReadAllBytes(original));
                string copy = Path.Combine(temporary, Path.GetFileName(original));
                File.Copy(original, copy);

                SaveDocument document = SaveDocument.OpenEncrypted(copy);
                Suikoden2Adapter adapter = new(document);
                Assert.DoesNotContain(adapter.Validate(), issue => issue.Severity == ValidationSeverity.Error);
                string output = copy + ".roundtrip";
                new SaveFileService().SaveAs(document.DeepClone(), output);
                Assert.True(SaveDocument.SemanticallyEquals(document.Root, SaveDocument.OpenEncrypted(output).Root));

                SaveDocument renamed = document.DeepClone();
                new Suikoden2Adapter(renamed).SetName("bozu_name", "Private Test Hero");
                string renamedOutput = copy + ".renamed";
                new SaveFileService().SaveAs(renamed, renamedOutput);
                SaveDocument reopenedRename = SaveDocument.OpenEncrypted(renamedOutput);
                Assert.Equal("Private Test Hero", reopenedRename.Root["game_data"]!["bozu_name"]!.GetValue<string>());
                Assert.Equal("Private Test Hero", reopenedRename.Root["game_data"]!["bozu_name2"]!.GetValue<string>());

                SaveDocument quantityEdit = document.DeepClone();
                Suikoden2Adapter quantityAdapter = new(quantityEdit);
                (Suikoden2Inventory Inventory, JsonArray Items)[] quantityContainers =
                [
                    (Suikoden2Inventory.Party, quantityEdit.Root["party_data"]!["party_item"]!.AsArray()),
                    (Suikoden2Inventory.Warehouse, quantityEdit.Root["game_data"]!["base_item"]!.AsArray()),
                    (Suikoden2Inventory.RoomExperimental, quantityEdit.Root["game_data"]!["room_item"]!.AsArray()),
                ];
                bool quantityChanged = false;
                foreach ((Suikoden2Inventory inventory, JsonArray items) in quantityContainers)
                {
                    int stack = items.Select(node => node!.AsObject()).ToList().FindIndex(item =>
                    {
                        Suikoden2ItemDefinition? definition = Suikoden2Catalog.StoredItem(
                            item["item_no"]!.GetValue<int>(),
                            item["use_cnt"]!.GetValue<int>());
                        return definition is { Category: Suikoden2ItemCategory.Regular, UseCount: > 1 };
                    });
                    if (stack < 0)
                    {
                        continue;
                    }

                    quantityAdapter.SetInventoryQuantity(inventory, stack, 1);
                    string quantityOutput = copy + ".quantity";
                    new SaveFileService().SaveAs(quantityEdit, quantityOutput);
                    JsonArray reopenedItems = inventory switch
                    {
                        Suikoden2Inventory.Party => SaveDocument.OpenEncrypted(quantityOutput).Root["party_data"]!["party_item"]!.AsArray(),
                        Suikoden2Inventory.Warehouse => SaveDocument.OpenEncrypted(quantityOutput).Root["game_data"]!["base_item"]!.AsArray(),
                        _ => SaveDocument.OpenEncrypted(quantityOutput).Root["game_data"]!["room_item"]!.AsArray(),
                    };
                    Assert.Equal(1, reopenedItems[stack]!["use_cnt"]!.GetValue<int>());
                    quantityChanged = true;
                    break;
                }

                Assert.True(quantityChanged);

                SaveDocument optimized = document.DeepClone();
                PartyOptimizationResult result = new Suikoden2Adapter(optimized).MaximizeAndEquipParty();
                Assert.True(result.CharactersUpdated > 0);
                Assert.DoesNotContain(new Suikoden2Adapter(optimized).Validate(), issue => issue.Severity == ValidationSeverity.Error);
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
