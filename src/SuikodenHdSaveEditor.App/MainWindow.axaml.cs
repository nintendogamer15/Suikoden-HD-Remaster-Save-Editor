// SPDX-License-Identifier: 0BSD
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveEditor.Ui.Codecs;
using SaveEditor.Ui.Controls;
using SaveEditor.Ui.Dialogs;
using SaveEditor.Ui.Display;
using SaveEditor.Ui.Editing;
using SaveEditor.Ui.Hosting;
using SaveEditor.Ui.Interaction;
using SaveEditor.Ui.Settings;
using SaveEditor.Ui.Shell;
using SaveEditor.Ui.Theming;
using SaveEditor.Ui.Workflow;
using SuikodenHdSaveEditor.App.Editing;
using SuikodenHdSaveEditor.App.Saves;
using SuikodenHdSaveEditor.App.Sections;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App;

/// <summary>The editor window, and the composition root for everything in it.</summary>
/// <remarks>
/// Avalonia never disposes a window, so the session and shell view model it owns are released
/// when the window closes.
/// </remarks>
public partial class MainWindow : Window, IDisposable
{
    private bool disposed;

    private readonly SnapshotEditHistory history = new();
    private readonly SuikodenDocumentSession session;
    private readonly EditorShellViewModel viewModel;
    private readonly Dictionary<SectionKind, SectionHost> hosts = [];
    private readonly ThemedUserInteraction interaction;

    /// <summary>Creates the window and wires the editor together.</summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        EditorSettingsStore settings = new(EditorApplicationId.Parse("SuikodenHdSaveEditor"));
        ThemeController theme = new(
            Application.Current!.Styles.OfType<SaveEditorTheme>().Single(),
            settings,
            CatppuccinAccent.Blue);

        WindowEditorHost host = new(this);
        interaction = new ThemedUserInteraction(this, PathDisplayFormatter.Default);

        SuikodenSaveCodec codec = new();
        SaveCodecRegistry<SaveDocument> registry = new(
        [
            new CodecRegistration<SaveDocument>(new SuikodenSaveDetector(), codec),
        ]);

        SafeFileWorkflow<SaveDocument> workflow = new(new SafeFileWorkflowOptions<SaveDocument>
        {
            Registry = registry,
            Interaction = interaction,

            // SaveDocument is a mutable class with no equality contract, so the default
            // comparer would compare references and fail the pre-replace round-trip check on
            // every save.
            DocumentComparer = SuikodenDocumentComparer.Instance,

            // Save As has never replaced an existing file in this editor.
            WritePolicy = new SuikodenWritePolicy(),
        });

        session = new SuikodenDocumentSession(workflow, history, codec, interaction)
        {
            // Drafts live on the field view models, which the session deliberately knows
            // nothing about. Without this the exit guard cannot see typed-but-unapplied edits.
            PendingEditProbe = () => hosts.Values.Any(section => section.Editor?.HasPendingEdits == true),
        };

        // Every operation that swaps the document instance - open, reload, restore - leaves the
        // sections bound to the previous tree, editing something that will never be saved. They
        // are rebuilt rather than refreshed.
        session.DocumentChanged += (_, _) => RebuildSections();

        // A wholesale snapshot restore bypasses the per-field refresh closures the framework
        // records at apply time, so the fields have to be told. Never on Record: refreshing on
        // every apply would discard pending drafts across the whole editor.
        history.Restored += (_, _) => RefreshSections();

        viewModel = new EditorShellViewModel(session, interaction, settings, host, theme)
        {
            AboutMessage = EmbeddedLegalNotices.Load(),
        };

        RegisterSections();

        EditorShell shell = this.FindControl<EditorShell>("Shell")!;
        shell.DataContext = viewModel;
        DragDropAdapter.Attach(shell, viewModel);

        WireFolderSlots();
        WireRestoreFromBackup();

