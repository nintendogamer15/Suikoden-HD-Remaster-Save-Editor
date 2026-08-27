// SPDX-License-Identifier: 0BSD
using System.Globalization;
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>Suikoden II per-character fields.</summary>
/// <remarks>A direct port of the view model's <c>BuildSuikoden2Character</c> method.</remarks>
internal static class Suikoden2CharacterSection
{
    public static void Build(Suikoden2Adapter adapter, SectionBuilder builder, SectionContext context)
    {
        if (context.SelectedCharacterId is not int id)
        {
            builder.AddReadOnly("Character search", "characters", "No matching character");
            return;
        }

        Suikoden2CharacterView character = adapter.Characters[id];
        builder.AddReadOnly("Recruitment status", $"chara_flag[{id}]", () => adapter.Characters[id].RecruitmentStatus.ToString(CultureInfo.InvariantCulture));
        builder.AddReadOnly("Current party", "party_data.party_cha_no", () => adapter.Characters[id].IsInParty ? "Yes" : "No");

        int ReadScalar(string field) => field switch
        {
            "level" => adapter.Characters[id].Level,
            "exp" => adapter.Characters[id].Experience,
            "now_hp" => adapter.Characters[id].CurrentHp,
            "max_hp" => adapter.Characters[id].MaximumHp,
            "buki_lv" => adapter.Characters[id].WeaponLevel,
            "buki_mon" => adapter.Characters[id].WeaponRune,
            "todome" => adapter.Characters[id].KilledEnemies,
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown character scalar field."),
        };

        foreach ((string field, string label) in new[]
        {
            ("level", "Level"), ("exp", "EXP"),
            ("now_hp", "Current HP"), ("max_hp", "Maximum HP"),
            ("buki_lv", "Weapon level"), ("buki_mon", "Weapon rune ID"),
            ("todome", "Killed enemies"),
        })
        {
            if (field == "buki_mon")
            {
                IEnumerable<Suikoden2ItemDefinition> weaponRunes = Suikoden2Catalog.Items
                    .Where(item => item.Category == Suikoden2ItemCategory.Rune && (item.Id == 0 || item.Attributes.Contains("Wep")))
                    .Where(item => !Suikoden2Catalog.Beasts.Contains(id) || item.Id == 0);
                builder.AddChoice(
                    "Weapon rune",
                    $"chara_data.c_varia_dat[{id}].{field}",
                    () =>
                    {
                        int current = ReadScalar(field);
                        return SectionText.FormatS2CatalogChoice(Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Rune && item.Id == current), Suikoden2ItemCategory.Rune, current);
                    },
                    weaponRunes.Select(SectionText.FormatS2CatalogChoice),
                    changed => adapter.SetCharacterScalar(id, field, SectionText.ParseS2Item(changed).Id),
                    "Only reviewed weapon runes are offered; beasts and monsters can select only None.");
            }
            else
            {
                builder.AddNumber(label, $"chara_data.c_varia_dat[{id}].{field}", () => ReadScalar(field), changed => adapter.SetCharacterScalar(id, field, changed));
            }
        }

        for (int index = 0; index < character.MagicPoints.Count; index++)
        {
            int captured = index;
            builder.AddNumber($"Packed MP · level {index + 1}", $"chara_data.c_varia_dat[{id}].mp[{index}]", () => adapter.Characters[id].MagicPoints[captured], value => adapter.SetMagicPoint(id, captured, value), "Verified packed range: 0–153; 17 points represent one visible MP square.");
        }

        for (int index = 0; index < character.Stats.Count; index++)
        {
            int captured = index;
            builder.AddNumber(Suikoden2Adapter.StatNames[index], $"chara_data.c_varia_dat[{id}].para[{index}]", () => adapter.Characters[id].Stats[captured], value => adapter.SetStat(id, captured, value));
        }

        for (int index = 0; index < character.Runes.Count; index++)
        {
            int captured = index;
            builder.AddChoice(
                $"Rune slot {index + 1}",
                $"chara_data.c_varia_dat[{id}].mon_eqp[{index}]",
                () =>
                {
                    int current = adapter.Characters[id].Runes[captured];
                    return SectionText.FormatS2CatalogChoice(Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Rune && item.Id == current), Suikoden2ItemCategory.Rune, current);
                },
                Suikoden2Catalog.Items
                    .Where(item => item.Category == Suikoden2ItemCategory.Rune && Suikoden2Catalog.IsRuneAllowed(id, captured, item.Id))
                    .Select(SectionText.FormatS2CatalogChoice),
                value => adapter.SetRune(id, captured, SectionText.ParseS2Item(value).Id),
                "Slot, character-exclusive, and locked-rune restrictions are enforced. A currently locked rune is shown but cannot be changed.");
        }

        for (int index = 0; index < character.Equipment.Count; index++)
        {
            int captured = index;
            Suikoden2ItemCategory category = index switch
            {
                0 => Suikoden2ItemCategory.Helmet,
                1 => Suikoden2ItemCategory.Armor,
                _ => Suikoden2ItemCategory.Shield,
            };
            builder.AddChoice(
                $"{category} slot",
                $"chara_data.c_varia_dat[{id}].bogu_eqp[{index}]",
                () =>
                {
                    int current = adapter.Characters[id].Equipment[captured];
                    return SectionText.FormatS2CatalogChoice(Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == category && item.Id == current), category, current);
                },
                Suikoden2Catalog.Items
                    .Where(item => item.Category == category && Suikoden2Catalog.IsEquipmentAllowed(id, category, item.Id))
                    .Select(SectionText.FormatS2CatalogChoice),
                value => adapter.SetEquipment(id, captured, SectionText.ParseS2Item(value).Id),
                "Equipment-type compatibility and beast/monster restrictions are enforced.");
        }

        JsonArray accessories = character.Accessories;
        for (int index = 0; index < accessories.Count; index++)
        {
            int captured = index;
            IEnumerable<Suikoden2ItemDefinition> accessoryChoices = Suikoden2Catalog.Items
                .Where(item => item.Category is Suikoden2ItemCategory.Regular or Suikoden2ItemCategory.Accessory or Suikoden2ItemCategory.Food)
                .Where(item => !item.StoryCritical)
                .Where(item => !Suikoden2Catalog.Beasts.Contains(id) || item.Id == 0);
            builder.AddChoice($"Accessory {index + 1}", $"chara_data.c_varia_dat[{id}].item_eqp[{index}]", () =>
            {
                JsonObject current = adapter.Characters[id].Accessories[captured]!.AsObject();
                return SectionText.FormatS2Item(current["item_no"]!.GetValue<int>(), current["use_cnt"]!.GetValue<int>());
            }, accessoryChoices.Select(SectionText.FormatS2CatalogChoice), value => adapter.SetAccessory(id, captured, SectionText.ParseS2Item(value)), "Only reviewed item, accessory, and food entries are offered; beast/monster restrictions are enforced.");
        }
    }
}
