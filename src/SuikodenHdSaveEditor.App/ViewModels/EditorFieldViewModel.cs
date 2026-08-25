// SPDX-License-Identifier: 0BSD
using System.Windows.Input;

namespace SuikodenHdSaveEditor.App.ViewModels;

public sealed class EditorFieldViewModel : ObservableObject
{
    private readonly Action<string>? apply;
    private string value;

    public EditorFieldViewModel(
        string label,
        string path,
        string value,
        bool isReadOnly,
        string? warning,
        Action<string>? apply,
        Action<EditorFieldViewModel>? applyRequested = null,
        IReadOnlyList<string>? choices = null)
    {
        Label = label;
        Path = path;
        this.value = value;
        OriginalValue = value;
        IsReadOnly = isReadOnly;
        Warning = warning ?? string.Empty;
        this.apply = apply;
        Choices = choices ?? [];
        ApplyCommand = new RelayCommand(
            () => applyRequested?.Invoke(this),
            () => !IsReadOnly && apply is not null && applyRequested is not null);
    }

    public string Label { get; }

    public string Path { get; }

    public string Value
    {
        get => value;
        set => SetProperty(ref this.value, value);
    }

    public string OriginalValue { get; }

    public bool HasPendingValue => !string.Equals(Value, OriginalValue, StringComparison.Ordinal);

    public bool IsReadOnly { get; }

    public IReadOnlyList<string> Choices { get; }

    public bool HasChoices => Choices.Count > 0;

    public bool UsesTextEntry => !HasChoices;

    public string Warning { get; }

    public bool HasWarning => Warning.Length > 0;

    public ICommand ApplyCommand { get; }

    internal void ApplyValue()
    {
        if (HasPendingValue)
        {
            apply?.Invoke(Value);
        }
    }
}
public sealed record ChoiceViewModel(int Id, string Name)
{
    public string DisplayName => $"{Id}: {Name}";
}

public sealed record SlotEntryViewModel(string Path, int Slot, string Game)
{
    public string DisplayName => $"{Game} · Data{Slot}";
}
