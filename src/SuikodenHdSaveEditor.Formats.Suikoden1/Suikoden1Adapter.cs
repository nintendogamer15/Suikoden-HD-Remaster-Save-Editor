// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Formats.Suikoden1;

public sealed class Suikoden1Adapter
{
    public const int ProtagonistId = 8;
    public const int PartySize = 6;
    public const int PartyInventorySize = 8;
    public const int CharacterInventorySize = 9;
    public const int MaximumCharacterLevel = 99;
    public const int MaximumCharacterHp = 9999;
    public const int MaximumCharacterStat = 255;
    public const int MaximumWeaponLevel = 16;
    public static readonly IReadOnlyList<string> StatNames = ["Strength", "Dexterity", "Protection", "Speed", "Magic", "Luck"];

    private static readonly HashSet<int> EquipmentSlotValues = [0, 1, 2, 3, 4, 5, 129, 130, 131, 132, 133];
    private static readonly HashSet<int> HornedHelmetUsers = [1, 5, 10, 12, 14, 16, 19, 21, 25, 29, 36, 37, 41, 42, 47, 48, 49, 63, 65, 67, 74, 80, 82, 107];
    private static readonly HashSet<int> WindspunArmorUsers = [12, 21, 25, 40, 73, 74];
    private static readonly HashSet<int> TaikyokuTunicUsers = [6, 11, 13, 15, 18, 22, 23, 31, 32, 38, 39, 52, 54, 55, 65, 66, 67, 68, 91, 95, 101, 102];
    private static readonly HashSet<int> MasterRobeUsers = [0, 9, 33, 59, 97];
    private static readonly HashSet<int> EarthShieldUsers = [7, 10, 14, 17, 19, 25, 28, 29, 30, 36, 42, 47, 48, 49, 63, 74, 80, 82, 101, 102, 107];
    private readonly SaveDocument document;

    public Suikoden1Adapter(SaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Guard.Valid(document.Game == GameKind.Suikoden1, "The Suikoden I adapter can only edit a detected Suikoden I save.");
        this.document = document;
    }

    public SaveDocument Document => document;

    public int Potch => Party["mochi_kin"]!.GetValue<int>();

    public int HeadquartersLevel => Headquarters["level"]!.GetValue<int>();

    public string HeroName => document.Root["playerName"]?.GetValue<string>() ?? string.Empty;

    public string HeadquartersName => document.Root["playerCName"]?.GetValue<string>() ?? string.Empty;

    public int PlayTime => document.Root["playTime"]?.GetValue<int>() ?? 0;

    public IReadOnlyList<int> PartyCharacterIds => Party["chara_code"]!.AsArray().Select(node => node!.GetValue<int>()).ToArray();

    public IReadOnlyList<Suikoden1CharacterView> Characters => Players.Select(node => new Suikoden1CharacterView(node!.AsObject())).ToArray();

    public IReadOnlyList<int> RecruitedCharacterIds => MemberFlags
        .Select((node, index) => (node, index))
        .Where(value => value.node?.GetValue<int>() == 9)
        .Select(value => value.index)
        .ToArray();

    public void SetPotch(int value)
    {
        Guard.Valid(value >= 0, "Potch cannot be negative.");
        Party["mochi_kin"] = value;
        document.MarkChanged();
    }

    public void SetHeadquartersLevel(int value)
    {
        Guard.Valid(value is >= 0 and <= 4, "Headquarters level must be 0 through 4; level 4 is the cap.");
        Headquarters["level"] = value;
        document.MarkChanged();
    }

    public void SetNames(string heroName, string headquartersName)
    {
        Guard.Valid(!string.IsNullOrWhiteSpace(heroName), "The hero name cannot be empty.");
        Guard.Valid(!string.IsNullOrWhiteSpace(headquartersName), "The headquarters name cannot be empty.");
        document.Root["playerName"] = heroName;
        document.Root["playerCName"] = headquartersName;
        document.MarkChanged();
    }

