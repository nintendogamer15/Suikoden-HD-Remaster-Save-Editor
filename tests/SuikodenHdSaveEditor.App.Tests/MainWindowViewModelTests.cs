// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.App.Services;
using SuikodenHdSaveEditor.App.ViewModels;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task OpenPopulatesDetectedGamePathAndOverviewFields()
    {
        using TestDirectory directory = new();
        string save = directory.CreateSave();
        MainWindowViewModel viewModel = CreateViewModel(directory);

        await viewModel.OpenPathAsync(save);

        Assert.Equal("Suikoden I", viewModel.DetectedGame);
        Assert.Equal(Path.GetFullPath(save), viewModel.OriginalPath);
        Assert.Contains(viewModel.Fields, field => field.Label == "Potch");
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.DoesNotContain("Synthetic Hero", File.ReadAllText(directory.RecentPath));
    }

    [Fact]
    public async Task FieldApplyUndoAndRedoUpdateLosslessDocument()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        EditorFieldViewModel potch = viewModel.Fields.Single(field => field.Label == "Potch");
        potch.Value = "9000";

        potch.ApplyCommand.Execute(null);

        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal(9000, JsonNode.Parse(viewModel.RawJson)!["party_data"]!["mochi_kin"]!.GetValue<int>());

        viewModel.UndoCommand.Execute(null);
        Assert.Equal(100, JsonNode.Parse(viewModel.RawJson)!["party_data"]!["mochi_kin"]!.GetValue<int>());

        viewModel.RedoCommand.Execute(null);
        Assert.Equal(9000, JsonNode.Parse(viewModel.RawJson)!["party_data"]!["mochi_kin"]!.GetValue<int>());
    }

    [Fact]
    public async Task InvalidFieldValueIsRejectedWithoutChangingJson()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        string before = viewModel.RawJson;
        EditorFieldViewModel potch = viewModel.Fields.Single(field => field.Label == "Potch");
        potch.Value = "not-a-number";

        potch.ApplyCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(JsonNode.Parse(before)!.ToJsonString(), JsonNode.Parse(viewModel.RawJson)!.ToJsonString());
    }

    [Fact]
    public void RecentFileStorePersistsPathsOnlyAndCapsEntries()
    {
        using TestDirectory directory = new();
        RecentFileStore store = new(directory.RecentPath);
        for (int index = 0; index < 12; index++)
        {
            store.Add(Path.Combine(directory.Path, $"Data{index}"));
        }

        IReadOnlyList<string> result = store.Load();

        Assert.Equal(10, result.Count);
        Assert.All(result, path => Assert.StartsWith(directory.Path, path, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SafeItemBulkCommandIsUnavailableForSuikoden1()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());

        Assert.False(viewModel.IsSuikoden2);
        Assert.False(viewModel.GiveAllSafeItemsCommand.CanExecute(null));
    }

    [Fact]
    public async Task CharacterFilterShowsOnlyCurrentParty()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        viewModel.SelectedSection = "Characters";

        viewModel.SelectedCharacterFilter = "Current party";

        ChoiceViewModel character = Assert.Single(viewModel.CharacterChoices);
        Assert.Equal(8, character.Id);
    }

    [Fact]
    public async Task GuidedFieldsShowNamesAndOnlyExposeCharacterControlsOnCharactersTab()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());

        viewModel.SelectedSection = "Inventory";
        EditorFieldViewModel item = viewModel.Fields.Single(field => field.Path == "party_data.party_item[0]");
        Assert.True(item.HasChoices);
        Assert.Contains(item.Choices, choice => choice.Contains("Medicine", StringComparison.Ordinal));
        Assert.DoesNotContain(item.Choices, choice => choice.Contains("— item ", StringComparison.Ordinal));
        int medicineId = SuikodenHdSaveEditor.Formats.Suikoden1.Suikoden1Catalog.Items.Single(entry => entry.Value == "Medicine").Key;
        item.Value = "Medicine";
        item.ApplyCommand.Execute(null);
        Assert.Equal(medicineId, JsonNode.Parse(viewModel.RawJson)!["party_data"]!["party_item"]![0]!.GetValue<int>());
        Assert.False(viewModel.IsCharacters);
        Assert.True(viewModel.IsInventory);

        viewModel.SelectedSection = "Advanced Data";
        Assert.False(viewModel.IsCharacters);
        Assert.False(viewModel.IsSearchVisible);

        viewModel.SelectedSection = "Characters";
        Assert.True(viewModel.IsCharacters);
        Assert.True(viewModel.IsSearchVisible);
    }

    [Fact]
    public async Task Suikoden2GuidedChoicesExposeItemCharacterRecruitmentAndCastleMeanings()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSuikoden2Save());

        viewModel.SelectedSection = "Inventory";
        EditorFieldViewModel itemField = viewModel.Fields.Single(field => field.Path == "party_data.party_item[0]");
        Suikoden2ItemDefinition regular34 = Suikoden2Catalog.FindItem(Suikoden2ItemCategory.Regular, 34);
        Assert.Equal("Medicine", itemField.Value);
        Assert.Contains(regular34.Name, itemField.Choices);
        Assert.DoesNotContain(itemField.Choices, choice => choice.Contains("Regular:", StringComparison.Ordinal));
        Assert.DoesNotContain(itemField.Choices, choice => choice.Contains("Trade:", StringComparison.Ordinal));
        EditorFieldViewModel quantity = viewModel.Fields.Single(field => field.Path == "party_data.party_item[0].use_cnt");
        Assert.Equal("3", quantity.Value);
        Assert.Equal(9, quantity.Choices.Count);
        quantity.Value = "5";
        quantity.ApplyCommand.Execute(null);
        Assert.Equal(5, JsonNode.Parse(viewModel.RawJson)!["party_data"]!["party_item"]![0]!["use_cnt"]!.GetValue<int>());
        itemField = viewModel.Fields.Single(field => field.Path == "party_data.party_item[0]");
        itemField.Value = regular34.Name;
        itemField.ApplyCommand.Execute(null);
        JsonNode changedItem = JsonNode.Parse(viewModel.RawJson)!;
        Assert.Equal(regular34.Id, changedItem["party_data"]!["party_item"]![0]!["item_no"]!.GetValue<int>());
        Assert.Equal(regular34.UseCount, changedItem["party_data"]!["party_item"]![0]!["use_cnt"]!.GetValue<int>());
        Assert.DoesNotContain(viewModel.Fields, field => field.Path == "party_data.party_item[0].use_cnt");

        EditorFieldViewModel bathPainting = viewModel.Fields.Single(field => field.Path == "game_data.furo_item[2]");
        Assert.Contains(Suikoden2Catalog.FindItem(Suikoden2ItemCategory.Trade, 18).Name, bathPainting.Choices);
        Assert.DoesNotContain(Suikoden2Catalog.FindItem(Suikoden2ItemCategory.Trade, 1).Name, bathPainting.Choices);
        Assert.DoesNotContain(bathPainting.Choices, choice => choice.Contains("Trade:", StringComparison.Ordinal));

        viewModel.SelectedSection = "Party";
        Assert.Contains(viewModel.Fields[0].Choices, choice => choice.Contains("Riou", StringComparison.Ordinal));

        viewModel.SelectedSection = "Recruitment";
        EditorFieldViewModel recruitment = viewModel.Fields.Single(field => field.Path == "chara_flag[1]");
        Assert.Contains("Recruited manually — 71", recruitment.Choices);
        Assert.Contains("Deceased — 212", recruitment.Choices);

        viewModel.SelectedSection = "Headquarters / Progress";
        EditorFieldViewModel castle = viewModel.Fields.Single(field => field.Label == "Castle level");
        Assert.Contains("Level 4 — Maximum", castle.Choices);
    }

    [Fact]
    public async Task Suikoden2HeroNameUpdatesBothPairedSaveFields()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSuikoden2Save());
        EditorFieldViewModel hero = viewModel.Fields.Single(field => field.Label == "Hero / save-list name");
        Assert.DoesNotContain(viewModel.Fields, field => field.Label == "Hero real name");

        hero.Value = "Edited Hero";
        hero.ApplyCommand.Execute(null);

        JsonNode edited = JsonNode.Parse(viewModel.RawJson)!;
        Assert.Equal("Edited Hero", edited["game_data"]!["bozu_name"]!.GetValue<string>());
        Assert.Equal("Edited Hero", edited["game_data"]!["bozu_name2"]!.GetValue<string>());
    }

    [Fact]
    public async Task ApplyAllCommitsSectionAsOneUndoableTransaction()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        viewModel.Fields.Single(field => field.Label == "Hero name").Value = "Edited Hero";
        viewModel.Fields.Single(field => field.Label == "Potch").Value = "9000";

        viewModel.ApplyAllCommand.Execute(null);

        JsonNode edited = JsonNode.Parse(viewModel.RawJson)!;
        Assert.Equal("Edited Hero", edited["playerName"]!.GetValue<string>());
        Assert.Equal(9000, edited["party_data"]!["mochi_kin"]!.GetValue<int>());

        viewModel.UndoCommand.Execute(null);
        JsonNode undone = JsonNode.Parse(viewModel.RawJson)!;
        Assert.Equal("Synthetic Hero", undone["playerName"]!.GetValue<string>());
        Assert.Equal(100, undone["party_data"]!["mochi_kin"]!.GetValue<int>());
    }

    [Fact]
    public async Task ApplyAllChangesS1PartyTogetherSoTirCanMoveSlots()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        viewModel.SelectedSection = "Party";
        Assert.All(viewModel.Fields, field => Assert.True(field.HasChoices));
        viewModel.Fields.Single(field => field.Path.EndsWith("[0]", StringComparison.Ordinal)).Value = "Empty — character -1";
        viewModel.Fields.Single(field => field.Path.EndsWith("[1]", StringComparison.Ordinal)).Value = "Tir McDohl — character 8";

        viewModel.ApplyAllCommand.Execute(null);

        JsonArray party = JsonNode.Parse(viewModel.RawJson)!["party_data"]!["chara_code"]!.AsArray();
        Assert.Equal(-1, party[0]!.GetValue<int>());
        Assert.Equal(8, party[1]!.GetValue<int>());
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ApplyAllOrdersDependentHpChangesAgainstTheFinalValues()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        viewModel.SelectedSection = "Characters";
        viewModel.Fields.Single(field => field.Label == "Current HP").Value = "120";
        viewModel.Fields.Single(field => field.Label == "Maximum HP").Value = "130";

        viewModel.ApplyAllCommand.Execute(null);

        JsonObject character = JsonNode.Parse(viewModel.RawJson)!["player_base"]![0]!.AsObject();
        Assert.Equal(120, character["hp"]!.GetValue<int>());
        Assert.Equal(130, character["max_hp"]!.GetValue<int>());
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task MaximizeAndEquipPartyCommandIsCharacterOnlyAndUndoable()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        Assert.False(viewModel.MaximizeAndEquipPartyCommand.CanExecute(null));
        viewModel.SelectedSection = "Characters";
        Assert.True(viewModel.MaximizeAndEquipPartyCommand.CanExecute(null));

        viewModel.MaximizeAndEquipPartyCommand.Execute(null);

        JsonObject optimized = JsonNode.Parse(viewModel.RawJson)!["player_base"]![0]!.AsObject();
        Assert.Equal(99, optimized["level"]!.GetValue<int>());
        Assert.Equal(9999, optimized["hp"]!.GetValue<int>());
        Assert.Equal(16, optimized["buki_data"]!["level"]!.GetValue<int>());
        Assert.Contains("Maximized 1 active battle character", viewModel.StatusMessage, StringComparison.Ordinal);
        viewModel.UndoCommand.Execute(null);
        Assert.Equal(1, JsonNode.Parse(viewModel.RawJson)!["player_base"]![0]!["level"]!.GetValue<int>());
    }

    [Fact]
    public async Task HeadquartersLevelIsCappedChoiceAndInvalidApplyAllIsAtomic()
    {
        using TestDirectory directory = new();
        MainWindowViewModel viewModel = CreateViewModel(directory);
        await viewModel.OpenPathAsync(directory.CreateSave());
        EditorFieldViewModel level = viewModel.Fields.Single(field => field.Label == "Headquarters level");
        Assert.Equal(5, level.Choices.Count);
        Assert.Contains("Level 4 — Maximum", level.Choices);
        string before = viewModel.RawJson;
        viewModel.Fields.Single(field => field.Label == "Potch").Value = "9000";
        level.Value = "Level 5 — Invalid";

        viewModel.ApplyAllCommand.Execute(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(JsonNode.Parse(before)!.ToJsonString(), JsonNode.Parse(viewModel.RawJson)!.ToJsonString());
    }

    [Fact]
    public async Task PrivateOptInCopiesAndOpensEverySaveInViewModel()
    {
        string? saveRoot = Environment.GetEnvironmentVariable("SUIKODEN_PRIVATE_SAVE_ROOT");
        if (string.IsNullOrWhiteSpace(saveRoot) || !Directory.Exists(saveRoot))
        {
            return;
        }

        using TestDirectory directory = new();
        string[] originals = Directory.EnumerateFiles(saveRoot, "Data*", SearchOption.AllDirectories)
            .Where(path => SlotDetector.FromPath(path).HasValue)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(originals);
        foreach (string original in originals)
        {
            string copy = System.IO.Path.Combine(directory.Path, $"{new DirectoryInfo(System.IO.Path.GetDirectoryName(original)!).Name}-{System.IO.Path.GetFileName(original)}");
            File.Copy(original, copy);
            MainWindowViewModel viewModel = CreateViewModel(directory);

            await viewModel.OpenPathAsync(copy);

            Assert.True(viewModel.HasDocument);
            Assert.False(viewModel.HasError);
            Assert.NotEmpty(viewModel.Fields);
            foreach (string section in viewModel.Sections)
            {
                viewModel.SelectedSection = section;
                if (viewModel.IsFieldEditor)
                {
                    Assert.NotEmpty(viewModel.Fields);
                }
            }
        }
    }

    private static MainWindowViewModel CreateViewModel(TestDirectory directory) =>
        new(new FakeInteraction(), new RecentFileStore(directory.RecentPath));

    private sealed class FakeInteraction : IUserInteraction
    {
        public Task<string?> PickSaveToOpenAsync() => Task.FromResult<string?>(null);

        public Task<string?> PickSaveFolderAsync() => Task.FromResult<string?>(null);

        public Task<string?> PickSaveDestinationAsync(string suggestedName) => Task.FromResult<string?>(null);

        public Task<bool> ConfirmAsync(string title, string message, string acceptText) => Task.FromResult(true);

        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;

        public Task ShowAboutAsync(string content) => Task.CompletedTask;
    }

    private sealed class TestDirectory : IDisposable
    {
        internal TestDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"suikoden-app-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
            RecentPath = System.IO.Path.Combine(Path, "recent.json");
        }

        internal string Path { get; }

        internal string RecentPath { get; }

        internal string CreateSave()
        {
            const string json = """
                {
                  "version": 8,
                  "party_data": {"chara_code":[8,-1,-1,-1,-1,-1],"player_kazu":1,"mochi_kin":100,"party_item_kazu":0,"party_item":[0,0,0,0,0,0,0,0]},
                  "shiro_data": {"level":1},
                  "player_base": [{"chara_no":8,"max_hp":20,"hp":20,"magic_point":[0,0,0,0,0],"level":1,"exp":0,"noryoku":[1,1,1,1,1,1],"status":0,"item_kazu":0,"item":[{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0}],"buki_data":{"buki_id":1,"level":1,"monsyo":[0,0,0,0,0,0]},"monsyo_data":{"monsyo_id":1,"monsyo_level":0,"monsyo_exp":0}}],
                  "member_flag": [0,0,0,0,0,0,0,0,9],
                  "playerName": "Synthetic Hero",
                  "playerCName": "Synthetic HQ",
                  "playTime": 1,
                  "private_unknown": {"keep":true}
                }
                """;
            string path = System.IO.Path.Combine(Path, "Data1");
            File.WriteAllText(path, SaveCrypto.EncryptJson(json));
            return path;
        }

        internal string CreateSuikoden2Save()
        {
            JsonObject root = new()
            {
                ["version"] = 100,
                ["game_data"] = new JsonObject
                {
                    ["bozu_name"] = "Synthetic Hero",
                    ["bozu_name2"] = "Synthetic Real Hero",
                    ["kari_name"] = StringArray(6),
                    ["macd_name"] = string.Empty,
                    ["base_name"] = "Synthetic Castle",
                    ["m_base_name"] = "Synthetic S1 HQ",
                    ["team_name"] = "Synthetic Army",
                    ["base_lv"] = 1,
                    ["kaji_lv"] = 1,
                    ["nakam_1_num"] = 0,
                    ["play_time"] = IntArray(1, 2, 3),
                    ["base_item"] = ItemArray(60),
                    ["furo_item"] = ItemArray(8, 64),
                    ["room_item"] = ItemArray(8, 64),
                    ["furo_info"] = IntArray(2),
                    ["food_menu"] = IntArray(7),
                    ["food_resipi"] = IntArray(5),
                    ["food_num"] = IntArray(12),
                    ["tantei_lv"] = IntArray(65),
                    ["hon_flag"] = IntArray(50),
                    ["area_no"] = 0,
                    ["s_area_no"] = 0,
                    ["town_no"] = 0,
                    ["s_town_no"] = 0,
                    ["area_no2"] = 0,
                    ["town_no2"] = 0,
                    ["map_no"] = 0,
                    ["s_map_no"] = 0,
                },
                ["chara_data"] = new JsonObject
                {
                    ["c_varia_dat"] = new JsonArray(Enumerable.Range(0, 85).Select(id => (JsonNode?)CreateSuikoden2Character(id)).ToArray()),
                    ["c_kotei_dat"] = new JsonArray(Enumerable.Range(0, 85).Select(_ => (JsonNode?)new JsonObject()).ToArray()),
                },
                ["party_data"] = new JsonObject
                {
                    ["party_cha_no"] = IntArray(1, 2, 3, 4, 5, 6, 0, 0),
                    ["party_item"] = ItemArray(30),
                    ["event_item"] = IntArray(10),
                    ["ninki"] = 5,
                    ["gold"] = 1000,
                },
                ["chara_flag"] = IntArray(128),
                ["event_flag"] = IntArray(256),
                ["t_box_flag"] = IntArray(32),
                ["px"] = 0,
                ["py"] = 0,
            };
            root["chara_flag"]![1] = 71;
            root["party_data"]!["party_item"]![0]!["item_no"] = 1;
            root["party_data"]!["party_item"]![0]!["use_cnt"] = 3;
            string path = System.IO.Path.Combine(Path, "Data2");
            File.WriteAllText(path, SaveCrypto.EncryptJson(root.ToJsonString()));
            return path;
        }

        private static JsonObject CreateSuikoden2Character(int id) => new()
        {
            ["level"] = 10,
            ["exp"] = 20,
            ["now_hp"] = 90,
            ["max_hp"] = 100,
            ["mp"] = IntArray(0, 17, 34, 51),
            ["para"] = IntArray(10, 11, 12, 13, 14, 15, 16),
            ["buki_lv"] = 5,
            ["buki_mon"] = 0,
            ["mon_eqp"] = IntArray(3),
            ["bogu_eqp"] = IntArray(3),
            ["item_eqp"] = ItemArray(3),
            ["todome"] = id,
        };

        private static JsonArray ItemArray(int count, int emptyUseCount = 0) => new(Enumerable.Range(0, count).Select(_ => (JsonNode?)new JsonObject
        {
            ["item_no"] = 0,
            ["use_cnt"] = emptyUseCount,
        }).ToArray());

        private static JsonArray IntArray(params int[] values) => new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

        private static JsonArray IntArray(int count) => new(Enumerable.Repeat(0, count).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

        private static JsonArray StringArray(int count) => new(Enumerable.Range(0, count).Select(index => (JsonNode?)JsonValue.Create($"Alias {index}")).ToArray());

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
