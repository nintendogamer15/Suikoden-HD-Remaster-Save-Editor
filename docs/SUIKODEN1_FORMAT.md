# Suikoden I format notes

## Evidence and confidence

Four supplied encrypted Suikoden I slot files (`Data0`, `Data1`, `Data2`, and `Data16`) were decrypted and inspected only through temporary copies. Structural claims below were repeated across those files. Mappings were compared with the pinned asilverthorn reference documentation and Suikoden-Fix research; because `suikoden_ref` has no obvious license, its prose is not copied and no MIT status is claimed.

| Area | Evidence | Confidence / editor behavior |
|---|---|---|
| Game signature | `party_data.chara_code`, `shiro_data`, `player_base`, and `member_flag` all present | High; all four supplied slots detected, and ambiguous mixed signatures are rejected |
| Party | Six `chara_code` entries; `player_kazu` count; Tir code `8` present in valid samples and documented failure behavior | High; exactly six slots, `-1` empty, Tir required, only characters with a `player_base` record accepted |
| Party inventory | Eight fixed `party_item` entries plus `party_item_kazu` | High; ordering/duplicates retained and count updated incrementally |
| Character records | `chara_no`, level/EXP/HP, five-entry `magic_point`, six-entry `noryoku`, status, items, weapon, rune | High for exposed paths; unexplained siblings stay read-only |
| Character inventory | Nine fixed item records with `item_id`, `soubi`, and `data`; `item_kazu` is the active prefix count | High; stale nonzero records can exist after the active prefix and must not be counted or activated accidentally |
| Equipment state | Reviewed values `0`–`5` and `129`–`133`; high-bit values represent non-removable slot states in source research and samples | Medium/high; only reviewed values accepted and the danger is shown |
| Recruitment | `member_flag`, with observed/researched recruited value `9` and normal unrecruited value `0` | High for these two transitions; story consequences remain the user's responsibility |
| Headquarters | `shiro_data.level` | High for level visibility; other `shiro_data` values are preserved but not named or editable |

## Exposed normal-editor fields

- hero and headquarters names, Potch, play time display, and headquarters level display;
- party formation and synchronized party count;
- character level, EXP, current/maximum HP, all six `noryoku` values, and current MP for spell levels 1–4;
- weapon numeric ID and level, weapon rune-piece array, and equipped rune ID;
- all character-carried item IDs, equipment-slot values, and remaining-use data;
- eight party-inventory slots with searchable reviewed item names;
- recruitment flags with warnings.

The catalogue keeps character, item, equipment-state, and rune mappings outside UI code. Weapon-name mappings and maximum-stat/maximum-level synthesis were not verified well enough to expose as named bulk operations. “Max selected” and “max all” are therefore deliberately absent instead of using invented caps.

## Count preservation nuance

`item_kazu` and `party_item_kazu` describe the active prefix, not necessarily the count of every nonzero backing slot. When editing one slot, the adapter changes the count only if that slot crosses the active/inactive boundary. It does not scan and awaken stale data beyond the count. This behavior was derived from comparisons across supplied saves and is covered by regression tests.

## Known-dangerous states

Tir must remain somewhere in the six party slots. A party member must also have a battle record in the current save. The adapter blocks violations because upstream research associates them with black screens or infinite loading. Story flags, unexplained headquarters values, unknown character siblings, and unverified weapon identities remain unchanged in Advanced Data.
