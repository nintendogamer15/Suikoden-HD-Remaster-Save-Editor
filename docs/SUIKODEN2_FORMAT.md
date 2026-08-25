# Suikoden II format notes

## Sources and detection

Seven supplied encrypted Suikoden II slots (`Data0` through `Data5`, plus `Data16`) were opened only from temporary copies. The normal editor uses a separate adapter and requires `game_data`, `chara_data.c_varia_dat`, `party_data.party_cha_no`, and `chara_flag`. It does not trust a `Data*` filename.

The MIT-licensed faospark editor at the pinned commit in [UPSTREAM_SOURCES.md](UPSTREAM_SOURCES.md) was reviewed beyond its README: code, schema, constants, renderers, restrictions, changelog, and game-data tables. The reviewable `Suikoden2GameData.json` is a mechanically converted, attributed form of that data.

## Normal editor coverage

- six battle-party and two convoy slots;
- battle records: level, EXP, current/maximum HP, packed MP, seven base stats, kills, weapon level/rune, three character runes, helmet/armor/shield, and three accessory/carried-item records;
- character-, slot-, locked-, exclusive-rune, beast/monster, equipment-category, and accessory restrictions;
- reviewed recruitment status values and current-party/recruited search context;
- party, warehouse, bath, room, and key-item containers; IDs, category, `use_cnt`, fixed sizes, order, and duplicates;
- searchable item/reference catalogue and confirmed “give all safe items,” limited to empty party slots and non-key reviewed regular items;
- hero/imported-hero, headquarters/castle, and army names; Potch, popularity, castle/bath/blacksmith levels; area/town/map and coordinates; play time and visible metadata;
- Greenhill aliases, food menu and recipe arrays, cook-off stages, Richmond clue bytes, castle/farm bytes, and treasure-chest flag bytes;
- imported Suikoden I recruit-count visibility and compatibility guidance for McDohl/Gremio, Better Leona, Valeria/Kasumi, Abizboah/Rulodia, and Kraken-related optional states.

The party, inventory, equipment, rune, accessory, key-item, and recruitment controls use searchable or fixed named choices instead of unexplained numeric entry. Recruitment choices explain all seven reviewed status values. Bath slots offer only the upstream-reviewed Trade-category painting or ornament ranges for their positions and keep `use_cnt` at 64. Castle level uses a raw 0–4 dropdown; event research checks for level 4 and establishes it as the cap.

The imported recruit count is read-only because safe McDohl/Gremio state synthesis spans more than one field. Room items, clue/castle/farm flags, and chest-byte editing are marked experimental or dangerous. Optional-mod notes use explicit UI checkboxes and do not assume a mod is installed.

## Validation rules

Packed MP accepts the reviewed encoded range 0–153. Weapon level accepts 1–16. Castle level accepts 0–4. Recruitment accepts the reviewed set `0, 1, 70, 71, 86, 212, 213`. Byte arrays accept 0–255. Equipment and rune edits must match the current character, slot, lock/exclusivity, and creature restrictions from the reviewed catalogue. Containers never grow, and `use_cnt` is synchronized from the selected catalogue entry.

No undocumented scalar maximum is invented. Unknown root, game, character, inventory-record, and array data remains in the lossless tree. The data-values view presents names, IDs, categories, use counts, and warnings without reconstructing the save.
