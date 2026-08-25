// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.Suikoden2.Tests;

public sealed class Suikoden2AdapterTests
{
    [Fact]
    public void CatalogSupportsSearchCategoriesAndStorySafety()
    {
        Assert.Equal("Riou", Suikoden2Catalog.Characters[1].Name);
        Assert.NotEmpty(Suikoden2Catalog.SearchItems("Medicine", Suikoden2ItemCategory.Regular));
        Assert.Contains(Suikoden2Catalog.Items, item => item.StoryCritical);
        Assert.Contains(26, Suikoden2Catalog.Beasts);
    }

    [Fact]
    public void PartyUsesSixBattleAndTwoConvoyLimits()
    {
        Suikoden2Adapter adapter = new(Suikoden2TestFactory.Create());

        adapter.SetParty([1, 2, 3, 4, 5, 6, 100, 124]);

        Assert.Equal([1, 2, 3, 4, 5, 6, 100, 124], adapter.PartyCharacterIds);
        Assert.Throws<SaveEditorException>(() => adapter.SetParty([1, 2, 3, 4, 5, 84, 0, 0]));
        Assert.Throws<SaveEditorException>(() => adapter.SetParty([1, 2, 3]));
    }

    [Fact]
    public void EditsCharacterStatsHpMpWeaponAndKills()
    {
        Suikoden2Adapter adapter = new(Suikoden2TestFactory.Create());
        adapter.SetCharacterScalar(1, "level", 20);
        adapter.SetCharacterScalar(1, "max_hp", 120);
        adapter.SetCharacterScalar(1, "now_hp", 110);
        adapter.SetCharacterScalar(1, "buki_lv", 16);
        adapter.SetCharacterScalar(1, "todome", 99);
        adapter.SetMagicPoint(1, 0, 153);
        adapter.SetStat(1, 6, 77);

        Suikoden2CharacterView hero = adapter.Characters[1];
        Assert.Equal(20, hero.Level);
        Assert.Equal(110, hero.CurrentHp);
        Assert.Equal(120, hero.MaximumHp);
        Assert.Equal(16, hero.WeaponLevel);
        Assert.Equal(99, hero.KilledEnemies);
        Assert.Equal(153, hero.MagicPoints[0]);
        Assert.Equal(77, hero.Stats[6]);
    }

    [Fact]
    public void MaximizeAndEquipPartyUsesCapsRolesRestrictionsAndPreservesLockedGear()
    {
        SaveDocument document = Suikoden2TestFactory.Create();
        JsonObject riou = document.Root["chara_data"]!["c_varia_dat"]![1]!.AsObject();
        riou["para"]![0] = 30;
        riou["para"]![1] = 10;
        JsonObject sheenaLocked = document.Root["chara_data"]!["c_varia_dat"]![5]!["item_eqp"]![0]!.AsObject();
        sheenaLocked["item_no"] = 72;
        sheenaLocked["use_cnt"] = 16;
        Suikoden2Adapter adapter = new(document);

        PartyOptimizationResult result = adapter.MaximizeAndEquipParty();

        Assert.Equal(6, result.CharactersUpdated);
        Assert.True(result.EquipmentSlotsUpdated > 0);
        Assert.True(result.LockedOrUnavailableSlotsPreserved > 0);
        foreach (int id in adapter.PartyCharacterIds.Take(Suikoden2Adapter.BattlePartySize).Distinct())
        {
            Suikoden2CharacterView character = adapter.Characters[id];
            Assert.Equal(Suikoden2Adapter.MaximumCharacterLevel, character.Level);
            Assert.Equal(Suikoden2Adapter.MaximumCharacterHp, character.CurrentHp);
            Assert.Equal(Suikoden2Adapter.MaximumCharacterHp, character.MaximumHp);
            Assert.All(character.MagicPoints, value => Assert.Equal(Suikoden2Adapter.MaximumMagicValue, value));
            Assert.All(character.Stats, value => Assert.Equal(Suikoden2Adapter.MaximumCharacterStat, value));
            Assert.Equal(16, character.WeaponLevel);
        }
        Assert.Equal([10, 34, 0], adapter.Characters[1].Equipment);
        Assert.All(adapter.Characters[1].Accessories, item => Assert.Equal(82, item!["item_no"]!.GetValue<int>()));
        Assert.Equal(72, sheenaLocked["item_no"]!.GetValue<int>());
        Assert.True(document.Root["unknown_root"]!["keep"]!.GetValue<bool>());
        Assert.DoesNotContain(adapter.Validate(), issue => issue.Severity == ValidationSeverity.Error);
        Assert.Throws<SaveEditorException>(() => adapter.SetStat(1, 0, 256));
        Assert.Throws<SaveEditorException>(() => adapter.SetCharacterScalar(1, "level", 100));
    }

