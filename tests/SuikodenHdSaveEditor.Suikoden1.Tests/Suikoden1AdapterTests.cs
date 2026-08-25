// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;

namespace SuikodenHdSaveEditor.Suikoden1.Tests;

public sealed class Suikoden1AdapterTests
{
    [Fact]
    public void CatalogContainsCreditedCharactersItemsSlotsAndRunes()
    {
        Assert.Equal("Tir McDohl", Suikoden1Catalog.Characters[8]);
        Assert.Equal("Medicine", Suikoden1Catalog.Items[25]);
        Assert.Equal("Non-removable Armor", Suikoden1Catalog.EquipmentSlots[130]);
        Assert.Equal("Soul Eater", Suikoden1Catalog.Runes[1]);
    }

    [Fact]
    public void EditsOverviewPartyAndRecruitment()
    {
        Suikoden1Adapter adapter = new(Suikoden1TestFactory.Create());

        adapter.SetPotch(4321);
        adapter.SetHeadquartersLevel(4);
        adapter.SetNames("Hero Test", "HQ Test");
        adapter.SetParty([8, 5, 4, 3, 2, 1]);
        adapter.SetRecruited(1, true);

        Assert.Equal(4321, adapter.Potch);
        Assert.Equal(4, adapter.HeadquartersLevel);
        Assert.Equal("Hero Test", adapter.HeroName);
        Assert.Equal([8, 5, 4, 3, 2, 1], adapter.PartyCharacterIds);
        Assert.Contains(1, adapter.RecruitedCharacterIds);
        Assert.Throws<SaveEditorException>(() => adapter.SetHeadquartersLevel(5));
    }

    [Fact]
    public void PartyRequiresTirAndBattleRecords()
    {
        Suikoden1Adapter adapter = new(Suikoden1TestFactory.Create());

        Assert.Throws<SaveEditorException>(() => adapter.SetParty([1, 2, 3, 4, 5, 1]));
        Assert.Throws<SaveEditorException>(() => adapter.SetParty([8, 1, 2, 3, 4, 27]));
    }

    [Fact]
    public void EditsEveryReviewedCharacterCategory()
    {
        Suikoden1Adapter adapter = new(Suikoden1TestFactory.Create());

        adapter.SetCharacterScalar(8, "level", 20);
        adapter.SetCharacterScalar(8, "max_hp", 120);
        adapter.SetCharacterScalar(8, "hp", 110);
        adapter.SetCharacterStat(8, 0, 40);
        adapter.SetMagicPoint(8, 1, 9);
        adapter.SetWeapon(8, 7, 8);
        adapter.SetWeaponRunePiece(8, 4, 1);
        adapter.SetCharacterRune(8, 1);
        adapter.SetCharacterItem(8, 1, 73, 0, 1);

        Suikoden1CharacterView hero = adapter.Characters.Single(character => character.Id == 8);
        Assert.Equal(20, hero.Level);
        Assert.Equal(110, hero.CurrentHp);
        Assert.Equal(120, hero.MaximumHp);
        Assert.Equal(40, hero.Stats[0]);
        Assert.Equal(9, hero.CurrentMagicPoints[1]);
        Assert.Equal(7, hero.WeaponId);
        Assert.Equal(8, hero.WeaponLevel);
        Assert.Equal(1, hero.RuneId);
        Assert.Equal(2, hero.ItemCount);
    }

