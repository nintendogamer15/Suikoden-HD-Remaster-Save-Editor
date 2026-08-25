// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.App.Services;

public interface IUserInteraction
{
    Task<string?> PickSaveToOpenAsync();

    Task<string?> PickSaveFolderAsync();

    Task<string?> PickSaveDestinationAsync(string suggestedName);

    Task<bool> ConfirmAsync(string title, string message, string acceptText);

    Task ShowMessageAsync(string title, string message);

    Task ShowAboutAsync(string content);
}
