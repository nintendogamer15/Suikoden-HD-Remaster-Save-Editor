// SPDX-License-Identifier: MIT
// Editor paths, restrictions, and constants substantially ported from faospark/suisaveeditor.
// Copyright (c) 2025 faospark. See LICENSES/suisaveeditor-MIT.txt.
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Formats.Suikoden2;

public enum Suikoden2Inventory
{
    Party,
    Warehouse,
    Bath,
    RoomExperimental,
}

public sealed record CookOffStage(int BattlesWon, int EventByte152, int EventByte153, string Opponent);

public sealed class Suikoden2Adapter
{
    public const int BattlePartySize = 6;
    public const int ConvoySize = 2;
    public const int TotalPartySize = BattlePartySize + ConvoySize;
    public const int MaximumMagicValue = 153;
    public static readonly IReadOnlyList<string> StatNames =
        ["Strength", "Magic", "Protection", "Magic Defence", "Dexterity", "Speed", "Luck"];
    public static readonly IReadOnlySet<int> RecruitmentStatuses = new HashSet<int> { 0, 1, 70, 71, 86, 212, 213 };
    public static readonly IReadOnlyList<CookOffStage> CookOffStages =
    [
        new(0, 0, 32, "No matches"),
        new(1, 1, 32, "Ky Yun"),
        new(2, 3, 32, "Goetsu"),
        new(3, 7, 32, "Shinki"),
        new(4, 15, 32, "Ryuki"),
        new(5, 31, 32, "Bashok"),
        new(6, 63, 32, "Ryuko"),
        new(7, 127, 32, "Antoio"),
        new(8, 255, 32, "Gyokuran"),
        new(9, 255, 33, "Retso"),
        new(10, 255, 35, "Lester"),
        new(11, 255, 39, "Retso rematch"),
        new(12, 255, 47, "Jinkai — complete"),
    ];

    private readonly SaveDocument document;

    public Suikoden2Adapter(SaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Guard.Valid(document.Game == GameKind.Suikoden2, "The Suikoden II adapter can only edit a detected Suikoden II save.");
        this.document = document;
    }

    public SaveDocument Document => document;

    public int Potch => Party["gold"]!.GetValue<int>();

    public int Popularity => Party["ninki"]!.GetValue<int>();

    public IReadOnlyList<int> PartyCharacterIds => Party["party_cha_no"]!.AsArray().Select(node => node!.GetValue<int>()).ToArray();

    public IReadOnlyList<Suikoden2CharacterView> Characters
    {
        get
        {
            HashSet<int> party = PartyCharacterIds.ToHashSet();
            JsonArray flags = document.Root["chara_flag"]!.AsArray();
            return CharacterData
                .Select((node, index) => new Suikoden2CharacterView(
                    index,
                    node!.AsObject(),
                    index < flags.Count ? flags[index]!.GetValue<int>() : 0,
                    party.Contains(index)))
                .ToArray();
        }
    }

