// SPDX-License-Identifier: 0BSD
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Saves;

/// <summary>
/// Compares two <see cref="SaveDocument"/> instances by their JSON content.
/// </summary>
/// <remarks>
/// <para>
/// SaveEditor.Ui's pre-replace round-trip guard decodes what it is about to write and compares
/// it against the document in memory. <see cref="SaveDocument"/> is a mutable class that does
/// not override <c>Equals</c>, so the default comparer would compare references, never match,
/// and fail every save. That failure is loud and the framework diagnoses it.
/// </para>
/// <para>
/// The dangerous mistake is the opposite one: a comparer that is too permissive makes the guard
/// pass unconditionally and a lossy write is reported as "Saved." Nothing detects that, which
/// is why this compares the whole tree rather than the fields the UI happens to edit, and why
/// a test mutates a field the UI never touches and asserts this returns false.
/// </para>
/// </remarks>
public sealed class SuikodenDocumentComparer : IEqualityComparer<SaveDocument>
{
    /// <summary>The shared instance.</summary>
    public static SuikodenDocumentComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(SaveDocument? x, SaveDocument? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return SaveDocument.SemanticallyEquals(x.Root, y.Root);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Constant, so equality always runs through <see cref="Equals(SaveDocument, SaveDocument)"/>.
    /// The document is mutable, so any content-derived hash would go stale the moment a field is
    /// written and would break the contract that equal values hash equally.
    /// </remarks>
    public int GetHashCode(SaveDocument obj) => 0;
}
