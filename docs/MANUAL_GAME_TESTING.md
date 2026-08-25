# Manual in-game verification checklist

Automated tests prove encryption and structural intent, not game acceptance. Never claim a category has passed in-game until this checklist has been completed on a disposable copy with Steam Cloud controlled.

## Preparation

- [ ] Close the game and disable or pause Steam Cloud synchronization.
- [ ] Back up the entire account save folder outside the live save directory.
- [ ] Record which game, slot, and editor build/commit are under test.
- [ ] Open the encrypted file directly and confirm detected game, slot, path, and visible metadata.
- [ ] Use Save As first. Confirm the original hash is unchanged.

## Common acceptance

- [ ] The edited slot appears in the game's load menu with credible metadata.
- [ ] It loads without a hang, black screen, crash, or fallback to another slot.
- [ ] Saving again in game and reopening that new game save works.
- [ ] Names, Potch, play time display, party order, stats, HP/MP, weapon, runes, equipment, carried items, and inventory match the intended changes.
- [ ] No unrelated inventory entry, duplicate, order, character, flag, coordinate, or metadata changed.
- [ ] Recruitment and required-party transitions survive an area change and battle.
- [ ] The editor can reopen the game-written result.
- [ ] On a copied save, run **Max stats + best party gear** and confirm all six active battle characters show level 99, full 9,999 HP, maximum MP/base stats, and weapon level 16 where applicable.
- [ ] Confirm the recommended helmet/body/shield/accessory loadout is accepted for each current party member, known fixed equipment remains fixed, monsters retain empty equipment, and combat completes without stat overflow or display corruption.
- [ ] Use Undo immediately after the bulk action and confirm every affected stat and equipment slot returns to its prior value.

## Suikoden I

- [ ] Tir remains in the party and every party member has a valid battle record.
- [ ] Party and character item counts behave correctly after add, replace, and clear operations.
- [ ] Non-removable equipment remains appropriately locked in game.
- [ ] Weapon-rune pieces and equipped runes produce the expected battle behavior.
- [ ] Headquarters/recruitment edits do not block the next scripted event.

## Suikoden II

- [ ] Six battle and two convoy positions display and enter battle correctly.
- [ ] Character-specific/locked/exclusive runes and beast equipment remain valid.
- [ ] Party, warehouse, key, bath, painting/ornament, and experimental room items appear in the intended container.
- [ ] `use_cnt` decrements correctly for a consumable.
- [ ] Greenhill aliases, food/recipes, cook-off, detective clues, castle/farm values, and chest flags are checked independently.
- [ ] Optional-mod cases are tested only with their declared mod actually installed: McDohl/Gremio, Better Leona, Valeria/Kasumi, and Abizboah/Rulodia/Kraken states.

## Overwrite and recovery

- [ ] Explicit overwrite creates a timestamped backup and shows its exact path.
- [ ] The backup decrypts to the pre-edit document.
- [ ] Restoring the backup recovers the original game behavior.
- [ ] Steam Cloud is re-enabled only after deciding which version should win synchronization.

Record pass/fail evidence without committing saves, screenshots containing personal names, Steam IDs, or decrypted private JSON.
