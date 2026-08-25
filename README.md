# Suikoden I & II HD Remaster Save Editor

## Credits — Projects That Made This Editor Possible

- **d3xMachina — [SuikodenSaveDecrypter](https://github.com/d3xMachina/SuikodenSaveDecrypter)**: the MIT-licensed save encryption/decryption implementation used to establish direct encrypted-save compatibility.
- **d3xMachina — [Suikoden-Fix](https://github.com/d3xMachina/Suikoden-Fix)**: the MIT-licensed project that enabled and documented decrypted-save workflows, backups, and optional-mod behavior.
- **faospark — [Suikoden II HD Remaster Save Editor](https://github.com/faospark/suisaveeditor)**: the MIT-licensed source of Suikoden II mappings, game data, restrictions, and editor feature research.
- **asilverthorn — [Suikoden reference documentation](https://github.com/asilverthorn/suikoden_ref)**: Suikoden I save research and factual reference documentation. No obvious license file was present when inspected, so this project credits the research, independently verifies mappings, and does not treat it as MIT.
- Additional factual provenance credited by the projects above: **[Suikosource's Suikoden II item guide](https://suikosource.com/games/gs2/guides/itemdigits.php)**, **[makotech222's suiko2edit](https://github.com/makotech222/suiko2edit)**, and **[nesrak1's UABEA](https://github.com/nesrak1/UABEA)**.
- **Cyril — [Suikoden Guide and Walkthrough](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/80674/part-10-to-live-and-die-freely)**: factual corroboration that Suikoden I headquarters level 4 is the final development level; guide copyright remains with its author.
- **Shiro — [Suikoden Character Power-Up FAQ](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/10601)**: Suikoden I level, weapon, armor-class, and end-game equipment recommendations; no guide prose is distributed.
- **DHolmes — [Suikoden II Game Save Hacking Guide](https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/7234)** and **Feral — [Suikoden II Armor/Equipment List](https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/6620)**: stat storage, weapon cap, equipment classes, and defensive ranking research. These copyrighted guides have no software-license grant; only factual mappings are used.
- **[Gensopedia's Suikoden II equipment reference](https://gensopedia.org/w/Equipment_%28Suikoden_II%29)**: CC BY-NC-SA factual cross-checks for equipment effects and compatibility. Its prose and tables are not redistributed.
- **WiduraGoez — [Suikoden I & II HD Remaster 1.0.3 runtime-code research](https://www.nsboy.net/thread-31928-1-1.html)**: factual corroboration of remaster status and HP limits. No reuse license is claimed, and no cheat code or site prose is distributed.

Full terms, exact inspected commits, and material-use details are in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and [docs/UPSTREAM_SOURCES.md](docs/UPSTREAM_SOURCES.md).

> **Fan-project disclaimer:** This independent, nonprofit fan project is not affiliated with, authorized by, sponsored by, or endorsed by Konami or any upstream author. Suikoden and related names are trademarks of their respective owners. No proprietary game artwork, logos, music, fonts, or other extracted assets are included.

## Features

- Opens encrypted `Data1` through `Data16` files directly; no mod or separate decryptor is required.
- Detects Suikoden I or II from verified decrypted schema signatures rather than trusting the filename.
- Preserves unknown JSON properties, arrays, ordering, duplicate items, and unrelated values.
- Provides game-specific overview, party, character, inventory, recruitment, headquarters/progress, and read-only Advanced Data views.
- Includes searchable, name-first dropdowns for inventory and party editing; explained recruitment states; capped headquarters-level choices; per-field Apply and centered transactional Apply All; undo/redo; reload; recent-file paths; and slot-folder browsing.
- Provides a confirmation-gated **Max stats + best party gear** action for the six active battle characters. It uses documented caps, keeps fixed weapon identities and runes, preserves known locked gear, and applies researched/class-compatible end-game equipment recommendations as one undoable change.
- Defaults to Save As. Explicit overwrite first creates a timestamped backup, then uses validated temporary output and atomic replacement.
- Works fully offline with no telemetry and never sends save data anywhere.

## Supported games and limitations

Both games in **Suikoden I & II HD Remaster: Gate Rune and Dunan Unification Wars** are supported on encrypted PC saves.

The normal UI exposes only fields supported by inspected source code and supplied-save evidence. Unknown or insufficiently documented fields remain visible in the read-only Advanced Data view and are preserved. Experimental Suikoden II room/flag fields are clearly marked. Automated tests prove cryptographic and structural round trips, not that every possible semantic edit is accepted in game; see [manual verification](docs/MANUAL_GAME_TESTING.md).

## Download and install

Download the standalone archive for your platform from a GitHub Actions build or published GitHub release when one is available. No .NET installation is required.

- **Windows x64:** extract `SuikodenHdSaveEditor-windows-x64.zip`, then run `SuikodenHdSaveEditor.App.exe`.
- **Linux x64:** extract `SuikodenHdSaveEditor-linux-x64.tar.gz`, make `SuikodenHdSaveEditor.App` executable if needed, and run it from a desktop session.

Compare the archive against `SHA256SUMS.txt`. Keep the application and its included `LICENSE`, `LICENSES/`, and `THIRD_PARTY_NOTICES.md` together.

## Save locations

Konami documents saves under:

```text
<Steam library>/steamapps/common/Suikoden I and II HD Remaster/Save/<Steam ID>/
```

The account folder normally contains `gsd1` and `gsd2`. On Linux/Steam Deck with Proton, locate the installed game's `Save/<Steam ID>` directory through Steam. Use **Open Save Folder** to browse both game folders. See [Konami's official save-location answer](https://us-support.konami.com/hc/en-us/articles/30453763710359-Where-is-the-location-of-the-Steam-save-data).

## Backups and Steam Cloud

Close the game before editing. Make an independent backup of the entire account save folder. Save As is the default; Overwrite creates a timestamped backup and displays its path. Steam Cloud can restore an older version after a successful edit, so consider temporarily disabling synchronization and verify the local file before re-enabling it.

## Usage

1. Choose **Open Save** for one `Data*` file, or **Open Save Folder** for the slot browser.
2. Confirm the detected game, slot, original path, and any warnings.
3. Make validated edits. Use search and game-specific navigation; use Advanced Data to inspect preserved fields.
4. Choose **Save As** for a new output. Use **Overwrite with Backup** only when intentional.
5. Keep the original and backup until the edited save has passed the [in-game checklist](docs/MANUAL_GAME_TESTING.md).

## Build from source

Install the .NET 10 SDK and common Linux desktop libraries. Then run:

```bash
./scripts/restore.sh
./scripts/check-format.sh
./scripts/test.sh
./scripts/publish-linux.sh
./scripts/publish-windows.sh
./scripts/package.sh
```

Build automation and private opt-in integration tests are described in [docs/TESTING.md](docs/TESTING.md). The checked-in GitHub and Gitea workflows call the same scripts.

## Licensing

Original project code is licensed under the OSI-approved [Zero-Clause BSD license](LICENSE), SPDX `0BSD`. Substantially ported upstream work remains subject to its MIT notices. Avalonia, .NET, and distributed transitive-component notices are included under [LICENSES](LICENSES). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the complete summary.

Technical documentation: [architecture](docs/ARCHITECTURE.md), [save envelope](docs/SAVE_FORMAT.md), [Suikoden I format](docs/SUIKODEN1_FORMAT.md), [Suikoden II format](docs/SUIKODEN2_FORMAT.md), and [testing](docs/TESTING.md).
