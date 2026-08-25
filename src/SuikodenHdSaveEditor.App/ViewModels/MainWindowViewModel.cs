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
    private const string CreditsSummary = """
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

        Cyril — Suikoden Guide and Walkthrough
        https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/80674/part-10-to-live-and-die-freely
        Credited factual corroboration that Suikoden I headquarters level 4 is its final development. No guide prose is distributed.

        Shiro — Suikoden Character Power-Up FAQ
        https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/10601
        Factual Suikoden I level, weapon, equipment-class, and end-game recommendation research. No guide prose is distributed.

        DHolmes — Suikoden II Game Save Hacking Guide
        https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/7234
        Feral — Suikoden II Armor/Equipment List
        https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/6620
        Factual stat storage, weapon cap, armor-class, locked-item, and defensive-ranking research. These copyrighted guides have no software-license grant; no prose or tables are distributed.

        Gensopedia — Suikoden II equipment reference
        https://gensopedia.org/w/Equipment_%28Suikoden_II%29
        CC BY-NC-SA unless otherwise noted. Consulted only for factual equipment cross-checks; no wiki prose or tables are distributed.

        WiduraGoez — Suikoden I & II HD Remaster 1.0.3 runtime-code research
        https://www.nsboy.net/thread-31928-1-1.html
        Factual corroboration of remaster status and HP limits. No cheat code or site prose is distributed, and no reuse license is claimed.

        LICENSES

        Original project code: Zero-Clause BSD (0BSD).
        Substantially ported upstream portions: MIT, with retained copyright notices.
        Avalonia and .NET: MIT. Inter font: SIL Open Font License 1.1.
        Full license and notice texts are embedded below and remain available offline.

        FAN-PROJECT DISCLAIMER

        This independent fan project is not affiliated with, authorized by, sponsored by, or endorsed by Konami or any upstream author. Suikoden and related names are trademarks of their respective owners. No Konami logos, extracted artwork, music, fonts, or other proprietary game assets are included.

        """;

    public static readonly string CreditsAndLicenses =
        $"{CreditsSummary}{Environment.NewLine}{EmbeddedLegalNotices.Load()}";

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

    private static readonly string[] CharacterFilterNames = ["All", "Recruited", "Unrecruited", "Current party"];

    private readonly IUserInteraction interaction;
    private readonly RecentFileStore recentFileStore;
    private readonly SaveFileService saveFileService = new();
    private readonly EditHistory history = new();
    private readonly List<ChoiceViewModel> allCharacters = [];
    private readonly List<EditorFieldViewModel> allFields = [];
    private SaveDocument? document;
    private string selectedSection = SectionNames[0];
    private string selectedCharacterFilter = CharacterFilterNames[0];
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
        CharacterFilters = new ObservableCollection<string>(CharacterFilterNames);
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
        ApplyAllCommand = new RelayCommand(ApplyAll, () => document is not null && IsFieldEditor && Fields.Any(field => !field.IsReadOnly));
        MaximizeAndEquipPartyCommand = new AsyncRelayCommand(MaximizeAndEquipPartyAsync, () => document is not null && IsCharacters);
        GiveAllSafeItemsCommand = new AsyncRelayCommand(GiveAllSafeItemsAsync, () => document?.Game == GameKind.Suikoden2);
        AboutCommand = new AsyncRelayCommand(() => interaction.ShowAboutAsync(CreditsAndLicenses));
    }

    public ObservableCollection<string> Sections { get; }

    public ObservableCollection<EditorFieldViewModel> Fields { get; }

    public ObservableCollection<string> CharacterFilters { get; }

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

    public ICommand ApplyAllCommand { get; }

    public ICommand MaximizeAndEquipPartyCommand { get; }

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
                OnPropertyChanged(nameof(IsCharacters));
                OnPropertyChanged(nameof(IsInventory));
                OnPropertyChanged(nameof(IsRecruitment));
                OnPropertyChanged(nameof(IsSearchVisible));
                OnPropertyChanged(nameof(IsApplyAllVisible));
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

    public string SelectedCharacterFilter
    {
        get => selectedCharacterFilter;
        set
        {
            if (SetProperty(ref selectedCharacterFilter, value))
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

    public bool IsCharacters => SelectedSection == "Characters";

    public bool IsInventory => SelectedSection == "Inventory";

    public bool IsRecruitment => SelectedSection == "Recruitment";

    public bool IsSearchVisible => IsCharacters || IsInventory || IsRecruitment;

    public bool IsApplyAllVisible => HasDocument && IsFieldEditor;

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
            $"This will create a timestamped backup, then atomically replace:\n{document.OriginalPath}\n\nClose the game before continuing.",
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

    private async Task MaximizeAndEquipPartyAsync()
    {
        if (document is null)
        {
            return;
        }

        bool accepted = await interaction.ConfirmAsync(
            "Max and optimize the active battle party?",
            "This sets every active battle character to level 99, 9,999 current/maximum HP, maximum MP, maximum base stats, and weapon level 16 where a weapon exists. It also replaces removable gear with researched, class-compatible end-game equipment and physical or magic accessories. Fixed weapon identities, runes, known locked gear, and unrelated data are preserved.\n\nSome equipment choices are informed recommendations rather than official character builds. Use Undo if the result is not what you want, and verify a copied save in game before overwriting anything important.",
            "Max and equip party").ConfigureAwait(true);
        if (!accepted)
        {
            return;
        }

        PartyOptimizationResult? result = null;
        ApplyEdit("Maximized stats and equipped recommended party gear", () =>
        {
            result = document.Game == GameKind.Suikoden1
                ? new Suikoden1Adapter(document).MaximizeAndEquipParty()
                : new Suikoden2Adapter(document).MaximizeAndEquipParty();
        });
        if (!HasError && result is not null)
        {
            StatusMessage = $"Maximized {result.CharactersUpdated} active battle character(s); updated {result.EquipmentSlotsUpdated} gear slot(s) and preserved {result.LockedOrUnavailableSlotsPreserved} locked or unavailable slot(s).";
        }
    }

    private void ApplyAll()
    {
        if (document is null)
        {
            return;
        }

        EditorFieldViewModel[] editable = Fields.Where(field => !field.IsReadOnly).ToArray();
        if (editable.Length == 0)
        {
            StatusMessage = "There are no editable fields in this section.";
            return;
        }

        EditorFieldViewModel[] pending = editable.Where(field => field.HasPendingValue).ToArray();
        if (pending.Length == 0)
        {
            StatusMessage = "There are no pending field changes in this section.";
            return;
        }

        ApplyEdit($"Applied all {pending.Length} changed fields in {SelectedSection}", () =>
        {
            if (SelectedSection == "Party")
            {
                int[] party = editable.Select(field => ParseLabeledInteger(field.Value, "character")).ToArray();
                if (document.Game == GameKind.Suikoden1)
                {
                    new Suikoden1Adapter(document).SetParty(party);
                }
                else
                {
                    new Suikoden2Adapter(document).SetParty(party);
                }

                return;
            }

            foreach (EditorFieldViewModel field in OrderDependentFields(pending))
            {
                field.ApplyValue();
            }
        });
    }

    private static List<EditorFieldViewModel> OrderDependentFields(IReadOnlyList<EditorFieldViewModel> pending)
    {
        List<EditorFieldViewModel> ordered = [.. pending];
        EditorFieldViewModel? maximumHp = ordered.FirstOrDefault(field => field.Path.EndsWith(".max_hp", StringComparison.Ordinal));
        EditorFieldViewModel? currentHp = ordered.FirstOrDefault(field =>
            field.Path.EndsWith(".hp", StringComparison.Ordinal) || field.Path.EndsWith(".now_hp", StringComparison.Ordinal));
        if (maximumHp is null || currentHp is null)
        {
            return ordered;
        }

        int newMaximum = ParseInteger(maximumHp.Value);
        int newCurrent = ParseInteger(currentHp.Value);
        Guard.Valid(newMaximum >= newCurrent, "Maximum HP cannot be below current HP.");
        int oldMaximum = ParseInteger(maximumHp.OriginalValue);
        int insertAt = Math.Min(ordered.IndexOf(maximumHp), ordered.IndexOf(currentHp));
        ordered.Remove(maximumHp);
        ordered.Remove(currentHp);
        if (newCurrent > oldMaximum)
        {
            ordered.Insert(insertAt, maximumHp);
            ordered.Insert(insertAt + 1, currentHp);
        }
        else
        {
            ordered.Insert(insertAt, currentHp);
            ordered.Insert(insertAt + 1, maximumHp);
        }

        return ordered;
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
        OnPropertyChanged(nameof(IsApplyAllVisible));
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
        if (SelectedSection == "Characters")
        {
            choices = choices.Where(MatchesCharacterFilter);
            if (SearchText.Trim().Length > 0)
            {
                string query = SearchText.Trim();
                choices = choices.Where(choice => choice.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || choice.Id.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase));
            }
        }

        ChoiceViewModel[] filtered = choices.ToArray();
        CharacterChoices.Clear();
        foreach (ChoiceViewModel choice in filtered)
        {
            CharacterChoices.Add(choice);
        }

        SelectedCharacter = CharacterChoices.FirstOrDefault(choice => choice.Id == current) ?? CharacterChoices.FirstOrDefault();
    }

    private bool MatchesCharacterFilter(ChoiceViewModel choice)
    {
        if (document is null || SelectedCharacterFilter == "All")
        {
            return true;
        }

        bool inParty;
        bool recruited;
        if (document.Game == GameKind.Suikoden1)
        {
            Suikoden1Adapter adapter = new(document);
            inParty = adapter.PartyCharacterIds.Contains(choice.Id);
            recruited = adapter.RecruitedCharacterIds.Contains(choice.Id);
        }
        else
        {
            Suikoden2CharacterView character = new Suikoden2Adapter(document).Characters[choice.Id];
            inParty = character.IsInParty;
            recruited = character.RecruitmentStatus is 70 or 71;
        }

        return SelectedCharacterFilter switch
        {
            "Recruited" => recruited,
            "Unrecruited" => !recruited,
            "Current party" => inParty,
            _ => true,
        };
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

        ((RelayCommand)ApplyAllCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)MaximizeAndEquipPartyCommand).RaiseCanExecuteChanged();
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
                AddHeadquartersLevelChoice("Headquarters level", "shiro_data.level", adapter.HeadquartersLevel, adapter.SetHeadquartersLevel);
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
                AddHeadquartersLevelChoice("Headquarters level", "shiro_data.level", adapter.HeadquartersLevel, adapter.SetHeadquartersLevel);
                AddReadOnly("Unexposed headquarters fields", "shiro_data", "Preserved in Advanced Data", "Other shiro_data fields are intentionally read-only because their meanings are not sufficiently verified.");
                AddReadOnly("Story flags", "tmpEventFlagS / storyFlagS", "Preserved in Advanced Data", "Meanings and safe transitions are not sufficiently documented for normal editing.");
                break;
        }
    }

    private void BuildSuikoden1Party(Suikoden1Adapter adapter)
    {
        string[] characterChoices =
        [
            FormatCharacterChoice(-1, "Empty"),
            .. adapter.Characters.OrderBy(character => character.Id).Select(character => FormatCharacterChoice(character.Id, character.Name)),
        ];
        int[] values = adapter.PartyCharacterIds.ToArray();
        for (int index = 0; index < values.Length; index++)
        {
            int captured = index;
            string name = values[index] == -1 ? "Empty" : Suikoden1Catalog.CharacterName(values[index]);
            AddChoice(
                $"Party slot {index + 1} · {name}",
                $"party_data.chara_code[{index}]",
                FormatCharacterChoice(values[index], name),
                characterChoices,
                value =>
                {
                    int[] changed = adapter.PartyCharacterIds.ToArray();
                    changed[captured] = ParseLabeledInteger(value, "character");
                    adapter.SetParty(changed);
                },
                "Tir must remain somewhere in the six slots. Only characters with a battle record in this save are offered.");
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
        AddChoice(
            "Equipped rune",
            $"player_base[{id}].monsyo_data.monsyo_id",
            FormatNamedId(Suikoden1Catalog.Runes.GetValueOrDefault(character.RuneId, $"Rune {character.RuneId}"), "rune", character.RuneId),
            Suikoden1Catalog.Runes.OrderBy(item => item.Key).Select(item => FormatNamedId(item.Value, "rune", item.Key)),
            value => adapter.SetCharacterRune(id, ParseLabeledInteger(value, "rune")));
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
            AddChoice(
                $"Carried item {slot + 1}",
                $"player_base[{id}].item[{slot}].item_id",
                FormatNamedId(Suikoden1Catalog.ItemName(itemId), "item", itemId),
                Suikoden1Catalog.Items.OrderBy(item => item.Key).Select(item => FormatNamedId(item.Value, "item", item.Key)),
                value => SetS1ItemPart(adapter, id, captured, ParseLabeledInteger(value, "item"), null, null));
            AddChoice(
                $"Carried item {slot + 1} equipment state",
                $"player_base[{id}].item[{slot}].soubi",
                FormatNamedId(Suikoden1Catalog.EquipmentSlots.GetValueOrDefault(equipment, $"State {equipment}"), "state", equipment),
                Suikoden1Catalog.EquipmentSlots.OrderBy(item => item.Key).Select(item => FormatNamedId(item.Value, "state", item.Key)),
                value => SetS1ItemPart(adapter, id, captured, null, ParseLabeledInteger(value, "state"), null),
                equipment >= 129 ? "States 129–133 are verified non-removable equipment states." : null);
            AddNumber($"Carried item {slot + 1} remaining uses", $"player_base[{id}].item[{slot}].data", uses, value => SetS1ItemPart(adapter, id, captured, null, null, value));
        }
    }

    private void BuildSuikoden1Inventory(Suikoden1Adapter adapter)
    {
        Dictionary<string, int> itemChoices = new(StringComparer.Ordinal);
        foreach ((int id, string name) in Suikoden1Catalog.Items.OrderBy(item => item.Key))
        {
            itemChoices.TryAdd(name, id);
        }

        JsonArray items = adapter.Document.Root["party_data"]!["party_item"]!.AsArray();
        for (int index = 0; index < items.Count; index++)
        {
            int captured = index;
            int id = items[index]!.GetValue<int>();
            AddChoice(
                $"Party item {index + 1}",
                $"party_data.party_item[{index}]",
                Suikoden1Catalog.ItemName(id),
                itemChoices.Keys.Order(StringComparer.OrdinalIgnoreCase),
                value => adapter.SetPartyItem(captured, ParseNamedChoice(value, itemChoices, "item")));
        }

        if (SearchText.Trim().Length > 0)
        {
            string query = SearchText.Trim();
            foreach ((int _, string name) in Suikoden1Catalog.Items.Where(item => item.Value.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(200))
            {
                AddReadOnly($"Catalogue · {name}", "item reference", name);
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
            AddChoice(
                $"{id}: {Suikoden1Catalog.CharacterName(id)}",
                $"member_flag[{id}]",
                FormatSuikoden1Recruitment(current),
                new[] { 0, 9, current }.Distinct().Select(FormatSuikoden1Recruitment),
                value =>
                {
                    int selected = ParseLabeledInteger(value, "member flag");
                    if (selected == current && selected is not (0 or 9))
                    {
                        return;
                    }

                    adapter.SetRecruited(captured, ParseRecruitmentBoolean(value));
                },
                "Flag 0 means not recruited; flag 9 means recruited. Recruitment edits can break story progression or required-party events.");
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
        if (game["bozu_name"] is JsonValue heroName)
        {
            AddString(
                "Hero / save-list name",
                "game_data.bozu_name + game_data.bozu_name2",
                heroName.GetValue<string>(),
                text => adapter.SetName("bozu_name", text),
                "Both paired hero-name fields are updated together. Every supplied Suikoden II save keeps these fields equal; changing only one can leave menus or the save list showing the old name.");
        }

        foreach ((string path, string label) in new[]
        {
            ("macd_name", "Imported Suikoden I hero"),
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
        AddHeadquartersLevelChoice("Castle level", "game_data.base_lv", game["base_lv"]!.GetValue<int>(), value => adapter.SetGeneralNumber("base_lv", value));
        foreach ((string path, string label) in new[]
        {
            ("kaji_lv", "Blacksmith level"), ("area_no", "Area"),
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
            int maximum = index < Suikoden2Adapter.BattlePartySize ? 83 : 124;
            string[] choices = Enumerable.Range(0, maximum + 1)
                .Where(id => id == 0 || Suikoden2Catalog.Character(id) is not null)
                .Select(id => FormatCharacterChoice(id, id == 0 ? "Empty" : Suikoden2Catalog.Character(id)!.Name))
                .ToArray();
            AddChoice($"{type} slot {index + 1}", $"party_data.party_cha_no[{index}]", FormatCharacterChoice(values[index], name), choices, value =>
            {
                int[] changed = adapter.PartyCharacterIds.ToArray();
                changed[captured] = ParseLabeledInteger(value, "character");
                adapter.SetParty(changed);
            }, index < Suikoden2Adapter.BattlePartySize
                ? "Battle slots accept the reviewed battle-character range."
                : "Convoy slots also offer named support characters from the reviewed catalogue.");
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
            if (field == "buki_mon")
            {
                IEnumerable<Suikoden2ItemDefinition> weaponRunes = Suikoden2Catalog.Items
                    .Where(item => item.Category == Suikoden2ItemCategory.Rune && (item.Id == 0 || item.Attributes.Contains("Wep")))
                    .Where(item => !Suikoden2Catalog.Beasts.Contains(id) || item.Id == 0);
                AddChoice(
                    "Weapon rune",
                    $"chara_data.c_varia_dat[{id}].{field}",
                    FormatS2CatalogChoice(Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Rune && item.Id == value), Suikoden2ItemCategory.Rune, value),
                    weaponRunes.Select(FormatS2CatalogChoice),
                    changed => adapter.SetCharacterScalar(id, field, ParseS2Item(changed).Id),
                    "Only reviewed weapon runes are offered; beasts and monsters can select only None.");
            }
            else
            {
                AddNumber(label, $"chara_data.c_varia_dat[{id}].{field}", value, changed => adapter.SetCharacterScalar(id, field, changed));
            }
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
            int current = character.Runes[index];
            AddChoice(
                $"Rune slot {index + 1}",
                $"chara_data.c_varia_dat[{id}].mon_eqp[{index}]",
                FormatS2CatalogChoice(Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Rune && item.Id == current), Suikoden2ItemCategory.Rune, current),
                Suikoden2Catalog.Items
                    .Where(item => item.Category == Suikoden2ItemCategory.Rune && Suikoden2Catalog.IsRuneAllowed(id, captured, item.Id))
                    .Select(FormatS2CatalogChoice),
                value => adapter.SetRune(id, captured, ParseS2Item(value).Id),
                "Slot, character-exclusive, and locked-rune restrictions are enforced. A currently locked rune is shown but cannot be changed.");
        }

        for (int index = 0; index < character.Equipment.Count; index++)
        {
            int captured = index;
            Suikoden2ItemCategory category = index switch
            {
                0 => Suikoden2ItemCategory.Helmet,
                1 => Suikoden2ItemCategory.Armor,
                _ => Suikoden2ItemCategory.Shield,
            };
            int current = character.Equipment[index];
            AddChoice(
                $"{category} slot",
                $"chara_data.c_varia_dat[{id}].bogu_eqp[{index}]",
                FormatS2CatalogChoice(Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == category && item.Id == current), category, current),
                Suikoden2Catalog.Items
                    .Where(item => item.Category == category && Suikoden2Catalog.IsEquipmentAllowed(id, category, item.Id))
                    .Select(FormatS2CatalogChoice),
                value => adapter.SetEquipment(id, captured, ParseS2Item(value).Id),
                "Equipment-type compatibility and beast/monster restrictions are enforced.");
        }

        JsonArray accessories = character.Accessories;
        for (int index = 0; index < accessories.Count; index++)
        {
            int captured = index;
            JsonObject current = accessories[index]!.AsObject();
            string text = FormatS2Item(current["item_no"]!.GetValue<int>(), current["use_cnt"]!.GetValue<int>());
            IEnumerable<Suikoden2ItemDefinition> accessoryChoices = Suikoden2Catalog.Items
                .Where(item => item.Category is Suikoden2ItemCategory.Regular or Suikoden2ItemCategory.Accessory or Suikoden2ItemCategory.Food)
                .Where(item => !item.StoryCritical)
                .Where(item => !Suikoden2Catalog.Beasts.Contains(id) || item.Id == 0);
            AddChoice($"Accessory {index + 1}", $"chara_data.c_varia_dat[{id}].item_eqp[{index}]", text, accessoryChoices.Select(FormatS2CatalogChoice), value => adapter.SetAccessory(id, captured, ParseS2Item(value)), "Only reviewed item, accessory, and food entries are offered; beast/monster restrictions are enforced.");
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
                int itemId = item["item_no"]!.GetValue<int>();
                int useCount = item["use_cnt"]!.GetValue<int>();
                string text = FormatS2InventoryItem(inventory, itemId, useCount);
                IEnumerable<Suikoden2ItemDefinition> choices = InventoryChoices(inventory, captured);
                Dictionary<string, Suikoden2ItemDefinition> namedChoices = BuildS2ItemNameChoices(choices);
                string slotKind = inventory == Suikoden2Inventory.Bath ? captured is 2 or 5 ? "painting" : "ornament" : "slot";
                AddChoice(
                    $"{InventoryDisplayName(inventory)} {slotKind} {index + 1}",
                    $"{path}[{index}]",
                    text,
                    namedChoices.Keys.Order(StringComparer.OrdinalIgnoreCase),
                    value => adapter.SetInventorySlot(inventory, captured, ParseNamedChoice(value, namedChoices, "item")),
                    warning.Length == 0
                        ? "Choose by item name. Stackable consumables start at their reviewed maximum quantity; use the quantity field to lower it."
                        : warning + " Choose by item name.");

                Suikoden2ItemDefinition? currentItem = Suikoden2Catalog.StoredItem(itemId, useCount);
                if (currentItem is { Category: Suikoden2ItemCategory.Regular, UseCount: > 1 })
                {
                    string quantityWarning = $"Verified quantity range for {currentItem.Name}: 1–{currentItem.UseCount}. Select None in the item field to remove the stack.";
                    if (warning.Length > 0)
                    {
                        quantityWarning = warning + " " + quantityWarning;
                    }

                    AddChoice(
                        $"{InventoryDisplayName(inventory)} {slotKind} {index + 1} quantity",
                        $"{path}[{index}].use_cnt",
                        useCount.ToString(CultureInfo.InvariantCulture),
                        Enumerable.Range(1, currentItem.UseCount).Select(value => value.ToString(CultureInfo.InvariantCulture)),
                        value => adapter.SetInventoryQuantity(inventory, captured, ParseInteger(value)),
                        quantityWarning);
                }
            }
        }

        JsonArray keyItems = adapter.Document.Root["party_data"]!["event_item"]!.AsArray();
        for (int index = 0; index < keyItems.Count; index++)
        {
            int captured = index;
            int current = keyItems[index]!.GetValue<int>();
            Suikoden2ItemDefinition? currentItem = Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Regular && item.Id == current);
            IEnumerable<Suikoden2ItemDefinition> choices = Suikoden2Catalog.Items
                .Where(item => item.Category == Suikoden2ItemCategory.Regular && (item.Id == 0 || item.StoryCritical));
            Dictionary<string, Suikoden2ItemDefinition> namedChoices = BuildS2ItemNameChoices(choices);
            AddChoice(
                $"Key item slot {index + 1}",
                $"party_data.event_item[{index}]",
                S2ItemDisplayName(currentItem),
                namedChoices.Keys.Order(StringComparer.OrdinalIgnoreCase),
                value => adapter.SetKeyItem(captured, ParseNamedChoice(value, namedChoices, "key item").Id),
                "Story-critical: only reviewed key-item entries are offered. Select None to clear.");
        }

        if (SearchText.Trim().Length > 0)
        {
            foreach (Suikoden2ItemDefinition item in Suikoden2Catalog.SearchItems(SearchText).Take(200))
            {
                string detail = FriendlyItemCategory(item.Category);
                if (item is { Category: Suikoden2ItemCategory.Regular, UseCount: > 1 })
                {
                    detail += $" · Maximum quantity {item.UseCount}";
                }

                AddReadOnly($"Catalogue · {item.Name}", "item reference", detail, item.StoryCritical ? "Story-critical/key item; excluded from safe bulk operations." : null);
            }
        }
    }

    private void BuildSuikoden2Recruitment(Suikoden2Adapter adapter)
    {
        JsonArray flags = adapter.Document.Root["chara_flag"]!.AsArray();
        for (int id = 1; id < flags.Count; id++)
        {
            int captured = id;
            Suikoden2CharacterDefinition? definition = Suikoden2Catalog.Character(id);
            if (definition is null)
            {
                continue;
            }

            string name = definition.Name;
            int current = flags[id]!.GetValue<int>();
            AddChoice(
                $"{id}: {name}",
                $"chara_flag[{id}]",
                FormatSuikoden2RecruitmentStatus(current),
                Suikoden2Adapter.RecruitmentStatuses.Order().Select(FormatSuikoden2RecruitmentStatus),
                value =>
                {
                    int selected = ParseTrailingInteger(value);
                    if (selected == current && !Suikoden2Adapter.RecruitmentStatuses.Contains(selected))
                    {
                        return;
                    }

                    adapter.SetRecruitmentStatus(captured, selected);
                },
                "Auto Join and Manual Recruit are both recruited states. Other states reflect story availability; changing them can affect required-party and story events.");
        }

        foreach (string note in adapter.CompatibilityNotes(BetterLeonaEnabled, KrakenRecruitmentEnabled))
        {
            AddReadOnly("Optional-mod compatibility note", "compatibility", note);
        }
    }

    private void BuildSuikoden2Progress(Suikoden2Adapter adapter)
    {
        JsonObject game = adapter.Document.Root["game_data"]!.AsObject();
        AddHeadquartersLevelChoice("Castle level", "game_data.base_lv", game["base_lv"]!.GetValue<int>(), value => adapter.SetGeneralNumber("base_lv", value));
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
        int separator = value.LastIndexOf(" — ", StringComparison.Ordinal);
        string encoded = separator >= 0 ? value[(separator + 3)..] : value;
        string[] parts = encoded.Split(':', 2, StringSplitOptions.TrimEntries);
        Guard.Valid(parts.Length == 2, "Choose an item by name, ID, or category from the reviewed list.");
        Guard.Valid(Enum.TryParse(parts[0], true, out Suikoden2ItemCategory category), "The item category is not recognized.");
        int id = ParseInteger(parts[1]);
        return Suikoden2Catalog.FindItem(category, id);
    }

    private static string FormatS2Item(int id, int useCount)
    {
        Suikoden2ItemDefinition? item = Suikoden2Catalog.Items.FirstOrDefault(value => value.Id == id && (id == 0 || value.UseCount == useCount));
        return FormatS2CatalogChoice(item, item?.Category ?? Suikoden2ItemCategory.Regular, id);
    }

    private static string FormatS2InventoryItem(Suikoden2Inventory inventory, int id, int useCount)
    {
        if (inventory == Suikoden2Inventory.Bath)
        {
            Suikoden2ItemDefinition? bathItem = Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Trade && item.Id == id);
            return S2ItemDisplayName(bathItem);
        }

        return S2ItemDisplayName(Suikoden2Catalog.StoredItem(id, useCount));
    }

    private static Dictionary<string, Suikoden2ItemDefinition> BuildS2ItemNameChoices(IEnumerable<Suikoden2ItemDefinition> choices)
    {
        Dictionary<string, Suikoden2ItemDefinition> result = new(StringComparer.Ordinal);
        foreach (IGrouping<string, Suikoden2ItemDefinition> nameGroup in choices
            .GroupBy(S2ItemDisplayName, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            Suikoden2ItemCategory[] categories = nameGroup.Select(item => item.Category).Distinct().ToArray();
            if (categories.Length == 1)
            {
                result.TryAdd(nameGroup.Key, nameGroup.First());
                continue;
            }

            foreach (IGrouping<Suikoden2ItemCategory, Suikoden2ItemDefinition> categoryGroup in nameGroup.GroupBy(item => item.Category))
            {
                result.TryAdd($"{nameGroup.Key} ({FriendlyItemCategory(categoryGroup.Key)})", categoryGroup.First());
            }
        }

        return result;
    }

    private static string S2ItemDisplayName(Suikoden2ItemDefinition? item) => item switch
    {
        null => "Unknown item (preserved)",
        { Id: 0 } => "None",
        { StoryCritical: true } => $"{item.Name} [Story-critical]",
        _ => item.Name,
    };

    private static string FriendlyItemCategory(Suikoden2ItemCategory category) => category switch
    {
        Suikoden2ItemCategory.Regular => "Regular item",
        Suikoden2ItemCategory.Farming => "Farming item",
        Suikoden2ItemCategory.Trade => "Trade item",
        Suikoden2ItemCategory.Base => "Headquarters item",
        Suikoden2ItemCategory.Food => "Food",
        Suikoden2ItemCategory.Rune => "Rune",
        Suikoden2ItemCategory.Helmet => "Helmet",
        Suikoden2ItemCategory.Armor => "Armor",
        Suikoden2ItemCategory.Shield => "Shield",
        Suikoden2ItemCategory.Accessory => "Accessory",
        _ => category.ToString(),
    };

    private static IEnumerable<Suikoden2ItemDefinition> InventoryChoices(Suikoden2Inventory inventory, int slot)
    {
        if (inventory == Suikoden2Inventory.Bath)
        {
            bool paintingSlot = slot is 2 or 5;
            return Suikoden2Catalog.Items.Where(item =>
            {
                bool painting = item.Id == 0 || item.Id is >= 18 and <= 22 or >= 42 and <= 44;
                bool ornament = item.Id == 0 || item.Id is >= 1 and <= 17 or >= 45 and <= 50;
                return item.Category == Suikoden2ItemCategory.Trade && (paintingSlot ? painting : ornament);
            });
        }

        return Suikoden2Catalog.Items
            .GroupBy(item => (item.Id, UseCount: item.Id == 0 ? 0 : item.UseCount))
            .Select(group => group.First());
    }

    private static string FormatS2CatalogChoice(Suikoden2ItemDefinition item) =>
        FormatS2CatalogChoice(item, item.Category, item.Id);

    private static string FormatS2CatalogChoice(Suikoden2ItemDefinition? item, Suikoden2ItemCategory category, int id) =>
        $"{(id == 0 ? "None" : item?.Name ?? $"Unknown item {id}")}{(item?.StoryCritical == true ? " [Story-critical]" : string.Empty)} — {category}:{id}";

    private static string FormatNamedId(string name, string kind, int id) => $"{name} — {kind} {id}";

    private static string FormatCharacterChoice(int id, string name) => $"{name} — character {id}";

    private static string FormatSuikoden1Recruitment(int flag) => flag switch
    {
        0 => "Not recruited — member flag 0",
        9 => "Recruited — member flag 9",
        _ => $"Other state (preserved) — member flag {flag}",
    };

    private static string FormatSuikoden2RecruitmentStatus(int status) => status switch
    {
        0 => "Not recruited — 0",
        1 => "Spoken to, not yet recruited — 1",
        70 => "Recruited automatically — 70",
        71 => "Recruited manually — 71",
        86 => "Event-locked, unavailable for party — 86",
        212 => "Deceased — 212",
        213 => "On leave — 213",
        _ => $"Unknown state (preserved) — {status}",
    };

    private static string InventoryDisplayName(Suikoden2Inventory inventory) => inventory switch
    {
        Suikoden2Inventory.Party => "Party inventory",
        Suikoden2Inventory.Warehouse => "Warehouse",
        Suikoden2Inventory.Bath => "Bath / display item",
        Suikoden2Inventory.RoomExperimental => "Room item (experimental)",
        _ => inventory.ToString(),
    };

    private void AddHeadquartersLevelChoice(string label, string path, int value, Action<int> apply)
    {
        string[] choices =
        [
            "Level 0 — Pre-headquarters state",
            "Level 1",
            "Level 2",
            "Level 3",
            "Level 4 — Maximum",
        ];
        string selected = choices.SingleOrDefault(choice => ParseHeadquartersLevel(choice) == value) ?? $"Level {value} — Outside reviewed range";
        AddChoice(
            label,
            path,
            selected,
            choices,
            text => apply(ParseHeadquartersLevel(text)),
            "Reviewed range: 0–4. Level 0 is retained for pre-headquarters saves; playable headquarters levels are 1–4 and level 4 is the cap. Direct changes can desynchronize story-driven facilities.");
    }

    private void AddNumber(string label, string path, int value, Action<int> apply, string? warning = null)
    {
        AddString(label, path, value.ToString(CultureInfo.InvariantCulture), text => apply(ParseInteger(text)), warning);
    }

    private void AddString(string label, string path, string value, Action<string> apply, string? warning = null)
    {
        allFields.Add(new(label, path, value, false, warning, apply, field => ApplyEdit($"Changed {field.Label}", field.ApplyValue)));
    }

    private void AddChoice(
        string label,
        string path,
        string value,
        IEnumerable<string> choices,
        Action<string> apply,
        string? warning = null)
    {
        List<string> materialized = choices.Distinct(StringComparer.Ordinal).ToList();
        if (!materialized.Contains(value, StringComparer.Ordinal))
        {
            materialized.Insert(0, value);
        }

        allFields.Add(new(label, path, value, false, warning, apply, field => ApplyEdit($"Changed {field.Label}", field.ApplyValue), materialized));
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

    private static T ParseNamedChoice<T>(string value, IReadOnlyDictionary<string, T> choices, string label)
    {
        Guard.Valid(choices.TryGetValue(value, out T? result), $"Choose a reviewed {label} by name from the list.");
        return result!;
    }

    private static bool ParseRecruitmentBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "recruited" or "true" or "9" => true,
        "unrecruited" or "false" or "0" => false,
        _ when ParseLabeledInteger(value, "member flag") == 9 => true,
        _ when ParseLabeledInteger(value, "member flag") == 0 => false,
        _ => throw new SaveEditorException(SaveErrorCode.ValidationFailed, "Choose Recruited (flag 9) or Not recruited (flag 0)."),
    };

    private static int ParseLabeledInteger(string value, string label)
    {
        string marker = $"— {label} ";
        int markerIndex = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Guard.Valid(markerIndex >= 0, $"Choose a reviewed {label} value from the list.");
        return ParseInteger(value[(markerIndex + marker.Length)..]);
    }

    private static int ParseTrailingInteger(string value)
    {
        int separator = value.LastIndexOf(" — ", StringComparison.Ordinal);
        Guard.Valid(separator >= 0, "Choose a reviewed status from the list.");
        return ParseInteger(value[(separator + 3)..]);
    }

    private static int ParseHeadquartersLevel(string value)
    {
        Guard.Valid(value.StartsWith("Level ", StringComparison.OrdinalIgnoreCase), "Choose a reviewed headquarters level from the list.");
        string number = value[6..].Split(' ', 2)[0];
        return ParseInteger(number);
    }

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
            message + "\n\nContinue with the validated output?",
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
        ((RelayCommand)ApplyAllCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)MaximizeAndEquipPartyCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)GiveAllSafeItemsCommand).RaiseCanExecuteChanged();
    }
}
