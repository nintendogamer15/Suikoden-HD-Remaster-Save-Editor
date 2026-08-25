// SPDX-License-Identifier: 0BSD
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace SuikodenHdSaveEditor.App.Services;

public sealed class AvaloniaUserInteraction : IUserInteraction
{
    private readonly Window owner;
    private static readonly FilePickerFileType SaveType = new("Suikoden encrypted saves")
    {
        Patterns = ["Data*", "*"],
        MimeTypes = ["application/octet-stream", "text/plain"],
    };

    public AvaloniaUserInteraction(Window owner)
    {
        this.owner = owner;
    }

    public async Task<string?> PickSaveToOpenAsync()
    {
        IReadOnlyList<IStorageFile> files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open encrypted Suikoden save",
            AllowMultiple = false,
            FileTypeFilter = [SaveType],
        });
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickSaveFolderAsync()
    {
        IReadOnlyList<IStorageFolder> folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Suikoden save folder",
            AllowMultiple = false,
        });
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    public async Task<string?> PickSaveDestinationAsync(string suggestedName)
    {
        IStorageFile? file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save encrypted Suikoden save as",
            SuggestedFileName = suggestedName,
            FileTypeChoices = [SaveType],
            ShowOverwritePrompt = true,
        });
        return file?.TryGetLocalPath();
    }

    public Task<bool> ConfirmAsync(string title, string message, string acceptText) => ShowDialogAsync(title, message, acceptText, true);

    public async Task ShowMessageAsync(string title, string message)
    {
        await ShowDialogAsync(title, message, "OK", false);
    }

    public async Task ShowAboutAsync(string content)
    {
        Window dialog = new()
        {
            Title = "About · Credits · Licenses",
            Width = 760,
            Height = 650,
            MinWidth = 560,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        Button close = new() { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 96 };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 16,
            Children =
            {
                new TextBox
                {
                    Text = content,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                close,
            },
        };
        Grid.SetRow(close, 1);
        await dialog.ShowDialog(owner);
    }

    private async Task<bool> ShowDialogAsync(string title, string message, string acceptText, bool showCancel)
    {
        bool accepted = false;
        Window dialog = new()
        {
            Title = title,
            Width = 560,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        Button accept = new() { Content = acceptText, MinWidth = 110 };
        accept.Click += (_, _) =>
        {
            accepted = true;
            dialog.Close();
        };
        Button cancel = new() { Content = "Cancel", MinWidth = 96, IsVisible = showCancel };
        cancel.Click += (_, _) => dialog.Close();
        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, accept },
        };
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 20,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 500 },
                buttons,
            },
        };
        await dialog.ShowDialog(owner);
        return accepted;
    }
}
