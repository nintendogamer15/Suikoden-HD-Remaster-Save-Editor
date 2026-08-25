// SPDX-License-Identifier: 0BSD
using System.Windows.Input;

namespace SuikodenHdSaveEditor.App.ViewModels;

public sealed class EditorFieldViewModel : ObservableObject
{
    private string value;

    public EditorFieldViewModel(
        string label,
        string path,
        string value,
        bool isReadOnly,
        string? warning,
        Action<string>? apply)
    {
        Label = label;
        Path = path;
        this.value = value;
        IsReadOnly = isReadOnly;
        Warning = warning ?? string.Empty;
        ApplyCommand = new RelayCommand(() => apply?.Invoke(Value), () => !IsReadOnly && apply is not null);
    }

    public string Label { get; }

    public string Path { get; }

    public string Value
    {
        get => value;
        set => SetProperty(ref this.value, value);
    }

    public bool IsReadOnly { get; }

    public string Warning { get; }

    public bool HasWarning => Warning.Length > 0;

    public ICommand ApplyCommand { get; }
}
public sealed record ChoiceViewModel(int Id, string Name)
{
    public string DisplayName => $"{Id}: {Name}";
}

public sealed record SlotEntryViewModel(string Path, int Slot, string Game)
{
    public string DisplayName => $"{Game} · Data{Slot}";
}
