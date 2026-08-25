// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core;

public sealed record PartyOptimizationResult(
    int CharactersUpdated,
    int EquipmentSlotsUpdated,
    int LockedOrUnavailableSlotsPreserved);