    public void SetParty(IReadOnlyList<int> characterIds)
    {
        ArgumentNullException.ThrowIfNull(characterIds);
        Guard.Valid(characterIds.Count == TotalPartySize, $"Suikoden II requires six battle slots and two convoy slots ({TotalPartySize} total).");
        for (int index = 0; index < characterIds.Count; index++)
        {
            int maximum = index < BattlePartySize ? 83 : 124;
            Guard.Valid(characterIds[index] is >= 0 && characterIds[index] <= maximum, $"Party slot {index + 1} accepts character IDs 0 through {maximum}.");
        }

        Party["party_cha_no"] = new JsonArray(characterIds.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
        document.MarkChanged();
    }

    public void SetCharacterScalar(int characterId, string field, int value)
    {
        HashSet<string> allowed = ["level", "exp", "now_hp", "max_hp", "buki_lv", "buki_mon", "todome"];
        Guard.Valid(allowed.Contains(field), $"Field {field} is not a reviewed Suikoden II character scalar.");
        Guard.Valid(value >= 0, $"{field} cannot be negative.");
        JsonObject character = FindCharacter(characterId);
        if (field == "now_hp")
        {
            Guard.Valid(value <= character["max_hp"]!.GetValue<int>(), "Current HP cannot exceed maximum HP.");
        }
        else if (field == "max_hp")
        {
            Guard.Valid(value >= character["now_hp"]!.GetValue<int>(), "Maximum HP cannot be below current HP.");
        }
        else if (field == "buki_lv")
        {
            Guard.Valid(value is >= 1 and <= 16, "Weapon level must be 1 through 16.");
        }
        else if (field == "buki_mon")
        {
            Guard.Valid(!Suikoden2Catalog.Beasts.Contains(characterId) || value == 0, "Beasts and monsters cannot equip weapon runes.");
            Guard.Valid(value == 0 || Suikoden2Catalog.Items.Any(item => item.Category == Suikoden2ItemCategory.Rune && item.Id == value && item.Attributes.Contains("Wep")), "The selected value is not a reviewed weapon rune.");
        }

        character[field] = value;
        document.MarkChanged();
    }

    public void SetMagicPoint(int characterId, int spellLevelIndex, int value)
    {
        JsonArray magic = FindCharacter(characterId)["mp"]!.AsArray();
        Guard.Index(spellLevelIndex, magic.Count, "MP level");
        Guard.Valid(value is >= 0 and <= MaximumMagicValue, $"Packed MP must be 0 through {MaximumMagicValue}.");
        magic[spellLevelIndex] = value;
        document.MarkChanged();
    }

    public void SetStat(int characterId, int statIndex, int value)
    {
        JsonArray stats = FindCharacter(characterId)["para"]!.AsArray();
        Guard.Index(statIndex, stats.Count, "Stat");
        Guard.Valid(value >= 0, "A base stat cannot be negative.");
        stats[statIndex] = value;
        document.MarkChanged();
    }

    public void SetRune(int characterId, int slot, int runeId)
    {
        JsonArray runes = FindCharacter(characterId)["mon_eqp"]!.AsArray();
        Guard.Index(slot, runes.Count, "Rune slot");
        int current = runes[slot]!.GetValue<int>();
        Suikoden2ItemDefinition? currentDefinition = Suikoden2Catalog.Items.FirstOrDefault(
            item => item.Category == Suikoden2ItemCategory.Rune && item.Id == current);
        Guard.Valid(current == 0 || currentDefinition is null || !currentDefinition.Attributes.Contains("X"), "This character's current rune is locked and cannot be changed safely.");
        Guard.Valid(Suikoden2Catalog.IsRuneAllowed(characterId, slot, runeId), "That rune is not compatible with this character and slot.");
        runes[slot] = runeId;
        document.MarkChanged();
    }

    public void SetEquipment(int characterId, int slot, int itemId)
    {
        JsonArray equipment = FindCharacter(characterId)["bogu_eqp"]!.AsArray();
        Guard.Index(slot, equipment.Count, "Equipment slot");
        Suikoden2ItemCategory category = slot switch
        {
            0 => Suikoden2ItemCategory.Helmet,
            1 => Suikoden2ItemCategory.Armor,
            2 => Suikoden2ItemCategory.Shield,
            _ => throw new SaveEditorException(SaveErrorCode.ValidationFailed, "Unknown equipment slot."),
        };
        Guard.Valid(Suikoden2Catalog.IsEquipmentAllowed(characterId, category, itemId), "That equipment is not compatible with this character and slot.");
        equipment[slot] = itemId;
        document.MarkChanged();
    }

    public void SetAccessory(int characterId, int slot, Suikoden2ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Guard.Valid(!Suikoden2Catalog.Beasts.Contains(characterId) || item.Id == 0, "Beasts and monsters cannot equip accessories.");
        Guard.Valid(item.Category is Suikoden2ItemCategory.Regular or Suikoden2ItemCategory.Accessory or Suikoden2ItemCategory.Food, "Only reviewed items, accessories, and food can be placed in accessory slots.");
        Guard.Valid(!item.StoryCritical, "Story-critical key items cannot be placed in accessory slots.");
        JsonArray accessories = FindCharacter(characterId)["item_eqp"]!.AsArray();
        Guard.Index(slot, accessories.Count, "Accessory slot");
        JsonObject target = accessories[slot]!.AsObject();
        target["item_no"] = item.Id;
        target["use_cnt"] = item.Id == 0 ? 0 : item.UseCount;
        document.MarkChanged();
    }

    public void SetInventorySlot(Suikoden2Inventory inventory, int slot, Suikoden2ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        JsonArray container = GetInventory(inventory);
        Guard.Index(slot, container.Count, $"{inventory} inventory slot");
        if (inventory == Suikoden2Inventory.Bath)
        {
            Guard.Valid(item.Category == Suikoden2ItemCategory.Trade, "Bath displays accept only reviewed Trade-category paintings and ornaments.");
            bool isPaintingSlot = slot is 2 or 5;
            bool isPainting = item.Id == 0 || item.Id is >= 18 and <= 22 or >= 42 and <= 44;
            bool isOrnament = item.Id == 0 || item.Id is >= 1 and <= 17 or >= 45 and <= 50;
            Guard.Valid(isPaintingSlot ? isPainting : isOrnament, isPaintingSlot
                ? "This bath slot accepts only reviewed paintings (Trade IDs 18–22 and 42–44)."
                : "This bath slot accepts only reviewed ornaments (Trade IDs 1–17 and 45–50).");
        }

        JsonObject target = container[slot]!.AsObject();
        target["item_no"] = item.Id;
        target["use_cnt"] = inventory == Suikoden2Inventory.Bath ? 64 : item.Id == 0 ? 0 : item.UseCount;
        document.MarkChanged();
    }

    public int GiveAllSafePartyItems()
    {
        JsonArray inventory = GetInventory(Suikoden2Inventory.Party);
        Queue<Suikoden2ItemDefinition> safeItems = new(Suikoden2Catalog.Items
            .Where(item => item.Category == Suikoden2ItemCategory.Regular && item.Id != 0 && !item.StoryCritical)
            .OrderBy(item => item.Id));
        HashSet<(int Id, int UseCount)> existing = inventory
            .Select(node => node!.AsObject())
            .Where(item => item["item_no"]!.GetValue<int>() != 0)
            .Select(item => (item["item_no"]!.GetValue<int>(), item["use_cnt"]!.GetValue<int>()))
            .ToHashSet();
        int added = 0;
        foreach (JsonObject slot in inventory.Select(node => node!.AsObject()).Where(item => item["item_no"]!.GetValue<int>() == 0))
        {
            Suikoden2ItemDefinition? candidate = null;
            while (safeItems.TryDequeue(out candidate) && existing.Contains((candidate.Id, candidate.UseCount)))
            {
            }

            if (candidate is null)
            {
                break;
            }

            slot["item_no"] = candidate.Id;
            slot["use_cnt"] = candidate.UseCount;
            existing.Add((candidate.Id, candidate.UseCount));
            added++;
        }

        if (added > 0)
        {
            document.MarkChanged();
        }

        return added;
    }

    public void SetKeyItem(int slot, int itemId)
    {
        JsonArray keyItems = Party["event_item"]!.AsArray();
        Guard.Index(slot, keyItems.Count, "Key-item slot");
        Guard.Valid(itemId == 0 || Suikoden2Catalog.Items.Any(item => item.Category == Suikoden2ItemCategory.Regular && item.Id == itemId && item.StoryCritical), "Only a reviewed key-item ID can be placed in this story-critical container.");
        keyItems[slot] = itemId;
        document.MarkChanged();
    }

    public void SetRecruitmentStatus(int characterId, int status)
    {
        JsonArray flags = document.Root["chara_flag"]!.AsArray();
        Guard.Index(characterId, flags.Count, "Recruitment character");
        Guard.Valid(RecruitmentStatuses.Contains(status), $"Recruitment status {status} is not one of the reviewed states.");
        flags[characterId] = status;
        document.MarkChanged();
    }

    public void SetName(string field, string value)
    {
        HashSet<string> allowed = ["bozu_name", "bozu_name2", "macd_name", "base_name", "m_base_name", "team_name"];
        Guard.Valid(allowed.Contains(field), $"Name field {field} is not reviewed.");
        Guard.Valid(!string.IsNullOrWhiteSpace(value), "A name cannot be empty.");
        GameData[field] = value;
        document.MarkChanged();
    }

    public void SetGeneralNumber(string field, int value)
    {
        if (field is "gold" or "ninki")
        {
            Guard.Valid(value >= 0, $"{field} cannot be negative.");
            Party[field] = value;
        }
        else if (field is "px" or "py")
        {
            document.Root[field] = value;
        }
        else
        {
            HashSet<string> allowed = ["base_lv", "kaji_lv", "area_no", "s_area_no", "town_no", "s_town_no", "area_no2", "town_no2", "map_no", "s_map_no", "nakam_1_num"];
            Guard.Valid(allowed.Contains(field), $"General numeric field {field} is not reviewed.");
            Guard.Valid(field != "nakam_1_num", "Imported Suikoden I recruit count is read-only because its compatibility semantics are not safe to synthesize.");
            if (field == "base_lv")
            {
                Guard.Valid(value is >= 0 and <= 4, "Castle level must be 0 through 4; level 4 is the cap.");
            }
            else if (field == "kaji_lv")
            {
                Guard.Valid(value >= 0, $"{field} cannot be negative.");
            }

            GameData[field] = value;
        }

        document.MarkChanged();
    }

    public void SetGameDataArrayValue(string field, int index, int value)
    {
        HashSet<string> allowed = ["play_time", "food_menu", "food_resipi", "food_num", "furo_info", "tantei_lv", "hon_flag"];
        Guard.Valid(allowed.Contains(field), $"Array {field} is not a reviewed game_data editor field.");
        JsonArray array = GameData[field]!.AsArray();
        Guard.Index(index, array.Count, field);
        Guard.Valid(field is not ("tantei_lv" or "hon_flag") || value is >= 0 and <= 255, $"{field} is a byte array and accepts only 0 through 255.");
        Guard.Valid(value >= 0, $"{field} values cannot be negative.");
        array[index] = value;
        document.MarkChanged();
    }

    public void SetGreenhillAlias(int index, string value)
    {
        JsonArray aliases = GameData["kari_name"]!.AsArray();
        Guard.Index(index, aliases.Count, "Greenhill alias");
        Guard.Valid(!string.IsNullOrWhiteSpace(value), "A Greenhill alias cannot be empty.");
        aliases[index] = value;
        document.MarkChanged();
    }

    public void SetTreasureFlagByte(int index, int value)
    {
        JsonArray flags = document.Root["t_box_flag"]!.AsArray();
        Guard.Index(index, flags.Count, "Treasure-chest flag");
        Guard.Valid(value is >= 0 and <= 255, "Treasure-chest flag bytes accept only 0 through 255.");
        flags[index] = value;
        document.MarkChanged();
    }

    public void SetRootByteArrayValue(string field, int index, int value)
    {
        HashSet<string> allowed = ["event_flag", "town_flag", "scpoi_flag", "t_box_flag"];
        Guard.Valid(allowed.Contains(field), $"Root byte array {field} is not a reviewed flag container.");
        JsonArray flags = document.Root[field]!.AsArray();
        Guard.Index(index, flags.Count, field);
        Guard.Valid(value is >= 0 and <= 255, $"{field} is a byte array and accepts only 0 through 255.");
        flags[index] = value;
        document.MarkChanged();
    }

    public void SetCookOffStage(int battlesWon)
    {
        CookOffStage? stage = CookOffStages.FirstOrDefault(value => value.BattlesWon == battlesWon);
        Guard.Valid(stage is not null, "Cook-off progress must be a reviewed stage from 0 through 12 battles won.");
        JsonArray flags = document.Root["event_flag"]!.AsArray();
        Guard.Valid(flags.Count > 153, "The event_flag array is too short for reviewed cook-off progress bytes 152 and 153.");
        flags[152] = stage!.EventByte152;
        flags[153] = stage.EventByte153;
        document.MarkChanged();
    }

    public IReadOnlyList<string> CompatibilityNotes(bool betterLeonaEnabled, bool krakenPatchEnabled)
    {
        List<string> notes = [];
        JsonArray flags = document.Root["chara_flag"]!.AsArray();
        string importedHero = GameData["macd_name"]?.GetValue<string>() ?? string.Empty;
        if (importedHero.Length > 0)
        {
            string gremioStatus = flags.Count > 125 ? flags[125]!.GetValue<int>().ToString(System.Globalization.CultureInfo.InvariantCulture) : "unavailable";
            notes.Add($"An imported Suikoden I hero name is present. McDohl (82) status is {flags[82]!.GetValue<int>()}; Gremio (125) status is {gremioStatus}. The import can make them available without rewriting either status.");
        }

        if (betterLeonaEnabled)
        {
            bool valeria = IsNormallyRecruited(flags[12]!.GetValue<int>());
            bool kasumi = IsNormallyRecruited(flags[73]!.GetValue<int>());
            string effective = valeria == kasumi ? "their stored states agree" : valeria ? "Kasumi is treated as recruited through Valeria" : "Valeria is treated as recruited through Kasumi";
            notes.Add($"Better Leona mode: {effective}. Stored Valeria (12) and Kasumi (73) flags remain unchanged.");
        }

        if (krakenPatchEnabled)
        {
            bool chuchara = IsNormallyRecruited(flags[79]!.GetValue<int>());
            notes.Add(chuchara
                ? "Kraken recruitment mode: recruited Chuchara (79) makes Abizboah (49) and Rulodia (74) effective recruits without rewriting their stored flags."
                : "Kraken recruitment mode: Chuchara (79) is not normally recruited, so no effective Abizboah/Rulodia recruitment is inferred.");
        }

        return notes;
    }

    private static bool IsNormallyRecruited(int status) => status is 70 or 71;

    public IReadOnlyList<ValidationIssue> Validate()
    {
        List<ValidationIssue> issues = [];
        CheckLength(issues, Party["party_cha_no"]!.AsArray(), TotalPartySize, "party_data.party_cha_no");
        CheckLength(issues, Party["party_item"]!.AsArray(), 30, "party_data.party_item");
        CheckLength(issues, Party["event_item"]!.AsArray(), 10, "party_data.event_item");
        CheckLength(issues, GameData["base_item"]!.AsArray(), 60, "game_data.base_item");
        CheckLength(issues, GameData["furo_item"]!.AsArray(), 8, "game_data.furo_item");
        CheckLength(issues, GameData["room_item"]!.AsArray(), 8, "game_data.room_item");
        CheckLength(issues, CharacterData, 85, "chara_data.c_varia_dat");
        int castleLevel = GameData["base_lv"]!.GetValue<int>();
        if (castleLevel is < 0 or > 4)
        {
            issues.Add(new(ValidationSeverity.Error, "game_data.base_lv", "Castle level must be 0 through 4; level 4 is the cap."));
        }

        for (int index = 0; index < CharacterData.Count; index++)
        {
            JsonObject character = CharacterData[index]!.AsObject();
            if (character["now_hp"]!.GetValue<int>() > character["max_hp"]!.GetValue<int>())
            {
                issues.Add(new(ValidationSeverity.Error, $"chara_data.c_varia_dat[{index}].now_hp", "Current HP exceeds maximum HP."));
            }

            if (Suikoden2Catalog.Beasts.Contains(index)
                && (character["buki_mon"]!.GetValue<int>() != 0
                    || character["bogu_eqp"]!.AsArray().Any(node => node!.GetValue<int>() != 0)))
            {
                issues.Add(new(ValidationSeverity.Error, $"chara_data.c_varia_dat[{index}]", "A beast/monster has weapon rune or armor data that the reviewed editor restrictions do not permit."));
            }
        }

        issues.Add(new(ValidationSeverity.Warning, "game_data.room_item", "Room items are experimental and their in-game purpose remains uncertain."));
        issues.Add(new(ValidationSeverity.Warning, "event_flag/hon_flag/t_box_flag", "Story, castle/farm, cook-off, detective, and treasure flags can alter progression. Review each byte before saving."));
        return issues;
    }

    private JsonObject GameData => document.Root["game_data"]!.AsObject();

    private JsonObject Party => document.Root["party_data"]!.AsObject();

    private JsonArray CharacterData => document.Root["chara_data"]!["c_varia_dat"]!.AsArray();

    private JsonObject FindCharacter(int characterId)
    {
        Guard.Index(characterId, CharacterData.Count, "Battle character");
        return CharacterData[characterId]!.AsObject();
    }

    private JsonArray GetInventory(Suikoden2Inventory inventory) => inventory switch
    {
        Suikoden2Inventory.Party => Party["party_item"]!.AsArray(),
        Suikoden2Inventory.Warehouse => GameData["base_item"]!.AsArray(),
        Suikoden2Inventory.Bath => GameData["furo_item"]!.AsArray(),
        Suikoden2Inventory.RoomExperimental => GameData["room_item"]!.AsArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(inventory)),
    };

    private static void CheckLength(List<ValidationIssue> issues, JsonArray array, int expected, string path)
    {
        if (array.Count != expected)
        {
            issues.Add(new(ValidationSeverity.Error, path, $"Expected {expected} fixed slots; found {array.Count}."));
        }
    }
}