    [Fact]
    public void MaximizeAndEquipPartyUsesCapsRecommendationsAndPreservesLockedGear()
    {
        SaveDocument document = Suikoden1TestFactory.Create();
        JsonObject heroData = document.Root["player_base"]!.AsArray()[0]!.AsObject();
        JsonObject lockedHelmet = heroData["item"]!.AsArray()[0]!.AsObject();
        lockedHelmet["item_id"] = 37;
        lockedHelmet["soubi"] = 129;
        lockedHelmet["data"] = 0;
        Suikoden1Adapter adapter = new(document);
        Dictionary<int, int> weaponIds = adapter.Characters.ToDictionary(character => character.Id, character => character.WeaponId);

        PartyOptimizationResult result = adapter.MaximizeAndEquipParty();

        Assert.Equal(6, result.CharactersUpdated);
        Assert.True(result.EquipmentSlotsUpdated > 0);
        Assert.True(result.LockedOrUnavailableSlotsPreserved > 0);
        Assert.All(adapter.Characters, character =>
        {
            Assert.Equal(Suikoden1Adapter.MaximumCharacterLevel, character.Level);
            Assert.Equal(Suikoden1Adapter.MaximumCharacterHp, character.CurrentHp);
            Assert.Equal(Suikoden1Adapter.MaximumCharacterHp, character.MaximumHp);
            Assert.All(character.CurrentMagicPoints.Skip(1), value => Assert.Equal(9, value));
            Assert.All(character.Stats, value => Assert.Equal(Suikoden1Adapter.MaximumCharacterStat, value));
            Assert.Equal(Suikoden1Adapter.MaximumWeaponLevel, character.WeaponLevel);
            Assert.Equal(weaponIds[character.Id], character.WeaponId);
        });
        Assert.Equal(37, lockedHelmet["item_id"]!.GetValue<int>());
        Assert.Equal(129, lockedHelmet["soubi"]!.GetValue<int>());
        Assert.True(document.Root["unknown_root"]!["keep"]!.GetValue<bool>());
        Assert.DoesNotContain(adapter.Validate(), issue => issue.Severity == ValidationSeverity.Error);
        Assert.Throws<SaveEditorException>(() => adapter.SetCharacterStat(8, 0, 256));
        Assert.Throws<SaveEditorException>(() => adapter.SetCharacterScalar(8, "level", 100));
        Assert.Throws<SaveEditorException>(() => adapter.SetWeapon(8, 1, 17));
    }

    [Fact]
    public void InventoryEditsPreserveOrderDuplicatesAndSynchronizeCounts()
    {
        Suikoden1Adapter adapter = new(Suikoden1TestFactory.Create());
        adapter.SetPartyItem(1, 25);
        adapter.SetCharacterItem(8, 1, 25, 0, 4);

        JsonArray partyItems = adapter.Document.Root["party_data"]!["party_item"]!.AsArray();
        JsonObject player = adapter.Document.Root["player_base"]!.AsArray()[0]!.AsObject();

        Assert.Equal(8, partyItems.Count);
        Assert.Equal(2, adapter.Document.Root["party_data"]!["party_item_kazu"]!.GetValue<int>());
        Assert.Equal(25, partyItems[0]!.GetValue<int>());
        Assert.Equal(25, partyItems[1]!.GetValue<int>());
        Assert.Equal(9, player["item"]!.AsArray().Count);
        Assert.Equal(2, player["item_kazu"]!.GetValue<int>());
    }

    [Fact]
    public void NoUnrelatedFieldsChangeAcrossEncryptedEditRoundTrip()
    {
        SaveDocument document = Suikoden1TestFactory.Create();
        JsonNode unknown = document.Root["unknown_root"]!.DeepClone();
        new Suikoden1Adapter(document).SetPotch(9999);

        string envelope = SaveCrypto.EncryptJson(document.ToJson());
        SaveDocument reopened = SaveDocument.Parse(SaveCrypto.DecryptEnvelope(envelope));

        Assert.True(JsonNode.DeepEquals(unknown, reopened.Root["unknown_root"]));
        Assert.Equal(9999, reopened.Root["party_data"]!["mochi_kin"]!.GetValue<int>());
    }

    [Fact]
    public void ValidationReportsNoErrorsForSyntheticValidSave()
    {
        IReadOnlyList<ValidationIssue> issues = new Suikoden1Adapter(Suikoden1TestFactory.Create()).Validate();
        Assert.DoesNotContain(issues, issue => issue.Severity == ValidationSeverity.Error);
    }
}