        Closed += (_, _) => Dispose();
        Loaded += async (_, _) =>
        {
            await theme.InitializeAsync().ConfigureAwait(true);
            await viewModel.InitializeAsync().ConfigureAwait(true);
        };
    }

    /// <summary>The shell view model, for tests and the smoke-test path.</summary>
    public EditorShellViewModel ViewModel => viewModel;

    /// <summary>Releases the session's file handles and the shell view model.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        viewModel.Dispose();
        session.Dispose();
        GC.SuppressFinalize(this);
    }

    private void RegisterSections()
    {
        List<SectionDescriptor> descriptors = [];

        foreach ((SectionKind kind, string title, string subtitle) in SuikodenSectionFactory.Sections)
        {
            SectionHost host = new(kind);
            hosts[kind] = host;

            descriptors.Add(new SectionDescriptor
            {
                Key = kind.ToString(),
                Title = title,
                Subtitle = subtitle,
                BodyMode = SectionBodyMode.Custom,
                Body = host.Body,
            });
        }

        viewModel.RegisterSections(descriptors);
    }

    private void RebuildSections()
    {
        SaveDocument? document = session.Document;

        // Rebinding clears the history: undo steps describe a tree that is no longer open.
        history.Bind(document?.Root);

        foreach (SectionHost host in hosts.Values)
        {
            host.Rebuild(document, history, BuildBulkActionsFor(host.Kind, document));
        }
    }

    private void RefreshSections()
    {
        foreach (SectionHost host in hosts.Values)
        {
            if (host.Editor is { } editor)
            {
                GuardedEdit.RefreshPreservingRejections(editor);
            }
        }
    }

    private void WireFolderSlots()
    {
        ComboBox picker = this.FindControl<ComboBox>("SlotPicker")!;
        Button open = this.FindControl<Button>("OpenSlotButton")!;

        open.Click += async (_, _) =>
        {
            if (picker.SelectedItem is SaveSlotEntry entry)
            {
                await viewModel.OpenPathAsync(entry.Path).ConfigureAwait(true);
            }
        };

        session.Opened += (_, path) =>
        {
            string? directory = Path.GetDirectoryName(path);
            if (directory is null)
            {
                return;
            }

            try
            {
                picker.ItemsSource = SaveSlotBrowser.Discover(directory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                // Slot discovery is a convenience. A folder that cannot be listed must not stop
                // the file that just opened from being edited.
                picker.ItemsSource = null;
            }
        };
    }

    private void WireRestoreFromBackup()
    {
        Button restore = this.FindControl<Button>("RestoreButton")!;

        restore.Click += async (_, _) =>
        {
            string? backup = await interaction.PickOpenFileAsync(
                new FilePickerRequest("Choose a backup to restore", [new SuikodenSaveCodec().Format]))
                .ConfigureAwait(true);

            if (backup is null)
            {
                return;
            }

            if (await session.RestoreFromBackupAsync(backup).ConfigureAwait(true))
            {
                // ReplaceDocument does not raise DocumentChanged, so the rebuild is explicit.
                // Without it every section stays bound to the tree that was just replaced.
                RebuildSections();
            }
        };
    }

    private StackPanel? BuildBulkActionsFor(SectionKind kind, SaveDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        StackPanel panel = new() { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };

        if (kind == SectionKind.Characters)
        {
            panel.Children.Add(BulkButton(
                "Max stats + best party gear",
                () => BulkActions.MaximizeAndEquipPartyAsync(document, history, interaction)));
        }

        // The safe-item fill is a Suikoden II container feature; Suikoden I has no equivalent
        // reviewed set, so the button simply is not offered there.
        if (kind == SectionKind.Inventory && document.Game == GameKind.Suikoden2)
        {
            panel.Children.Add(BulkButton(
                "Give all safe party items",
                () => BulkActions.GiveAllSafeItemsAsync(document, history, interaction)));
        }

        return panel.Children.Count == 0 ? null : panel;
    }

    private Button BulkButton(string caption, Func<ValueTask<string?>> action)
    {
        Button button = new() { Content = caption };
        AutomationProperties.SetName(button, caption);

        button.Click += async (_, _) =>
        {
            string? outcome = await action().ConfigureAwait(true);
            if (outcome is not null)
            {
                viewModel.StatusMessage = outcome;
                RefreshSections();
            }
        };

        return button;
    }

    /// <summary>Holds one section's controls so they survive a document being swapped.</summary>
    private sealed class SectionHost(SectionKind kind)
    {
        private readonly FieldList fields = new();
        private readonly SectionToolbar toolbar = new();

        public SectionEditor? Editor { get; private set; }

        public Control Body { get; } = new DockPanel();

        public SectionKind Kind => kind;

        public void Rebuild(SaveDocument? document, SnapshotEditHistory history, Control? bulkActions)
        {
            EnsureLayout();
            toolbar.BulkActions = bulkActions;

            Editor = document is null
                ? null
                : SuikodenSectionFactory.Create(kind, document, history, new SectionContext());

            toolbar.Editor = Editor;
            fields.Fields = Editor?.VisibleFields;
        }

        private void EnsureLayout()
        {
            if (Body is not DockPanel panel || panel.Children.Count > 0)
            {
                return;
            }

            DockPanel.SetDock(toolbar, Dock.Top);
            panel.Children.Add(toolbar);
            panel.Children.Add(fields);
        }
    }
}
