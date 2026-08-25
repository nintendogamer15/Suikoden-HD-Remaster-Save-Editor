// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.App.Services;
using SuikodenHdSaveEditor.App.ViewModels;
using SuikodenHdSaveEditor.Core;

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

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