    [Fact]
    public void MaximizeAndEquipPartySkipsEmptySlotsAndLeavesBeastsUnequipped()
    {
        SaveDocument document = Suikoden2TestFactory.Create();
        document.Root["party_data"]!["party_cha_no"] = new JsonArray(26, 0, 0, 0, 0, 0, 0, 0);
        Suikoden2Adapter adapter = new(document);

        PartyOptimizationResult result = adapter.MaximizeAndEquipParty();

        Assert.Equal(1, result.CharactersUpdated);
        Suikoden2CharacterView beast = adapter.Characters[26];
        Assert.Equal(Suikoden2Adapter.MaximumCharacterHp, beast.CurrentHp);
        Assert.Equal([0, 0, 0], beast.Equipment);
        Assert.All(beast.Accessories, item =>
        {
            Assert.Equal(0, item!["item_no"]!.GetValue<int>());
            Assert.Equal(0, item["use_cnt"]!.GetValue<int>());
        });
        Assert.Equal(10, adapter.Characters[0].Level);
    }

    [Fact]
    public void EnforcesRuneEquipmentAndBeastRestrictions()
    {
        Suikoden2Adapter adapter = new(Suikoden2TestFactory.Create());
        Suikoden2ItemDefinition normalRune = Suikoden2Catalog.Items.First(
            item => item.Category == Suikoden2ItemCategory.Rune
                && !item.Attributes.Contains("ExR")
                && !item.Attributes.Contains("X")
                && Suikoden2Catalog.IsRuneAllowed(1, 0, item.Id));
        Suikoden2ItemDefinition helmet = Suikoden2Catalog.Items.First(
            item => item.Category == Suikoden2ItemCategory.Helmet
                && item.Id != 0
                && Suikoden2Catalog.IsEquipmentAllowed(1, Suikoden2ItemCategory.Helmet, item.Id));

        adapter.SetRune(1, 0, normalRune.Id);
        adapter.SetEquipment(1, 0, helmet.Id);

        Assert.Equal(normalRune.Id, adapter.Characters[1].Runes[0]);
        Assert.Equal(helmet.Id, adapter.Characters[1].Equipment[0]);
        Assert.Throws<SaveEditorException>(() => adapter.SetEquipment(26, 0, helmet.Id));
        Assert.Throws<SaveEditorException>(() => adapter.SetCharacterScalar(26, "buki_mon", 1));
        Suikoden2ItemDefinition keyItem = Suikoden2Catalog.Items.First(item => item.StoryCritical);
        Assert.Throws<SaveEditorException>(() => adapter.SetAccessory(1, 0, keyItem));
    }

    [Fact]
    public void EditsAllInventoryKindsAndExcludesStoryItemsFromBulkFill()
    {
        Suikoden2Adapter adapter = new(Suikoden2TestFactory.Create());
        Suikoden2ItemDefinition medicine = Suikoden2Catalog.FindItem(Suikoden2ItemCategory.Regular, 1);
        Suikoden2ItemDefinition baseItem = Suikoden2Catalog.Items.First(item => item.Category == Suikoden2ItemCategory.Base && item.Id != 0);
        Suikoden2ItemDefinition ornament = Suikoden2Catalog.FindItem(Suikoden2ItemCategory.Trade, 1);
        Suikoden2ItemDefinition painting = Suikoden2Catalog.FindItem(Suikoden2ItemCategory.Trade, 18);

        adapter.SetInventorySlot(Suikoden2Inventory.Party, 0, medicine);
        adapter.SetInventorySlot(Suikoden2Inventory.Warehouse, 0, baseItem);
        adapter.SetInventorySlot(Suikoden2Inventory.Bath, 0, ornament);
        adapter.SetInventorySlot(Suikoden2Inventory.Bath, 2, painting);
        adapter.SetInventorySlot(Suikoden2Inventory.RoomExperimental, 0, ornament);
        int added = adapter.GiveAllSafePartyItems();

        Assert.True(added > 0);
        JsonArray party = adapter.Document.Root["party_data"]!["party_item"]!.AsArray();
        Assert.Equal(30, party.Count);
        Assert.DoesNotContain(
            party.Select(node => node!.AsObject()),
            slot => Suikoden2Catalog.Items.Any(item => item.Category == Suikoden2ItemCategory.Regular
                && item.StoryCritical
                && item.Id == slot["item_no"]!.GetValue<int>()));
        Assert.Equal(64, adapter.Document.Root["game_data"]!["furo_item"]![2]!["use_cnt"]!.GetValue<int>());
        Assert.Throws<SaveEditorException>(() => adapter.SetInventorySlot(Suikoden2Inventory.Bath, 2, ornament));
        Assert.Throws<SaveEditorException>(() => adapter.SetInventorySlot(Suikoden2Inventory.Bath, 0, medicine));
    }