    public void SetParty(IReadOnlyList<int> characterIds)
    {
        ArgumentNullException.ThrowIfNull(characterIds);
        Guard.Valid(characterIds.Count == PartySize, $"Suikoden I requires exactly {PartySize} party slots.");
        Guard.Valid(characterIds.Contains(ProtagonistId), "Tir (character 8) must remain in the party or the game can load to a black screen.");

        HashSet<int> battleCharacters = Players.Select(node => node!["chara_no"]!.GetValue<int>()).ToHashSet();
        foreach (int characterId in characterIds)
        {
            if (characterId == -1)
            {
                continue;
            }

            Guard.Valid(
                battleCharacters.Contains(characterId),
                $"Character {characterId} has no player_base battle record in this save and cannot safely be placed in the party.");
        }

        Party["chara_code"] = new JsonArray(characterIds.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
        Party["player_kazu"] = characterIds.Count(value => value >= 0);
        document.MarkChanged();
    }

    public void SetPartyItem(int slot, int itemId)
    {
        JsonArray items = Party["party_item"]!.AsArray();
        Guard.Index(slot, items.Count, "Party item slot");
        Guard.Valid(itemId == 0 || Suikoden1Catalog.Items.ContainsKey(itemId), $"Item ID {itemId} is not in the reviewed Suikoden I item table.");
        int oldCount = Party["party_item_kazu"]!.GetValue<int>();
        items[slot] = itemId;
        Party["party_item_kazu"] = UpdatedActiveCount(items, oldCount, slot, itemId != 0, node => node!.GetValue<int>() != 0);
        document.MarkChanged();
    }

    public void SetCharacterScalar(int characterId, string field, int value)
    {
        HashSet<string> allowed = ["level", "exp", "hp", "max_hp", "status"];
        Guard.Valid(allowed.Contains(field), $"Field {field} is not a reviewed scalar character field.");
        Guard.Valid(value >= 0, $"{field} cannot be negative.");
        Guard.Valid(field != "level" || value <= MaximumCharacterLevel, $"Level cannot exceed {MaximumCharacterLevel}.");
        Guard.Valid(field is not ("hp" or "max_hp") || value <= MaximumCharacterHp, $"HP cannot exceed {MaximumCharacterHp}.");
        JsonObject player = FindPlayer(characterId);
        if (field == "hp")
        {
            Guard.Valid(value <= player["max_hp"]!.GetValue<int>(), "Current HP cannot exceed maximum HP.");
        }
        else if (field == "max_hp")
        {
            Guard.Valid(value >= player["hp"]!.GetValue<int>(), "Maximum HP cannot be below current HP.");
        }

        player[field] = value;
        document.MarkChanged();
    }

    public void SetCharacterStat(int characterId, int statIndex, int value)
    {
        JsonArray stats = FindPlayer(characterId)["noryoku"]!.AsArray();
        Guard.Index(statIndex, stats.Count, "Stat");
        Guard.Valid(value is >= 0 and <= MaximumCharacterStat, $"A base stat must be 0 through {MaximumCharacterStat}.");
        stats[statIndex] = value;
        document.MarkChanged();
    }

    public void SetMagicPoint(int characterId, int spellLevel, int currentPoints)
    {
        JsonArray magic = FindPlayer(characterId)["magic_point"]!.AsArray();
        Guard.Valid(spellLevel is >= 1 and <= 4, "Spell level must be 1 through 4.");
        Guard.Index(spellLevel, magic.Count, "Magic point");
        Guard.Valid(currentPoints is >= 0 and <= 9, "Current MP for a spell level must be 0 through 9.");
        magic[spellLevel] = currentPoints;
        document.MarkChanged();
    }

    public void SetWeapon(int characterId, int weaponId, int weaponLevel)
    {
        Guard.Valid(weaponId >= 0, "Weapon ID cannot be negative.");
        Guard.Valid(weaponLevel is >= 0 and <= MaximumWeaponLevel, $"Weapon level must be 0 through {MaximumWeaponLevel}.");
        JsonObject weapon = FindPlayer(characterId)["buki_data"]!.AsObject();
        weapon["buki_id"] = weaponId;
        weapon["level"] = weaponLevel;
        document.MarkChanged();
    }

    public void SetWeaponRunePiece(int characterId, int index, int value)
    {
        JsonArray pieces = FindPlayer(characterId)["buki_data"]!["monsyo"]!.AsArray();
        Guard.Index(index, pieces.Count, "Weapon rune-piece");
        Guard.Valid(value >= 0, "A weapon rune-piece value cannot be negative.");
        pieces[index] = value;
        document.MarkChanged();
    }

    public void SetCharacterRune(int characterId, int runeId)
    {
        Guard.Valid(Suikoden1Catalog.Runes.ContainsKey(runeId), $"Rune ID {runeId} is not in the reviewed Suikoden I rune table.");
        FindPlayer(characterId)["monsyo_data"]!["monsyo_id"] = runeId;
        document.MarkChanged();
    }

    public void SetCharacterItem(int characterId, int slot, int itemId, int equipmentSlot, int remainingUses)
    {
        JsonObject player = FindPlayer(characterId);
        JsonArray items = player["item"]!.AsArray();
        Guard.Index(slot, items.Count, "Character item slot");
        Guard.Valid(itemId == 0 || Suikoden1Catalog.Items.ContainsKey(itemId), $"Item ID {itemId} is not in the reviewed Suikoden I item table.");
        Guard.Valid(EquipmentSlotValues.Contains(equipmentSlot), $"Equipment-slot value {equipmentSlot} is not reviewed.");
        Guard.Valid(remainingUses >= 0, "Remaining item uses cannot be negative.");

        JsonObject item = items[slot]!.AsObject();
        int oldCount = player["item_kazu"]!.GetValue<int>();
        item["item_id"] = itemId;
        item["soubi"] = itemId == 0 ? 0 : equipmentSlot;
        item["data"] = itemId == 0 ? 0 : remainingUses;
        player["item_kazu"] = UpdatedActiveCount(items, oldCount, slot, itemId != 0, node => node!["item_id"]!.GetValue<int>() != 0);
        document.MarkChanged();
    }

    public void SetRecruited(int characterId, bool recruited)
    {
        Guard.Index(characterId, MemberFlags.Count, "Recruitment character");
        MemberFlags[characterId] = recruited ? 9 : 0;
        document.MarkChanged();
    }

    public PartyOptimizationResult MaximizeAndEquipParty()
    {
        int charactersUpdated = 0;
        int equipmentUpdated = 0;
        int preserved = 0;
        foreach (int characterId in PartyCharacterIds.Where(id => id >= 0).Distinct())
        {
            JsonObject player = FindPlayer(characterId);
            player["level"] = MaximumCharacterLevel;
            player["max_hp"] = MaximumCharacterHp;
            player["hp"] = MaximumCharacterHp;

            JsonArray magic = player["magic_point"]!.AsArray();
            for (int index = 1; index < Math.Min(magic.Count, 5); index++)
            {
                magic[index] = 9;
            }

            JsonArray stats = player["noryoku"]!.AsArray();
            for (int index = 0; index < stats.Count; index++)
            {
                stats[index] = MaximumCharacterStat;
            }

            player["buki_data"]!["level"] = MaximumWeaponLevel;
            (int changed, int skipped) = ApplyRecommendedEquipment(player, RecommendedEquipmentFor(characterId));
            equipmentUpdated += changed;
            preserved += skipped;
            charactersUpdated++;
        }

        Guard.Valid(charactersUpdated > 0, "The active party has no battle characters to optimize.");
        document.MarkChanged();
        return new(charactersUpdated, equipmentUpdated, preserved);
    }

    public IReadOnlyList<ValidationIssue> Validate()
    {
        List<ValidationIssue> issues = [];
        JsonArray partyCodes = Party["chara_code"]!.AsArray();
        if (partyCodes.Count != PartySize)
        {
            issues.Add(new(ValidationSeverity.Error, "party_data.chara_code", $"Expected {PartySize} party slots; found {partyCodes.Count}."));
        }

        if (!partyCodes.Any(node => node?.GetValue<int>() == ProtagonistId))
        {
            issues.Add(new(ValidationSeverity.Error, "party_data.chara_code", "Tir (character 8) is required; removing him is known to cause a black screen."));
        }

        int headquartersLevel = HeadquartersLevel;
        if (headquartersLevel is < 0 or > 4)
        {
            issues.Add(new(ValidationSeverity.Error, "shiro_data.level", "Headquarters level must be 0 through 4; level 4 is the cap."));
        }

        HashSet<int> battleCharacters = Players.Select(node => node!["chara_no"]!.GetValue<int>()).ToHashSet();
        foreach (int id in partyCodes.Select(node => node!.GetValue<int>()))
        {
            if (id != -1 && !battleCharacters.Contains(id))
            {
                issues.Add(new(ValidationSeverity.Error, "party_data.chara_code", $"Character {id} has no player_base battle record and can cause infinite loading."));
            }
        }

        ValidateCount(issues, Party, "party_item", "party_item_kazu", "party_data");
        foreach (JsonNode? node in Players)
        {
            JsonObject player = node!.AsObject();
            int id = player["chara_no"]!.GetValue<int>();
            if (player["item"] is JsonArray items && items.Count != CharacterInventorySize)
            {
                issues.Add(new(ValidationSeverity.Error, $"player_base[{id}].item", $"Expected {CharacterInventorySize} fixed item slots; found {items.Count}."));
            }

            ValidateCount(issues, player, "item", "item_kazu", $"player_base[{id}]");
            int hp = player["hp"]!.GetValue<int>();
            int maximumHp = player["max_hp"]!.GetValue<int>();
            if (hp < 0 || maximumHp < hp)
            {
                issues.Add(new(ValidationSeverity.Error, $"player_base[{id}].hp", "Current HP must be non-negative and no greater than maximum HP."));
            }
        }

        issues.Add(new(
            ValidationSeverity.Warning,
            "shiro_data",
            "Most headquarters and story fields remain undocumented and are preserved read-only in Advanced Data."));
        return issues;
    }

    private JsonObject Party => document.Root["party_data"]!.AsObject();

    private JsonObject Headquarters => document.Root["shiro_data"]!.AsObject();

    private JsonArray Players => document.Root["player_base"]!.AsArray();

    private JsonArray MemberFlags => document.Root["member_flag"]!.AsArray();

    private JsonObject FindPlayer(int characterId)
    {
        JsonObject? player = Players
            .Select(node => node!.AsObject())
            .FirstOrDefault(value => value["chara_no"]!.GetValue<int>() == characterId);
        if (player is null)
        {
            throw new SaveEditorException(SaveErrorCode.ValidationFailed, $"Character {characterId} has no player_base record in this save.");
        }

        return player;
    }

    private static (int Changed, int Preserved) ApplyRecommendedEquipment(JsonObject player, RecommendedEquipment equipment)
    {
        JsonArray items = player["item"]!.AsArray();
        int activeCount = Math.Clamp(player["item_kazu"]!.GetValue<int>(), 0, items.Count);
        int changed = 0;
        int preserved = 0;
        int[] recommendations = [equipment.Helmet, equipment.Armor, equipment.Shield, equipment.Other1, equipment.Other2];
        for (int equipmentSlot = 1; equipmentSlot <= recommendations.Length; equipmentSlot++)
        {
            int recommendedItem = recommendations[equipmentSlot - 1];
            int existingIndex = Enumerable.Range(0, activeCount).FirstOrDefault(
                index => EquipmentBaseSlot(items[index]!["soubi"]!.GetValue<int>()) == equipmentSlot,
                -1);
            if (existingIndex >= 0)
            {
                JsonObject existing = items[existingIndex]!.AsObject();
                int state = existing["soubi"]!.GetValue<int>();
                if (state >= 129)
                {
                    preserved++;
                    continue;
                }

                if (recommendedItem == 0)
                {
                    existing["soubi"] = 0;
                }
                else
                {
                    existing["item_id"] = recommendedItem;
                    existing["soubi"] = equipmentSlot;
                    existing["data"] = 0;
                }

                changed++;
                continue;
            }

            if (recommendedItem == 0)
            {
                continue;
            }

            int destination = Enumerable.Range(0, activeCount).FirstOrDefault(
                index => items[index]!["item_id"]!.GetValue<int>() == 0,
                -1);
            if (destination < 0 && activeCount < items.Count && items[activeCount]!["item_id"]!.GetValue<int>() == 0)
            {
                destination = activeCount++;
            }

            if (destination < 0)
            {
                preserved++;
                continue;
            }

            JsonObject target = items[destination]!.AsObject();
            target["item_id"] = recommendedItem;
            target["soubi"] = equipmentSlot;
            target["data"] = 0;
            changed++;
        }

        player["item_kazu"] = activeCount;
        return (changed, preserved);
    }

    private static int EquipmentBaseSlot(int state) => state is >= 129 and <= 133 ? state - 128 : state;

    // Item IDs and the per-character groupings are factual recommendations from Shiro's
    // Suikoden Character Power-Up FAQ. A few absent fighters use conservative armor-class
    // inferences; see docs/SUIKODEN1_FORMAT.md and THIRD_PARTY_NOTICES.md.
    private static RecommendedEquipment RecommendedEquipmentFor(int characterId)
    {
        int helmet = HornedHelmetUsers.Contains(characterId) ? 8 : 7;
        int armor = characterId switch
        {
            26 or 76 => 13, // Guard Robe
            61 => 17,       // Magic Robe
            107 => 19,      // Dragon Armor
            _ when WindspunArmorUsers.Contains(characterId) => 24,
            _ when TaikyokuTunicUsers.Contains(characterId) => 22,
            _ when MasterRobeUsers.Contains(characterId) => 20,
            _ => 23,        // Master's Garb
        };
        int shield = EarthShieldUsers.Contains(characterId) ? 70 : 0;
        (int other1, int other2) = characterId switch
        {
            0 => (49, 41),       // Star Earrings, Crimson Cape
            14 or 80 => (56, 56), // Gold Collars for kobolds
            26 => (52, 41),      // Speed Ring, Crimson Cape
            28 => (50, 41),      // Rose Brooch, Crimson Cape
            61 => (29, 41),      // Toe Shoes, Crimson Cape
            64 => (48, 41),      // Emblem, Crimson Cape
            75 => (30, 41),      // Winged Boots, Crimson Cape
            _ => (41, 41),       // Crimson Capes
        };
        return new(helmet, armor, shield, other1, other2);
    }

    private sealed record RecommendedEquipment(int Helmet, int Armor, int Shield, int Other1, int Other2);

    private static void ValidateCount(List<ValidationIssue> issues, JsonObject owner, string arrayName, string countName, string path)
    {
        JsonArray array = owner[arrayName]!.AsArray();
        int actual = owner[countName]!.GetValue<int>();
        if (actual < 0 || actual > array.Count)
        {
            issues.Add(new(
                ValidationSeverity.Error,
                $"{path}.{countName}",
                $"The active item count {actual} is outside the fixed array length {array.Count}."));
        }
    }

    private static int UpdatedActiveCount(
        JsonArray array,
        int oldCount,
        int editedSlot,
        bool nowActive,
        Func<JsonNode?, bool> isActive)
    {
        int count = Math.Clamp(oldCount, 0, array.Count);
        if (nowActive && editedSlot >= count)
        {
            return editedSlot + 1;
        }

        if (!nowActive && editedSlot == count - 1)
        {
            while (count > 0 && !isActive(array[count - 1]))
            {
                count--;
            }
        }

        return count;
    }
}
