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
        builder.AddNumber("Level", $"player_base[{id}].level", () => adapter.Characters.Single(value => value.Id == id).Level, value => adapter.SetCharacterScalar(id, "level", value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterLevel);
        builder.AddNumber("EXP", $"player_base[{id}].exp", () => adapter.Characters.Single(value => value.Id == id).Experience, value => adapter.SetCharacterScalar(id, "exp", value), minimum: 0);
        builder.AddNumber("Current HP", $"player_base[{id}].hp", () => adapter.Characters.Single(value => value.Id == id).CurrentHp, value => adapter.SetCharacterScalar(id, "hp", value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterHp);
        builder.AddNumber("Maximum HP", $"player_base[{id}].max_hp", () => adapter.Characters.Single(value => value.Id == id).MaximumHp, value => adapter.SetCharacterScalar(id, "max_hp", value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterHp);
        for (int spell = 1; spell <= 4; spell++)
        {
            int captured = spell;
            builder.AddNumber($"Current MP · spell level {spell}", $"player_base[{id}].magic_point[{spell}]", () => adapter.Characters.Single(value => value.Id == id).CurrentMagicPoints[captured], value => adapter.SetMagicPoint(id, captured, value), minimum: 0, maximum: 9);
        }

        for (int stat = 0; stat < Suikoden1Adapter.StatNames.Count; stat++)
        {
            int captured = stat;
            builder.AddNumber(Suikoden1Adapter.StatNames[stat], $"player_base[{id}].noryoku[{stat}]", () => adapter.Characters.Single(value => value.Id == id).Stats[captured], value => adapter.SetCharacterStat(id, captured, value), minimum: 0, maximum: Suikoden1Adapter.MaximumCharacterStat);
        }

        builder.AddNumber(
            "Weapon ID",
            $"player_base[{id}].buki_data.buki_id",
            () => adapter.Characters.Single(item => item.Id == id).WeaponId,
            value => adapter.SetWeapon(id, value, adapter.Characters.Single(item => item.Id == id).WeaponLevel),
            "Weapon-name mappings are not verified, so only the numeric ID is shown.",
            minimum: 0);
        builder.AddNumber(
            "Weapon level",
            $"player_base[{id}].buki_data.level",
            () => adapter.Characters.Single(item => item.Id == id).WeaponLevel,
            value => adapter.SetWeapon(id, adapter.Characters.Single(item => item.Id == id).WeaponId, value),
            minimum: 0,
            maximum: Suikoden1Adapter.MaximumWeaponLevel);
        builder.AddChoice(
            "Equipped rune",
            $"player_base[{id}].monsyo_data.monsyo_id",
            () =>
            {
                int runeId = adapter.Characters.Single(value => value.Id == id).RuneId;
                return SectionText.FormatNamedId(Suikoden1Catalog.Runes.GetValueOrDefault(runeId, $"Rune {runeId}"), "rune", runeId);
            },
            Suikoden1Catalog.Runes.OrderBy(item => item.Key).Select(item => SectionText.FormatNamedId(item.Value, "rune", item.Key)),
            value => adapter.SetCharacterRune(id, SectionText.ParseLabeledInteger(value, "rune")));
        for (int index = 0; index < character.WeaponRunePieces.Count; index++)
        {
            int captured = index;
            builder.AddNumber($"Weapon rune-piece value {index}", $"player_base[{id}].buki_data.monsyo[{index}]", () => adapter.Characters.Single(value => value.Id == id).WeaponRunePieces[captured], value => adapter.SetWeaponRunePiece(id, captured, value), minimum: 0);
        }

        for (int slot = 0; slot < character.Items.Count; slot++)
        {
            int captured = slot;
            JsonObject item = character.Items[slot]!.AsObject();
            int equipment = item["soubi"]!.GetValue<int>();
            builder.AddChoice(
                $"Carried item {slot + 1}",
                $"player_base[{id}].item[{slot}].item_id",
                () =>
                {
                    int liveItemId = adapter.Characters.Single(value => value.Id == id).Items[captured]!.AsObject()["item_id"]!.GetValue<int>();
                    return SectionText.FormatNamedId(Suikoden1Catalog.ItemName(liveItemId), "item", liveItemId);
                },
                Suikoden1Catalog.Items.OrderBy(item => item.Key).Select(item => SectionText.FormatNamedId(item.Value, "item", item.Key)),
                value => SetItemPart(adapter, id, captured, SectionText.ParseLabeledInteger(value, "item"), null, null));
            builder.AddChoice(
                $"Carried item {slot + 1} equipment state",
                $"player_base[{id}].item[{slot}].soubi",
                () =>
                {
                    int liveEquipment = adapter.Characters.Single(value => value.Id == id).Items[captured]!.AsObject()["soubi"]!.GetValue<int>();
                    return SectionText.FormatNamedId(Suikoden1Catalog.EquipmentSlots.GetValueOrDefault(liveEquipment, $"State {liveEquipment}"), "state", liveEquipment);
                },
                Suikoden1Catalog.EquipmentSlots.OrderBy(item => item.Key).Select(item => SectionText.FormatNamedId(item.Value, "state", item.Key)),
                value => SetItemPart(adapter, id, captured, null, SectionText.ParseLabeledInteger(value, "state"), null),
                equipment >= 129 ? "States 129–133 are verified non-removable equipment states." : null);
            builder.AddNumber($"Carried item {slot + 1} remaining uses", $"player_base[{id}].item[{slot}].data", () => adapter.Characters.Single(value => value.Id == id).Items[captured]!.AsObject()["data"]!.GetValue<int>(), value => SetItemPart(adapter, id, captured, null, null, value), minimum: 0);
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
