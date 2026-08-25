// SPDX-License-Identifier: 0BSD
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Windows.Input;
using SuikodenHdSaveEditor.App.Services;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    public const string ApplicationTitle = "Suikoden I & II HD Remaster Save Editor";
    public const string CreditsAndLicenses = """
        Suikoden I & II HD Remaster Save Editor

        CREDITS — PROJECTS THAT MADE THIS EDITOR POSSIBLE

        d3xMachina — SuikodenSaveDecrypter
        https://github.com/d3xMachina/SuikodenSaveDecrypter
        MIT licensed. Its implementation established the exact encrypted GR_DATA save envelope, password derivation, AES settings, salt layout, and compatibility oracle.

        d3xMachina — Suikoden-Fix
        https://github.com/d3xMachina/Suikoden-Fix
        MIT licensed. It enabled and documented decrypted-save workflows, backups, and optional-mod behavior.

        faospark — Suikoden II HD Remaster Save Editor
        https://github.com/faospark/suisaveeditor
        MIT licensed. Its current code, schema, constants, restrictions, renderers, changelog, and game data informed the Suikoden II editor.

        asilverthorn — Suikoden reference documentation
        https://github.com/asilverthorn/suikoden_ref
        Credited Suikoden I and II save research. No obvious license file was present when inspected; this project does not call it MIT or reproduce its prose wholesale.

        Additional factual provenance credited by upstream work: Suikosource's Suikoden II item-digits guide, makotech222/suiko2edit, and nesrak1/UABEA.

        LICENSES

        Original project code: Zero-Clause BSD (0BSD).
        Substantially ported upstream portions: MIT, with retained copyright notices.
        Avalonia and .NET: MIT. Inter font: SIL Open Font License 1.1.
        Full texts ship in LICENSES/ and THIRD_PARTY_NOTICES.md.

        FAN-PROJECT DISCLAIMER

        This independent fan project is not affiliated with, authorized by, sponsored by, or endorsed by Konami or any upstream author. Suikoden and related names are trademarks of their respective owners. No Konami logos, extracted artwork, music, fonts, or other proprietary game assets are included.

        Privacy: saves are processed locally. The application has no network code, telemetry, or save-content persistence. Recent files store paths only.
        """;

    private static readonly string[] SectionNames =
    [
        "Overview",
        "Party",
        "Characters",
        "Inventory",
        "Recruitment",
        "Headquarters / Progress",
        "Advanced Data",
        "Credits / Licenses",
    ];

    private readonly IUserInteraction interaction;
    private readonly RecentFileStore recentFileStore;
    private readonly SaveFileService saveFileService = new();
    private readonly EditHistory history = new();
    private readonly List<ChoiceViewModel> allCharacters = [];
    private readonly List<EditorFieldViewModel> allFields = [];
    private SaveDocument? document;
    private string selectedSection = SectionNames[0];
    private string searchText = string.Empty;
    private string statusMessage = "Open an encrypted Data file or a save folder to begin.";
    private string errorMessage = string.Empty;
    private string detectedGame = "No save open";
    private string slotText = "—";
    private string originalPath = "—";
    private string backupPath = "—";
    private string rawJson = string.Empty;
    private string? selectedRecentFile;
    private ChoiceViewModel? selectedCharacter;
    private SlotEntryViewModel? selectedSlot;
    private bool betterLeonaEnabled;
    private bool krakenRecruitmentEnabled;
    private bool hasUnsavedChanges;

    public MainWindowViewModel(IUserInteraction interaction, RecentFileStore? recentFileStore = null)
    {
        this.interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        this.recentFileStore = recentFileStore ?? new RecentFileStore();
        Sections = new ObservableCollection<string>(SectionNames);
        Fields = [];
        CharacterChoices = [];
        AvailableSlots = [];
        RecentFiles = new ObservableCollection<string>(this.recentFileStore.Load());

        OpenSaveCommand = new AsyncRelayCommand(OpenSaveAsync);
        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        OpenSelectedSlotCommand = new AsyncRelayCommand(OpenSelectedSlotAsync, () => SelectedSlot is not null);
        OpenRecentCommand = new AsyncRelayCommand(OpenRecentAsync, () => !string.IsNullOrWhiteSpace(SelectedRecentFile));
        SaveAsCommand = new AsyncRelayCommand(SaveAsAsync, () => document is not null);
        OverwriteCommand = new AsyncRelayCommand(OverwriteAsync, () => document?.OriginalPath is not null);
        ReloadCommand = new AsyncRelayCommand(ReloadAsync, () => document?.OriginalPath is not null);
        UndoCommand = new RelayCommand(Undo, () => history.CanUndo);
        RedoCommand = new RelayCommand(Redo, () => history.CanRedo);
        GiveAllSafeItemsCommand = new AsyncRelayCommand(GiveAllSafeItemsAsync, () => document?.Game == GameKind.Suikoden2);
        AboutCommand = new AsyncRelayCommand(() => interaction.ShowAboutAsync(CreditsAndLicenses));
    }

    public ObservableCollection<string> Sections { get; }

    public ObservableCollection<EditorFieldViewModel> Fields { get; }

    public ObservableCollection<ChoiceViewModel> CharacterChoices { get; }

    public ObservableCollection<SlotEntryViewModel> AvailableSlots { get; }

    public ObservableCollection<string> RecentFiles { get; }

    public ICommand OpenSaveCommand { get; }

    public ICommand OpenFolderCommand { get; }

    public ICommand OpenSelectedSlotCommand { get; }

    public ICommand OpenRecentCommand { get; }

    public ICommand SaveAsCommand { get; }

    public ICommand OverwriteCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand UndoCommand { get; }

    public ICommand RedoCommand { get; }

    public ICommand GiveAllSafeItemsCommand { get; }

    public ICommand AboutCommand { get; }

    public string Title => ApplicationTitle + (HasUnsavedChanges ? " *" : string.Empty);

    public string SelectedSection
    {
        get => selectedSection;
        set
        {
            if (SetProperty(ref selectedSection, value))
            {
                OnPropertyChanged(nameof(IsAdvancedData));
                OnPropertyChanged(nameof(IsCredits));
                OnPropertyChanged(nameof(IsFieldEditor));
                RefreshCharacterFilter();
                RebuildFields();
            }
        }
    }

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                RefreshCharacterFilter();
                RebuildFields();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => ErrorMessage.Length > 0;

    public string DetectedGame
    {
        get => detectedGame;
        private set => SetProperty(ref detectedGame, value);
    }

    public string SlotText
    {
        get => slotText;
        private set => SetProperty(ref slotText, value);
    }

    public string OriginalPath
    {
        get => originalPath;
        private set => SetProperty(ref originalPath, value);
    }

    public string BackupPath
    {
        get => backupPath;
        private set => SetProperty(ref backupPath, value);
    }

    public string RawJson
    {
        get => rawJson;
        private set => SetProperty(ref rawJson, value);
    }

    public string CreditsText { get; } = CreditsAndLicenses;

    public string? SelectedRecentFile
    {
        get => selectedRecentFile;
        set
        {
            if (SetProperty(ref selectedRecentFile, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public ChoiceViewModel? SelectedCharacter
    {
        get => selectedCharacter;
        set
        {
            if (SetProperty(ref selectedCharacter, value) && SelectedSection == "Characters")
            {
                RebuildFields();
            }
        }
    }

    public SlotEntryViewModel? SelectedSlot
    {
        get => selectedSlot;
        set
        {
            if (SetProperty(ref selectedSlot, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool HasDocument => document is not null;

    public bool IsSuikoden2 => document?.Game == GameKind.Suikoden2;

    public bool BetterLeonaEnabled
    {
        get => betterLeonaEnabled;
        set
        {
            if (SetProperty(ref betterLeonaEnabled, value) && SelectedSection == "Recruitment")
            {
                RebuildFields();
            }
        }
    }

    public bool KrakenRecruitmentEnabled
    {
        get => krakenRecruitmentEnabled;
        set
        {
            if (SetProperty(ref krakenRecruitmentEnabled, value) && SelectedSection == "Recruitment")
            {
                RebuildFields();
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(UnsavedText));
            }
        }
    }

    public string UnsavedText => HasUnsavedChanges ? "Unsaved changes" : "Saved / unchanged";

    public bool IsAdvancedData => SelectedSection == "Advanced Data";

    public bool IsCredits => SelectedSection == "Credits / Licenses";

    public bool IsFieldEditor => !IsAdvancedData && !IsCredits;

    public async Task OpenPathAsync(string path)
    {
        if (!await CanDiscardCurrentAsync().ConfigureAwait(true))
        {
            return;
        }

        try
        {
            SaveDocument opened = SaveDocument.OpenEncrypted(path);
            document = opened;
            history.Clear();
            BackupPath = "—";
            ErrorMessage = string.Empty;
            SelectedSection = "Overview";
            UpdateRecentFiles(path);
            PopulateCharacters();
            RefreshDocumentState();
            IReadOnlyList<ValidationIssue> issues = ValidateCurrent();
            int warnings = issues.Count(issue => issue.Severity == ValidationSeverity.Warning);
            StatusMessage = $"Opened {Path.GetFileName(path)} as {DetectedGame}. {warnings} warning(s); unknown data is preserved.";
        }
        catch (Exception exception) when (exception is SaveEditorException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = exception.Message;
            await interaction.ShowMessageAsync("Could not open save", exception.Message).ConfigureAwait(true);
        }
    }

    private async Task OpenSaveAsync()
    {
        string? path = await interaction.PickSaveToOpenAsync().ConfigureAwait(true);
        if (path is not null)
        {
            await OpenPathAsync(path).ConfigureAwait(true);
        }
    }

    private async Task OpenFolderAsync()
    {
        string? path = await interaction.PickSaveFolderAsync().ConfigureAwait(true);
        if (path is null)
        {
            return;
        }

        try
        {
            IReadOnlyList<SaveSlotEntry> slots = SaveSlotBrowser.Discover(path);
            AvailableSlots.Clear();
            foreach (SaveSlotEntry slot in slots)
            {
                AvailableSlots.Add(new(slot.Path, slot.Slot, slot.GameHint == GameKind.Suikoden1 ? "Suikoden I" : "Suikoden II"));
            }

            SelectedSlot = AvailableSlots.FirstOrDefault();
            StatusMessage = slots.Count == 0
                ? "No Data0–Data16 files were found in gsd1/gsd2."
                : $"Found {slots.Count} slot file(s). Choose one beside the toolbar and open it.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = exception.Message;
        }
    }

    private Task OpenSelectedSlotAsync() => SelectedSlot is null ? Task.CompletedTask : OpenPathAsync(SelectedSlot.Path);

    private Task OpenRecentAsync() => string.IsNullOrWhiteSpace(SelectedRecentFile)
        ? Task.CompletedTask
        : OpenPathAsync(SelectedRecentFile);

    private async Task SaveAsAsync()
    {
        if (document is null || !await ConfirmValidSaveAsync().ConfigureAwait(true))
        {
            return;
        }

        string currentName = Path.GetFileName(document.OriginalPath ?? "Data1");
        string? destination = await interaction.PickSaveDestinationAsync(currentName + ".edited").ConfigureAwait(true);
        if (destination is null)
        {
            return;
        }

        try
        {
            SaveWriteResult result = saveFileService.SaveAs(document, destination);
            BackupPath = result.BackupPath ?? "—";
            UpdateRecentFiles(result.DestinationPath);
            RefreshDocumentState();
            StatusMessage = $"Validated encrypted save written to {result.DestinationPath}. Test it in game before removing backups.";
        }
        catch (SaveEditorException exception)
        {
            ErrorMessage = exception.Message;
            await interaction.ShowMessageAsync("Save As failed", exception.Message).ConfigureAwait(true);
        }
    }

    private async Task OverwriteAsync()
    {
        if (document?.OriginalPath is null || !await ConfirmValidSaveAsync().ConfigureAwait(true))
        {
            return;
        }

        bool accepted = await interaction.ConfirmAsync(
            "Overwrite with backup",
            $"This will create a timestamped backup, then atomically replace:\n{document.OriginalPath}\n\nSteam Cloud may restore an older copy. Close the game first.",
            "Create backup and overwrite").ConfigureAwait(true);
        if (!accepted)
        {
            return;
        }

        try
        {
            SaveWriteResult result = saveFileService.OverwriteWithBackup(document, document.OriginalPath);
            BackupPath = result.BackupPath ?? "—";
            RefreshDocumentState();
            StatusMessage = $"Overwrite verified. Backup: {BackupPath}";
        }
        catch (SaveEditorException exception)
        {
            ErrorMessage = exception.Message;
            await interaction.ShowMessageAsync("Overwrite failed", exception.Message).ConfigureAwait(true);
        }
    }

    private async Task ReloadAsync()
    {
        if (document?.OriginalPath is null)
        {
            return;
        }

        string path = document.OriginalPath;
        if (HasUnsavedChanges)
        {
            bool accepted = await interaction.ConfirmAsync("Discard changes?", "Reloading discards all unsaved edits.", "Reload").ConfigureAwait(true);
            if (!accepted)
            {
                return;
            }
        }

        document = null;
        HasUnsavedChanges = false;
        await OpenPathAsync(path).ConfigureAwait(true);
    }

    private void Undo()
    {
        if (document is null)
        {
            return;
        }

        HistoryResult result = history.Undo(document.Root);
        if (!result.Applied)
        {
            return;
        }

        string? path = document.OriginalPath;
        document = SaveDocument.Parse(result.Root.ToJsonString(), path);
        document.MarkChanged();
        PopulateCharacters(preserveSelection: true);
        RefreshDocumentState();
        StatusMessage = $"Undid: {result.Description}";
    }

    private void Redo()
    {
        if (document is null)
        {
            return;
        }

        HistoryResult result = history.Redo(document.Root);
        if (!result.Applied)
        {
            return;
        }

        string? path = document.OriginalPath;
        document = SaveDocument.Parse(result.Root.ToJsonString(), path);
        document.MarkChanged();
        PopulateCharacters(preserveSelection: true);
        RefreshDocumentState();
        StatusMessage = $"Redid: {result.Description}";
    }

    private async Task GiveAllSafeItemsAsync()
    {
        if (document?.Game != GameKind.Suikoden2)
        {
            return;
        }

        bool accepted = await interaction.ConfirmAsync(
            "Fill empty party-inventory slots?",
            "This fills only empty party-inventory slots with reviewed, non-key regular items. It excludes story-critical items, keeps existing items and ordering, and never grows the container.",
            "Give safe items").ConfigureAwait(true);
        if (!accepted)
        {
            return;
        }

        int added = 0;
        ApplyEdit("Filled empty party-inventory slots with safe items", () =>
        {
            added = new Suikoden2Adapter(document).GiveAllSafePartyItems();
        });
        if (!HasError)
        {
            StatusMessage = added == 0
                ? "No empty party-inventory slots or new reviewed safe items were available."
                : $"Added {added} reviewed non-key item(s) to empty party-inventory slots.";
        }
    }

    private void ApplyEdit(string description, Action edit)
    {
        if (document is null)
        {
            return;
        }

        JsonObject before = (JsonObject)document.Root.DeepClone();
        bool wasUnsaved = document.HasUnsavedChanges;
        try
        {
            edit();
            JsonObject after = (JsonObject)document.Root.DeepClone();
            history.Record(description, before, after);
            ErrorMessage = string.Empty;
            StatusMessage = description;
            RefreshDocumentState();
        }
        catch (Exception exception) when (exception is SaveEditorException or FormatException or OverflowException or KeyNotFoundException)
        {
            string? path = document.OriginalPath;
            document = SaveDocument.Parse(before.ToJsonString(), path);
            if (wasUnsaved)
            {
                document.MarkChanged();
            }

            ErrorMessage = exception.Message;
            RefreshDocumentState();
        }
    }

    private void RefreshDocumentState()
    {
        if (document is null)
        {
            return;
        }

        DetectedGame = document.Game == GameKind.Suikoden1 ? "Suikoden I" : "Suikoden II";
        SlotText = document.Slot?.ToString(CultureInfo.InvariantCulture) ?? "Unknown";
        OriginalPath = document.OriginalPath ?? "Unsaved document";
        HasUnsavedChanges = document.HasUnsavedChanges;
        RawJson = document.ToJson(indented: true);
        RebuildFields();
        OnPropertyChanged(nameof(HasDocument));
        OnPropertyChanged(nameof(IsSuikoden2));
        RaiseCommandStates();
    }

    private void PopulateCharacters(bool preserveSelection = false)
    {
        int? previous = preserveSelection ? SelectedCharacter?.Id : null;
        allCharacters.Clear();
        if (document?.Game == GameKind.Suikoden1)
        {
            allCharacters.AddRange(new Suikoden1Adapter(document).Characters
                .Select(character => new ChoiceViewModel(character.Id, character.Name))
                .OrderBy(character => character.Id));
        }
        else if (document?.Game == GameKind.Suikoden2)
        {
            allCharacters.AddRange(new Suikoden2Adapter(document).Characters
                .Where(character => character.Id > 0 && Suikoden2Catalog.Character(character.Id) is not null)
                .Select(character => new ChoiceViewModel(character.Id, character.Name)));
        }

        RefreshCharacterFilter(previous);
    }

    private void RefreshCharacterFilter(int? preferredId = null)
    {
        if (allCharacters.Count == 0)
        {
            CharacterChoices.Clear();
            SelectedCharacter = null;
            return;
        }

        int? current = preferredId ?? SelectedCharacter?.Id;
        IEnumerable<ChoiceViewModel> choices = allCharacters;
        if (SelectedSection == "Characters" && SearchText.Trim().Length > 0)
        {
            string query = SearchText.Trim();
            choices = choices.Where(choice => choice.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || choice.Id.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        ChoiceViewModel[] filtered = choices.ToArray();
        CharacterChoices.Clear();
        foreach (ChoiceViewModel choice in filtered)
        {
            CharacterChoices.Add(choice);
        }

        SelectedCharacter = CharacterChoices.FirstOrDefault(choice => choice.Id == current) ?? CharacterChoices.FirstOrDefault();
    }

    private void RebuildFields()
    {
        allFields.Clear();
        if (document is null)
        {
            Fields.Clear();
            return;
        }

        if (document.Game == GameKind.Suikoden1)
        {
            BuildSuikoden1Fields(new Suikoden1Adapter(document));
        }
        else
        {
            BuildSuikoden2Fields(new Suikoden2Adapter(document));
        }

        string query = SearchText.Trim();
        IEnumerable<EditorFieldViewModel> visible = allFields;
        if (query.Length > 0 && SelectedSection is not ("Characters" or "Inventory"))
        {
            visible = visible.Where(field => field.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                || field.Path.Contains(query, StringComparison.OrdinalIgnoreCase)
                || field.Warning.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Fields.Clear();
        foreach (EditorFieldViewModel field in visible)
        {
            Fields.Add(field);
        }
    }

    private void BuildSuikoden1Fields(Suikoden1Adapter adapter)
    {
        switch (SelectedSection)
        {
            case "Overview":
                AddReadOnly("Detected game", "schema", "Suikoden I");
                AddString("Hero name", "playerName", adapter.HeroName, value => adapter.SetNames(value, adapter.HeadquartersName));
                AddString("Headquarters name", "playerCName", adapter.HeadquartersName, value => adapter.SetNames(adapter.HeroName, value));
                AddNumber("Potch", "party_data.mochi_kin", adapter.Potch, adapter.SetPotch);
                AddReadOnly("Play time (raw seconds/ticks)", "playTime", adapter.PlayTime.ToString(CultureInfo.InvariantCulture));
                AddReadOnly("Headquarters level", "shiro_data.level", adapter.HeadquartersLevel.ToString(CultureInfo.InvariantCulture));
                break;
            case "Party":
                BuildSuikoden1Party(adapter);
                break;
            case "Characters":
                BuildSuikoden1Character(adapter);
                break;
            case "Inventory":
                BuildSuikoden1Inventory(adapter);
                break;
            case "Recruitment":
                BuildSuikoden1Recruitment(adapter);
                break;
            case "Headquarters / Progress":
                AddReadOnly("Headquarters level", "shiro_data.level", adapter.HeadquartersLevel.ToString(CultureInfo.InvariantCulture));
                AddReadOnly("Unexposed headquarters fields", "shiro_data", "Preserved in Advanced Data", "Only the headquarters level is confidently identified. Other shiro_data fields are intentionally read-only.");
                AddReadOnly("Story flags", "tmpEventFlagS / storyFlagS", "Preserved in Advanced Data", "Meanings and safe transitions are not sufficiently documented for normal editing.");
                break;
        }
    }

    private void BuildSuikoden1Party(Suikoden1Adapter adapter)
    {
        int[] values = adapter.PartyCharacterIds.ToArray();
        for (int index = 0; index < values.Length; index++)
        {
            int captured = index;
            string name = values[index] == -1 ? "Empty" : Suikoden1Catalog.CharacterName(values[index]);
            AddNumber(
                $"Party slot {index + 1} · {name}",
                $"party_data.chara_code[{index}]",
                values[index],
                value =>
                {
                    int[] changed = adapter.PartyCharacterIds.ToArray();
                    changed[captured] = value;
                    adapter.SetParty(changed);
                },
                index == 0 ? "Tir (8) must remain somewhere in the six slots. Use -1 only for an empty slot." : "Use -1 for an empty slot; only IDs with player_base battle records are accepted.");
        }
    }

    private void BuildSuikoden1Character(Suikoden1Adapter adapter)
    {
        if (SelectedCharacter is null)
        {
            AddReadOnly("Character search", "characters", "No matching character");
            return;
        }

        int id = SelectedCharacter.Id;
        Suikoden1CharacterView character = adapter.Characters.Single(value => value.Id == id);
        AddNumber("Level", $"player_base[{id}].level", character.Level, value => adapter.SetCharacterScalar(id, "level", value));
        AddNumber("EXP", $"player_base[{id}].exp", character.Experience, value => adapter.SetCharacterScalar(id, "exp", value));
        AddNumber("Current HP", $"player_base[{id}].hp", character.CurrentHp, value => adapter.SetCharacterScalar(id, "hp", value));
        AddNumber("Maximum HP", $"player_base[{id}].max_hp", character.MaximumHp, value => adapter.SetCharacterScalar(id, "max_hp", value));
        for (int spell = 1; spell <= 4; spell++)
        {
            int captured = spell;
            AddNumber($"Current MP · spell level {spell}", $"player_base[{id}].magic_point[{spell}]", character.CurrentMagicPoints[spell], value => adapter.SetMagicPoint(id, captured, value));
        }

        for (int stat = 0; stat < Suikoden1Adapter.StatNames.Count; stat++)
        {
            int captured = stat;
            AddNumber(Suikoden1Adapter.StatNames[stat], $"player_base[{id}].noryoku[{stat}]", character.Stats[stat], value => adapter.SetCharacterStat(id, captured, value));
        }

        AddNumber("Weapon ID", $"player_base[{id}].buki_data.buki_id", character.WeaponId, value => adapter.SetWeapon(id, value, adapter.Characters.Single(item => item.Id == id).WeaponLevel), "Weapon-name mappings are not verified, so only the numeric ID is shown.");
        AddNumber("Weapon level", $"player_base[{id}].buki_data.level", character.WeaponLevel, value => adapter.SetWeapon(id, adapter.Characters.Single(item => item.Id == id).WeaponId, value));
        AddNumber("Equipped rune ID", $"player_base[{id}].monsyo_data.monsyo_id", character.RuneId, value => adapter.SetCharacterRune(id, value));
        for (int index = 0; index < character.WeaponRunePieces.Count; index++)
        {
            int captured = index;
            AddNumber($"Weapon rune-piece value {index}", $"player_base[{id}].buki_data.monsyo[{index}]", character.WeaponRunePieces[index], value => adapter.SetWeaponRunePiece(id, captured, value));
        }

        for (int slot = 0; slot < character.Items.Count; slot++)
        {
            int captured = slot;
            JsonObject item = character.Items[slot]!.AsObject();
            int itemId = item["item_id"]!.GetValue<int>();
            int equipment = item["soubi"]!.GetValue<int>();
            int uses = item["data"]!.GetValue<int>();
            AddNumber($"Carried item {slot + 1} ID · {Suikoden1Catalog.ItemName(itemId)}", $"player_base[{id}].item[{slot}].item_id", itemId, value => SetS1ItemPart(adapter, id, captured, value, null, null));
            AddNumber($"Carried item {slot + 1} equipment slot", $"player_base[{id}].item[{slot}].soubi", equipment, value => SetS1ItemPart(adapter, id, captured, null, value, null), equipment >= 129 ? "Values 129–133 are verified non-removable equipment states." : null);
            AddNumber($"Carried item {slot + 1} remaining uses", $"player_base[{id}].item[{slot}].data", uses, value => SetS1ItemPart(adapter, id, captured, null, null, value));
        }
    }

    private void BuildSuikoden1Inventory(Suikoden1Adapter adapter)
    {
        JsonArray items = adapter.Document.Root["party_data"]!["party_item"]!.AsArray();
        for (int index = 0; index < items.Count; index++)
        {
            int captured = index;
            int id = items[index]!.GetValue<int>();
            AddNumber($"Party item {index + 1} · {Suikoden1Catalog.ItemName(id)}", $"party_data.party_item[{index}]", id, value => adapter.SetPartyItem(captured, value));
        }

        if (SearchText.Trim().Length > 0)
        {
            string query = SearchText.Trim();
            foreach ((int id, string name) in Suikoden1Catalog.Items.Where(item => item.Value.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Key.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase)).Take(200))
            {
                AddReadOnly($"Catalogue · {id}: {name}", "item reference", id.ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    private void BuildSuikoden1Recruitment(Suikoden1Adapter adapter)
    {
        JsonArray flags = adapter.Document.Root["member_flag"]!.AsArray();
        int maximum = Math.Min(flags.Count, Suikoden1Catalog.Characters.Keys.Max() + 1);
        for (int id = 0; id < maximum; id++)
        {
            int captured = id;
            int current = flags[id]!.GetValue<int>();
            AddString(
                $"{id}: {Suikoden1Catalog.CharacterName(id)}",
                $"member_flag[{id}]",
                current == 9 ? "recruited" : "unrecruited",
                value => adapter.SetRecruited(captured, ParseRecruitmentBoolean(value)),
                "Recruitment edits can break story progression or required-party events. Accepted: recruited/unrecruited, true/false, 9/0.");
        }
    }

    private void BuildSuikoden2Fields(Suikoden2Adapter adapter)
    {
        switch (SelectedSection)
        {
            case "Overview":
                BuildSuikoden2Overview(adapter);
                break;
            case "Party":
                BuildSuikoden2Party(adapter);
                break;
            case "Characters":
                BuildSuikoden2Character(adapter);
                break;
            case "Inventory":
                BuildSuikoden2Inventory(adapter);
                break;
            case "Recruitment":
                BuildSuikoden2Recruitment(adapter);
                break;
            case "Headquarters / Progress":
                BuildSuikoden2Progress(adapter);
                break;
        }
    }

    private void BuildSuikoden2Overview(Suikoden2Adapter adapter)
    {
        JsonObject game = adapter.Document.Root["game_data"]!.AsObject();
        AddReadOnly("Detected game", "schema", "Suikoden II");
        foreach ((string path, string label) in new[]
        {
            ("bozu_name", "Hero name"), ("bozu_name2", "Hero real name"), ("macd_name", "Imported Suikoden I hero"),
            ("base_name", "Castle name"), ("m_base_name", "Imported Suikoden I HQ"), ("team_name", "Army name"),
        })
        {
            if (game[path] is JsonValue value)
            {
                AddString(label, $"game_data.{path}", value.GetValue<string>(), text => adapter.SetName(path, text));
            }
        }

        AddNumber("Potch", "party_data.gold", adapter.Potch, value => adapter.SetGeneralNumber("gold", value));
        AddNumber("Popularity", "party_data.ninki", adapter.Popularity, value => adapter.SetGeneralNumber("ninki", value));
        foreach ((string path, string label) in new[]
        {
            ("base_lv", "Castle level"), ("kaji_lv", "Blacksmith level"), ("area_no", "Area"),
            ("town_no", "Town"), ("map_no", "Map"),
        })
        {
            AddNumber(label, $"game_data.{path}", game[path]!.GetValue<int>(), value => adapter.SetGeneralNumber(path, value));
        }

        AddNumber("Player X", "px", adapter.Document.Root["px"]!.GetValue<int>(), value => adapter.SetGeneralNumber("px", value));
        AddNumber("Player Y", "py", adapter.Document.Root["py"]!.GetValue<int>(), value => adapter.SetGeneralNumber("py", value));
        AddGameArrayFields(adapter, "play_time", "Play time", null, "Hours", "Minutes", "Seconds");
        AddGameArrayFields(adapter, "furo_info", "Bath information", null, "Bath level", "Bath value 2");
        foreach (string metadata in new[] { "save_slot", "save_num", "save_poi", "load_count", "date_time_now" })
        {
            JsonNode? node = metadata == "date_time_now" ? adapter.Document.Root[metadata] : game[metadata];
            if (node is not null)
            {
                AddReadOnly($"Visible metadata · {metadata}", metadata, node.ToJsonString());
            }
        }
    }

    private void BuildSuikoden2Party(Suikoden2Adapter adapter)
    {
        int[] values = adapter.PartyCharacterIds.ToArray();
        for (int index = 0; index < values.Length; index++)
        {
            int captured = index;
            string name = Suikoden2Catalog.Character(values[index])?.Name ?? "Empty / NPC";
            string type = index < Suikoden2Adapter.BattlePartySize ? "Battle" : "Convoy";
            AddNumber($"{type} slot {index + 1} · {name}", $"party_data.party_cha_no[{index}]", values[index], value =>
            {
                int[] changed = adapter.PartyCharacterIds.ToArray();
                changed[captured] = value;
                adapter.SetParty(changed);
            });
        }
    }

    private void BuildSuikoden2Character(Suikoden2Adapter adapter)
    {
        if (SelectedCharacter is null)
        {
            AddReadOnly("Character search", "characters", "No matching character");
            return;
        }

        int id = SelectedCharacter.Id;
        Suikoden2CharacterView character = adapter.Characters[id];
        AddReadOnly("Recruitment status", $"chara_flag[{id}]", character.RecruitmentStatus.ToString(CultureInfo.InvariantCulture));
        AddReadOnly("Current party", "party_data.party_cha_no", character.IsInParty ? "Yes" : "No");
        foreach ((string field, string label, int value) in new[]
        {
            ("level", "Level", character.Level), ("exp", "EXP", character.Experience),
            ("now_hp", "Current HP", character.CurrentHp), ("max_hp", "Maximum HP", character.MaximumHp),
            ("buki_lv", "Weapon level", character.WeaponLevel), ("buki_mon", "Weapon rune ID", character.WeaponRune),
            ("todome", "Killed enemies", character.KilledEnemies),
        })
        {
            AddNumber(label, $"chara_data.c_varia_dat[{id}].{field}", value, changed => adapter.SetCharacterScalar(id, field, changed));
        }

        for (int index = 0; index < character.MagicPoints.Count; index++)
        {
            int captured = index;
            AddNumber($"Packed MP · level {index + 1}", $"chara_data.c_varia_dat[{id}].mp[{index}]", character.MagicPoints[index], value => adapter.SetMagicPoint(id, captured, value), "Verified packed range: 0–153; 17 points represent one visible MP square.");
        }

        for (int index = 0; index < character.Stats.Count; index++)
        {
            int captured = index;
            AddNumber(Suikoden2Adapter.StatNames[index], $"chara_data.c_varia_dat[{id}].para[{index}]", character.Stats[index], value => adapter.SetStat(id, captured, value));
        }

        for (int index = 0; index < character.Runes.Count; index++)
        {
            int captured = index;
            AddNumber($"Rune slot {index + 1}", $"chara_data.c_varia_dat[{id}].mon_eqp[{index}]", character.Runes[index], value => adapter.SetRune(id, captured, value), "Slot, character-exclusive, and locked-rune restrictions are enforced.");
        }

        for (int index = 0; index < character.Equipment.Count; index++)
        {
            int captured = index;
            AddNumber($"Equipment slot {index + 1}", $"chara_data.c_varia_dat[{id}].bogu_eqp[{index}]", character.Equipment[index], value => adapter.SetEquipment(id, captured, value), "Helmet/armor/shield compatibility and beast restrictions are enforced.");
        }

        JsonArray accessories = character.Accessories;
        for (int index = 0; index < accessories.Count; index++)
        {
            int captured = index;
            JsonObject current = accessories[index]!.AsObject();
            string text = FormatS2Item(current["item_no"]!.GetValue<int>(), current["use_cnt"]!.GetValue<int>());
            AddString($"Accessory {index + 1}", $"chara_data.c_varia_dat[{id}].item_eqp[{index}]", text, value => adapter.SetAccessory(id, captured, ParseS2Item(value)), "Enter Category:ID, for example Accessory:45 or Regular:1.");
        }
    }

    private void BuildSuikoden2Inventory(Suikoden2Adapter adapter)
    {
        foreach ((Suikoden2Inventory inventory, string path, string warning) in new[]
        {
            (Suikoden2Inventory.Party, "party_data.party_item", string.Empty),
            (Suikoden2Inventory.Warehouse, "game_data.base_item", string.Empty),
            (Suikoden2Inventory.Bath, "game_data.furo_item", "Bath items include trade-category paintings and ornaments."),
            (Suikoden2Inventory.RoomExperimental, "game_data.room_item", "Experimental: room-item purpose remains uncertain. Edit at your own risk."),
        })
        {
            JsonArray array = inventory switch
            {
                Suikoden2Inventory.Party => adapter.Document.Root["party_data"]!["party_item"]!.AsArray(),
                Suikoden2Inventory.Warehouse => adapter.Document.Root["game_data"]!["base_item"]!.AsArray(),
                Suikoden2Inventory.Bath => adapter.Document.Root["game_data"]!["furo_item"]!.AsArray(),
                _ => adapter.Document.Root["game_data"]!["room_item"]!.AsArray(),
            };
            for (int index = 0; index < array.Count; index++)
            {
                int captured = index;
                JsonObject item = array[index]!.AsObject();
                string text = FormatS2Item(item["item_no"]!.GetValue<int>(), item["use_cnt"]!.GetValue<int>());
                AddString($"{inventory} slot {index + 1}", $"{path}[{index}]", text, value => adapter.SetInventorySlot(inventory, captured, ParseS2Item(value)), warning.Length == 0 ? "Enter Category:ID; use_cnt is synchronized from the reviewed catalogue." : warning + " Enter Category:ID.");
            }
        }

        JsonArray keyItems = adapter.Document.Root["party_data"]!["event_item"]!.AsArray();
        for (int index = 0; index < keyItems.Count; index++)
        {
            int captured = index;
            AddNumber($"Key item slot {index + 1}", $"party_data.event_item[{index}]", keyItems[index]!.GetValue<int>(), value => adapter.SetKeyItem(captured, value), "Story-critical: only reviewed key-item IDs are accepted. Use 0 to clear.");
        }

        if (SearchText.Trim().Length > 0)
        {
            foreach (Suikoden2ItemDefinition item in Suikoden2Catalog.SearchItems(SearchText).Take(200))
            {
                AddReadOnly($"Catalogue · {item.Category}:{item.Id} · {item.Name}", "item reference", $"use_cnt {item.UseCount}", item.StoryCritical ? "Story-critical/key item; excluded from safe bulk operations." : null);
            }
        }
    }

    private void BuildSuikoden2Recruitment(Suikoden2Adapter adapter)
    {
        JsonArray flags = adapter.Document.Root["chara_flag"]!.AsArray();
        for (int id = 1; id < flags.Count; id++)
        {
            int captured = id;
            string name = Suikoden2Catalog.Character(id)?.Name ?? $"Character {id}";
            AddNumber($"{id}: {name}", $"chara_flag[{id}]", flags[id]!.GetValue<int>(), value => adapter.SetRecruitmentStatus(captured, value), "Reviewed statuses: 0, 1, 70, 71, 86, 212, 213. Story and required-party consequences are not synthesized.");
        }

        foreach (string note in adapter.CompatibilityNotes(BetterLeonaEnabled, KrakenRecruitmentEnabled))
        {
            AddReadOnly("Optional-mod compatibility note", "compatibility", note);
        }
    }

    private void BuildSuikoden2Progress(Suikoden2Adapter adapter)
    {
        JsonObject game = adapter.Document.Root["game_data"]!.AsObject();
        AddReadOnly("Imported Suikoden I recruit count", "game_data.nakam_1_num", game["nakam_1_num"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture), "Read-only: McDohl/Gremio import semantics are not safe to synthesize.");
        JsonArray aliases = game["kari_name"]!.AsArray();
        for (int index = 0; index < aliases.Count; index++)
        {
            int captured = index;
            AddString($"Greenhill alias {index + 1}", $"game_data.kari_name[{index}]", aliases[index]!.GetValue<string>(), value => adapter.SetGreenhillAlias(captured, value));
        }

        AddGameArrayFields(adapter, "food_menu", "Castle food menu", null);
        AddGameArrayFields(adapter, "food_resipi", "Recipe flags", null);
        AddGameArrayFields(adapter, "food_num", "Food / recipe values", null);

        JsonArray events = adapter.Document.Root["event_flag"]!.AsArray();
        CookOffStage? current = events.Count > 153
            ? Suikoden2Adapter.CookOffStages.FirstOrDefault(stage => stage.EventByte152 == events[152]!.GetValue<int>() && stage.EventByte153 == events[153]!.GetValue<int>())
            : null;
        AddNumber("Cook-off battles won", "event_flag[152..153]", current?.BattlesWon ?? 0, adapter.SetCookOffStage, "Dangerous story progress edit. Only the 13 reviewed stages are accepted.");
        AddGameArrayFields(adapter, "tantei_lv", "Richmond detective clue byte", "Experimental progress flags: each value is a byte (0–255).");
        AddGameArrayFields(adapter, "hon_flag", "Castle / farm flag byte", "Experimental castle and farm flags; indices 30–32 are upstream-researched but can affect progression.");

        JsonArray treasure = adapter.Document.Root["t_box_flag"]!.AsArray();
        for (int index = 0; index < treasure.Count; index++)
        {
            int captured = index;
            AddNumber($"Treasure-chest flag byte {index}", $"t_box_flag[{index}]", treasure[index]!.GetValue<int>(), value => adapter.SetTreasureFlagByte(captured, value), "Experimental: each byte controls eight chest flags.");
        }
    }

    private void AddGameArrayFields(Suikoden2Adapter adapter, string field, string label, string? warning, params string[] labels)
    {
        JsonArray array = adapter.Document.Root["game_data"]![field]!.AsArray();
        for (int index = 0; index < array.Count; index++)
        {
            int captured = index;
            string itemLabel = index < labels.Length ? labels[index] : $"{label} {index}";
            AddNumber(itemLabel, $"game_data.{field}[{index}]", array[index]!.GetValue<int>(), value => adapter.SetGameDataArrayValue(field, captured, value), warning);
        }
    }

    private static void SetS1ItemPart(Suikoden1Adapter adapter, int characterId, int slot, int? itemId, int? equipment, int? uses)
    {
        JsonObject item = adapter.Document.Root["player_base"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(value => value["chara_no"]!.GetValue<int>() == characterId)["item"]!.AsArray()[slot]!.AsObject();
        adapter.SetCharacterItem(
            characterId,
            slot,
            itemId ?? item["item_id"]!.GetValue<int>(),
            equipment ?? item["soubi"]!.GetValue<int>(),
            uses ?? item["data"]!.GetValue<int>());
    }

    private static Suikoden2ItemDefinition ParseS2Item(string value)
    {
        string[] parts = value.Split(':', 2, StringSplitOptions.TrimEntries);
        Guard.Valid(parts.Length == 2, "Enter an item as Category:ID, for example Regular:1 or Accessory:45.");
        Guard.Valid(Enum.TryParse(parts[0], true, out Suikoden2ItemCategory category), "The item category is not recognized.");
        int id = ParseInteger(parts[1]);
        return Suikoden2Catalog.FindItem(category, id);
    }

    private static string FormatS2Item(int id, int useCount)
    {
        Suikoden2ItemDefinition? item = Suikoden2Catalog.Items.FirstOrDefault(value => value.Id == id && (id == 0 || value.UseCount == useCount));
        return item is null ? $"Regular:{id}" : $"{item.Category}:{item.Id}";
    }

    private void AddNumber(string label, string path, int value, Action<int> apply, string? warning = null)
    {
        AddString(label, path, value.ToString(CultureInfo.InvariantCulture), text => apply(ParseInteger(text)), warning);
    }

    private void AddString(string label, string path, string value, Action<string> apply, string? warning = null)
    {
        allFields.Add(new(label, path, value, false, warning, text => ApplyEdit($"Changed {label}", () => apply(text))));
    }

    private void AddReadOnly(string label, string path, string value, string? warning = null)
    {
        allFields.Add(new(label, path, value, true, warning, null));
    }

    private static int ParseInteger(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            throw new SaveEditorException(SaveErrorCode.ValidationFailed, $"'{value}' is not a valid whole number.");
        }

        return result;
    }

    private static bool ParseRecruitmentBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "recruited" or "true" or "9" => true,
        "unrecruited" or "false" or "0" => false,
        _ => throw new SaveEditorException(SaveErrorCode.ValidationFailed, "Enter recruited/unrecruited, true/false, or 9/0."),
    };

    private IReadOnlyList<ValidationIssue> ValidateCurrent() => document?.Game switch
    {
        GameKind.Suikoden1 => new Suikoden1Adapter(document).Validate(),
        GameKind.Suikoden2 => new Suikoden2Adapter(document).Validate(),
        _ => [],
    };

    private async Task<bool> ConfirmValidSaveAsync()
    {
        IReadOnlyList<ValidationIssue> issues = ValidateCurrent();
        ValidationIssue[] errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            await interaction.ShowMessageAsync("Save blocked by validation", string.Join(Environment.NewLine, errors.Select(issue => $"{issue.Path}: {issue.Message}"))).ConfigureAwait(true);
            return false;
        }

        ValidationIssue[] warnings = issues.Where(issue => issue.Severity == ValidationSeverity.Warning).ToArray();
        if (warnings.Length == 0)
        {
            return true;
        }

        string message = string.Join(Environment.NewLine, warnings.Take(8).Select(issue => $"• {issue.Message}"));
        return await interaction.ConfirmAsync(
            "Review save warnings",
            message + "\n\nSteam Cloud may restore an older save. Continue with validated local output?",
            "Continue").ConfigureAwait(true);
    }

    private async Task<bool> CanDiscardCurrentAsync()
    {
        return !HasUnsavedChanges || await interaction.ConfirmAsync(
            "Discard unsaved changes?",
            "Opening another save will discard the current unsaved edits.",
            "Discard and open").ConfigureAwait(true);
    }

    private void UpdateRecentFiles(string path)
    {
        IReadOnlyList<string> paths = recentFileStore.Add(path);
        RecentFiles.Clear();
        foreach (string recent in paths)
        {
            RecentFiles.Add(recent);
        }

        SelectedRecentFile = RecentFiles.FirstOrDefault();
    }

    private void RaiseCommandStates()
    {
        ((AsyncRelayCommand)OpenSelectedSlotCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)OpenRecentCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SaveAsCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)OverwriteCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ReloadCommand).RaiseCanExecuteChanged();
        ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)GiveAllSafeItemsCommand).RaiseCanExecuteChanged();
    }
}
