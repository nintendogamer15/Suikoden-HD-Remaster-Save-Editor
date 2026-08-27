// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Formats.Suikoden1;

namespace SuikodenHdSaveEditor.App.Sections;

internal static class Suikoden1CharacterSection
{
    public static void Build(Suikoden1Adapter adapter, SectionBuilder builder, SectionContext context)
    {
        if (context.SelectedCharacterId is not int id)
        {
            builder.AddReadOnly("Character search", "characters", "No matching character");
            return;
        }

        Suikoden1CharacterView character = adapter.Characters.Single(value => value.Id == id);
        builder.AddNumber("Level", $"player_base[{id}].level", character.Level, value => adapter.SetCharacterScalar(id, "level", value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterLevel);
        builder.AddNumber("EXP", $"player_base[{id}].exp", character.Experience, value => adapter.SetCharacterScalar(id, "exp", value), minimum: 0);
        builder.AddNumber("Current HP", $"player_base[{id}].hp", character.CurrentHp, value => adapter.SetCharacterScalar(id, "hp", value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterHp);
        builder.AddNumber("Maximum HP", $"player_base[{id}].max_hp", character.MaximumHp, value => adapter.SetCharacterScalar(id, "max_hp", value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterHp);
        for (int spell = 1; spell <= 4; spell++)
        {
            int captured = spell;
            builder.AddNumber($"Current MP · spell level {spell}", $"player_base[{id}].magic_point[{spell}]", character.CurrentMagicPoints[spell], value => adapter.SetMagicPoint(id, captured, value), minimum: 0, maximum: 9);
        }

        for (int stat = 0; stat < Suikoden1Adapter.StatNames.Count; stat++)
        {
            int captured = stat;
            builder.AddNumber(Suikoden1Adapter.StatNames[stat], $"player_base[{id}].noryoku[{stat}]", character.Stats[stat], value => adapter.SetCharacterStat(id, captured, value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterStat);
        }

        builder.AddNumber(
            "Weapon ID",
            $"player_base[{id}].buki_data.buki_id",
            character.WeaponId,
            value => adapter.SetWeapon(id, value, adapter.Characters.Single(item => item.Id == id).WeaponLevel),
            "Weapon-name mappings are not verified, so only the numeric ID is shown.",
            minimum: 0);
        builder.AddNumber(
            "Weapon level",
            $"player_base[{id}].buki_data.level",
            character.WeaponLevel,
            value => adapter.SetWeapon(id, adapter.Characters.Single(item => item.Id == id).WeaponId, value),
            minimum: 0,
            maximum: Suikoden1Adapter.MaximumWeaponLevel);
        builder.AddChoice(
            "Equipped rune",
            $"player_base[{id}].monsyo_data.monsyo_id",
            SectionText.FormatNamedId(Suikoden1Catalog.Runes.GetValueOrDefault(character.RuneId, $"Rune {character.RuneId}"), "rune", character.RuneId),
            Suikoden1Catalog.Runes.OrderBy(item => item.Key).Select(item => SectionText.FormatNamedId(item.Value, "rune", item.Key)),
            value => adapter.SetCharacterRune(id, SectionText.ParseLabeledInteger(value, "rune")));
        for (int index = 0; index < character.WeaponRunePieces.Count; index++)
        {
            int captured = index;
            builder.AddNumber($"Weapon rune-piece value {index}", $"player_base[{id}].buki_data.monsyo[{index}]", character.WeaponRunePieces[index], value => adapter.SetWeaponRunePiece(id, captured, value), minimum: 0);
        }

        for (int slot = 0; slot < character.Items.Count; slot++)
        {
            int captured = slot;
            JsonObject item = character.Items[slot]!.AsObject();
            int itemId = item["item_id"]!.GetValue<int>();
            int equipment = item["soubi"]!.GetValue<int>();
            int uses = item["data"]!.GetValue<int>();
            builder.AddChoice(
                $"Carried item {slot + 1}",
                $"player_base[{id}].item[{slot}].item_id",
                SectionText.FormatNamedId(Suikoden1Catalog.ItemName(itemId), "item", itemId),
                Suikoden1Catalog.Items.OrderBy(item => item.Key).Select(item => SectionText.FormatNamedId(item.Value, "item", item.Key)),
                value => SetItemPart(adapter, id, captured, SectionText.ParseLabeledInteger(value, "item"), null, null));
            builder.AddChoice(
                $"Carried item {slot + 1} equipment state",
                $"player_base[{id}].item[{slot}].soubi",
                SectionText.FormatNamedId(Suikoden1Catalog.EquipmentSlots.GetValueOrDefault(equipment, $"State {equipment}"), "state", equipment),
                Suikoden1Catalog.EquipmentSlots.OrderBy(item => item.Key).Select(item => SectionText.FormatNamedId(item.Value, "state", item.Key)),
                value => SetItemPart(adapter, id, captured, null, SectionText.ParseLabeledInteger(value, "state"), null),
                equipment >= 129 ? "States 129–133 are verified non-removable equipment states." : null);
            builder.AddNumber($"Carried item {slot + 1} remaining uses", $"player_base[{id}].item[{slot}].data", uses, value => SetItemPart(adapter, id, captured, null, null, value), minimum: 0);
        }
    }

    private static void SetItemPart(Suikoden1Adapter adapter, int characterId, int slot, int? itemId, int? equipment, int? uses)
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
}