    [Fact]
    public void EditsRecruitmentNamesGeneralProgressAndExperimentalFlags()
    {
        Suikoden2Adapter adapter = new(Suikoden2TestFactory.Create());
        adapter.SetRecruitmentStatus(2, 70);
        adapter.SetName("bozu_name", "Test Riou");
        adapter.SetGeneralNumber("gold", 9999);
        adapter.SetGeneralNumber("base_lv", 4);
        adapter.SetGeneralNumber("area_no", 3);
        adapter.SetGameDataArrayValue("play_time", 0, 5);
        adapter.SetGameDataArrayValue("food_menu", 0, 1);
        adapter.SetGameDataArrayValue("food_resipi", 0, 1);
        adapter.SetGameDataArrayValue("tantei_lv", 0, 255);
        adapter.SetGameDataArrayValue("hon_flag", 30, 255);
        adapter.SetGreenhillAlias(0, "Test Alias");
        adapter.SetTreasureFlagByte(0, 255);
        adapter.SetRootByteArrayValue("event_flag", 10, 128);
        adapter.SetCookOffStage(12);

        Assert.Equal(9999, adapter.Potch);
        Assert.Equal(4, adapter.Document.Root["game_data"]!["base_lv"]!.GetValue<int>());
        Assert.Equal(70, adapter.Document.Root["chara_flag"]![2]!.GetValue<int>());
        Assert.Equal("Test Riou", adapter.Document.Root["game_data"]!["bozu_name"]!.GetValue<string>());
        Assert.Equal(255, adapter.Document.Root["t_box_flag"]![0]!.GetValue<int>());
        Assert.Equal(47, adapter.Document.Root["event_flag"]![153]!.GetValue<int>());
        Assert.Throws<SaveEditorException>(() => adapter.SetGeneralNumber("base_lv", 5));
    }

    [Fact]
    public void KeyItemsRequireReviewedStoryIds()
    {
        Suikoden2Adapter adapter = new(Suikoden2TestFactory.Create());
        Suikoden2ItemDefinition key = Suikoden2Catalog.Items.First(item => item.StoryCritical);

        adapter.SetKeyItem(0, key.Id);

        Assert.Equal(key.Id, adapter.Document.Root["party_data"]!["event_item"]![0]!.GetValue<int>());
        Assert.Throws<SaveEditorException>(() => adapter.SetKeyItem(1, 1));
    }

    [Fact]
    public void CompatibilityNotesDescribeOptionalStatesWithoutMutatingFlags()
    {
        SaveDocument document = Suikoden2TestFactory.Create();
        document.Root["game_data"]!["macd_name"] = "Synthetic Import";
        JsonNode flags = document.Root["chara_flag"]!.DeepClone();

        IReadOnlyList<string> notes = new Suikoden2Adapter(document).CompatibilityNotes(true, true);

        Assert.Equal(3, notes.Count);
        Assert.True(JsonNode.DeepEquals(flags, document.Root["chara_flag"]));
    }

    [Fact]
    public void NoUnrelatedFieldsChangeAcrossEncryptedEditRoundTrip()
    {
        SaveDocument document = Suikoden2TestFactory.Create();
        JsonNode unknown = document.Root["unknown_root"]!.DeepClone();
        new Suikoden2Adapter(document).SetGeneralNumber("gold", 7777);

        SaveDocument reopened = SaveDocument.Parse(SaveCrypto.DecryptEnvelope(SaveCrypto.EncryptJson(document.ToJson())));

        Assert.True(JsonNode.DeepEquals(unknown, reopened.Root["unknown_root"]));
        Assert.Equal(7777, reopened.Root["party_data"]!["gold"]!.GetValue<int>());
    }

    [Fact]
    public void ValidationReportsNoErrorsForSyntheticValidSave()
    {
        IReadOnlyList<ValidationIssue> issues = new Suikoden2Adapter(Suikoden2TestFactory.Create()).Validate();
        Assert.DoesNotContain(issues, issue => issue.Severity == ValidationSeverity.Error);
    }
}
