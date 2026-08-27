// SPDX-License-Identifier: 0BSD
using System.Globalization;
using System.Text.Json.Nodes;
using SaveEditor.Ui.Editing;
using SuikodenHdSaveEditor.App.Editing;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>
/// Collects the fields for one section, in display order.
/// </summary>
/// <remarks>
/// <para>
/// This replaces the <c>AddNumber</c> / <c>AddString</c> / <c>AddChoice</c> / <c>AddReadOnly</c>
/// helpers the view model used to carry. The shape is deliberately the same so the section
/// bodies port across as a rename rather than a redesign, but the fields are now typed: what
/// used to be a string parsed by hand on apply is a numeric field with real bounds, and a
/// choice list is a provider the control can filter.
/// </para>
/// <para>
/// Every write goes through <see cref="GuardedEdit.Wrap"/>, so an adapter that rejects an edit
/// after it has already touched the tree still leaves the document as it was.
/// </para>
/// </remarks>
public sealed class SectionBuilder(JsonObject root, SnapshotEditHistory history)
{
    private readonly List<FieldViewModel> fields = [];
    private int sequence;

    /// <summary>The fields added so far.</summary>
    public IReadOnlyList<FieldViewModel> Fields => fields;

    /// <summary>Adds a free-text field.</summary>
    public void AddString(string label, string path, string value, Action<string> apply, string? warning = null) =>
        fields.Add(new TextFieldViewModel(
            new TextFieldDescriptor
            {
                Key = NextKey(label),
                Label = label,
                Path = path,
                WarningText = warning,
                Read = () => value,
                Write = GuardedEdit.Wrap(root, history, apply),
            },
            history));

    /// <summary>Adds a whole-number field.</summary>
    /// <remarks>
    /// <paramref name="minimum"/> and <paramref name="maximum"/> default to the full 32-bit
    /// range because that is what the previous hand-rolled parse accepted; a caller that knows
    /// the real bound should pass it, so the control refuses out-of-range input up front
    /// instead of letting the adapter raise.
    /// </remarks>
    public void AddNumber(
        string label,
        string path,
        int value,
        Action<int> apply,
        string? warning = null,
        int minimum = int.MinValue,
        int maximum = int.MaxValue) =>
        fields.Add(new NumericFieldViewModel(
            new NumericFieldDescriptor
            {
                Key = NextKey(label),
                Label = label,
                Path = path,
                WarningText = warning,
                Minimum = minimum,
                Maximum = maximum,
                ShowSpinner = true,
                Read = () => value,
                Write = GuardedEdit.Wrap<long>(root, history, written => apply(checked((int)written))),
            },
            history));

    /// <summary>Adds a field whose value is chosen from a reviewed list.</summary>
    /// <remarks>
    /// The current value is kept in the list even when it is not one of the offered choices, so
    /// a save holding an unreviewed value still displays it rather than silently reading as
    /// something else.
    /// </remarks>
    public void AddChoice(
        string label,
        string path,
        string value,
        IEnumerable<string> choices,
        Action<string> apply,
        string? warning = null)
    {
        List<string> materialized = [.. choices.Distinct(StringComparer.Ordinal)];
        if (!materialized.Contains(value, StringComparer.Ordinal))
        {
            materialized.Insert(0, value);
        }

        fields.Add(new ChoiceFieldViewModel(
            new ChoiceFieldDescriptor
            {
                Key = NextKey(label),
                Label = label,
                Path = path,
                WarningText = warning,
                Read = () => value,
                Write = GuardedEdit.Wrap(root, history, apply),
                Options = new NamedChoiceProvider(materialized),
            },
            history));
    }

    /// <summary>Adds a value this editor shows but will not let anyone change.</summary>
    public void AddReadOnly(string label, string path, string value, string? warning = null) =>
        fields.Add(new ReadOnlyFieldViewModel(
            new ReadOnlyFieldDescriptor
            {
                Key = NextKey(label),
                Label = label,
                Path = path,
                WarningText = warning,
                IsReadOnly = true,
                Read = () => value,
            },
            history));

    /// <summary>Builds the section from everything added.</summary>
    public SectionEditor Build(string key, string title) => new(key, title, fields, history);

    // Keys have to be unique within a section, and labels repeat across characters and
    // inventory slots, so the ordinal disambiguates rather than the label alone.
    private string NextKey(string label) =>
        string.Create(CultureInfo.InvariantCulture, $"{label}#{sequence++}");
}

/// <summary>Offers a fixed, already-reviewed list of choices.</summary>
internal sealed class NamedChoiceProvider(IReadOnlyList<string> choices) : IChoiceProvider
{
    public ValueTask<IReadOnlyList<ChoiceOption>> GetOptionsAsync(
        string filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<string> matches = string.IsNullOrWhiteSpace(filter)
            ? choices
            : choices.Where(choice => choice.Contains(filter, StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult<IReadOnlyList<ChoiceOption>>(
            [.. matches.Select(choice => new ChoiceOption(choice, choice))]);
    }
}
