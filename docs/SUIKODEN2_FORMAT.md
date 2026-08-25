# Suikoden II format notes

## Sources and detection

Seven supplied encrypted Suikoden II slots (`Data0` through `Data5`, plus `Data16`) were opened only from temporary copies. The normal editor uses a separate adapter and requires `game_data`, `chara_data.c_varia_dat`, `party_data.party_cha_no`, and `chara_flag`. It does not trust a `Data*` filename.

The MIT-licensed faospark editor at the pinned commit in [UPSTREAM_SOURCES.md](UPSTREAM_SOURCES.md) was reviewed beyond its README: code, schema, constants, renderers, restrictions, changelog, and game-data tables. The reviewable `Suikoden2GameData.json` is a mechanically converted, attributed form of that data.

## Normal editor coverage

- six battle-party and two convoy slots;
- battle records: level, EXP, current/maximum HP, packed MP, seven base stats, kills, weapon level/rune, three character runes, helmet/armor/shield, and three accessory/carried-item records;
- character-, slot-, locked-, exclusive-rune, beast/monster, equipment-category, and accessory restrictions;
- reviewed recruitment status values and current-party/recruited search context;
- party, warehouse, bath, room, and key-item containers; internal IDs/category encoding, stack quantity, fixed sizes, order, and duplicates;
- searchable item/reference catalogue and confirmed “give all safe items,” limited to empty party slots and non-key reviewed regular items;
- paired hero/save-list name, imported-hero, headquarters/castle, and army names; Potch, popularity, castle/bath/blacksmith levels; area/town/map and coordinates; play time and visible metadata;
- Greenhill aliases, food menu and recipe arrays, cook-off stages, Richmond clue bytes, castle/farm bytes, and treasure-chest flag bytes;
- imported Suikoden I recruit-count visibility and compatibility guidance for McDohl/Gremio, Better Leona, Valeria/Kasumi, Abizboah/Rulodia, and Kraken-related optional states.

Normal inventory and key-item choices show names without item codes. Their IDs and category encoding remain internal and are preserved in Advanced Data. Partially used regular items resolve by the reviewed ordinary-item count range rather than requiring their maximum count, and items with a reviewed maximum above one expose a quantity dropdown from 1 through that maximum. Other `use_cnt` values are category encodings—not quantities—and remain synchronized automatically. Recruitment choices explain all seven reviewed status values. Bath slots offer only the upstream-reviewed Trade-category painting or ornament ranges for their positions and keep the Trade encoding at 64. Castle level uses a raw 0–4 dropdown; event research checks for level 4 and establishes it as the cap.

The save has paired `game_data.bozu_name` and `game_data.bozu_name2` fields. They were equal in all seven supplied saves, and upstream identifies both as Suikoden II hero-name values. The normal editor therefore presents one Hero / save-list name and updates both atomically; leaving either stale can make different game displays retain the old name.

The imported recruit count is read-only because safe McDohl/Gremio state synthesis spans more than one field. Room items, clue/castle/farm flags, and chest-byte editing are marked experimental or dangerous. Optional-mod notes use explicit UI checkboxes and do not assume a mod is installed.

## Validation rules

Packed MP accepts the reviewed encoded range 0–153. Weapon level accepts 1–16. Castle level accepts 0–4. Recruitment accepts the reviewed set `0, 1, 70, 71, 86, 212, 213`. Byte arrays accept 0–255. Equipment and rune edits must match the current character, slot, lock/exclusivity, and creature restrictions from the reviewed catalogue. Containers never grow. Selecting an item applies its reviewed category/default count; only regular items with a maximum quantity above one permit a separate quantity of 1 through that maximum.

The Characters-tab party optimizer uses documented storage/gameplay caps: level 99, HP 9,999, packed MP 153 (`0x99`), base stats 255, and weapon level 16 for weapon users. It applies only to the six battle slots, not the two convoy slots. Equipment is chosen by the upstream-reviewed character classes: Wind Hat or Horned Helmet; Windspun, Silver, Dream, or Robe of Mist body armor; and Earth Shield for shield users. Native pre-edit Strength versus Magic selects Power Rings or Magic Rings. Beasts receive stats/MP only, while runes, fixed weapon identities, known locked gear/accessories, EXP, kills, and all unrelated data remain unchanged.

The gear ranking is a researched recommendation, not an official per-character build. Known fixed equipment is preserved from the credited equipment references. Automated tests enforce catalog compatibility and encrypted round trips; manual in-game validation remains required for inferred recommendations.

Unknown root, game, character, inventory-record, and array data remains in the lossless tree. The normal inventory view presents names and applicable quantities; Advanced Data retains internal IDs, category/count values, and all unrelated properties without reconstructing the save.
