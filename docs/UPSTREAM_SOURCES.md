# Upstream sources

Inspected on 2026-08-25. Research clones live only in ignored `reference/`; no clone is part of this repository or its releases.

| Project | Inspected commit | License/status | Material use |
|---|---|---|---|
| [d3xMachina/SuikodenSaveDecrypter](https://github.com/d3xMachina/SuikodenSaveDecrypter) | `1757dfaf6bb78c848eafb67fa585d51d46fa586e` | MIT; copyright d3xMachina | Cryptographic file format and compatibility oracle. The clean-room project implementation retains an upstream attribution/SPDX notice. |
| [d3xMachina/Suikoden-Fix](https://github.com/d3xMachina/Suikoden-Fix) | `24fdc3eed7cd934a1482a785f206f6168c47877a` | MIT; copyright d3xMachina | Decrypted-save workflow, backup behavior, and optional-mod behavior research. |
| [faospark/suisaveeditor](https://github.com/faospark/suisaveeditor) | `efcf89e89ccc446c4a7e79919ac9d2f7e405d2a0` | MIT; copyright faospark | Suikoden II schema paths, display mappings, item/rune/equipment data, restrictions, editor behavior, README, changelog, and feature research. Ported mappings retain attribution. |
| [asilverthorn/suikoden_ref](https://github.com/asilverthorn/suikoden_ref) | `9a216b93a7bc3d9c062700e4e12b18f9e36cbdf6` | No obvious license file at inspection time | Credited research documentation for Suikoden I and factual Suikoden II mappings. No prose or code is redistributed under an invented license; exposed fields are independently checked against supplied saves. |
| [nintendogamer15/ffix-save-editor](https://github.com/nintendogamer15/ffix-save-editor) | `5fd8afc4474a956f79cad63a11d135dd3d19d730` | MIT; copyright 2026 ffix-save-editor contributors | Primary reference for native Gitea release creation, immutable package uploads, Arch/RPM construction, installed-package validation, and public repository instructions. Substantially adapted helper scripts retain its MIT notice. |
| [nintendogamer15/Final-Fantasy-IV-3D-Remake-Save-Editor](https://github.com/nintendogamer15/Final-Fantasy-IV-3D-Remake-Save-Editor) | `54a0abe811b569dac5394847d6fa0c25ed3e8dc3` | LGPL-3.0-or-later | Corroborating reference for the isolated Gitea Docker-in-Docker workflow and native package conventions. No project-specific or LGPL-covered application source was copied. |

## Additional sources credited through upstream work

- [Suikosource Suikoden II item digits guide](https://suikosource.com/games/gs2/guides/itemdigits.php), credited by faospark for item-reference research. Facts are cross-checked against the MIT-licensed editor data and supplied save schemas; site prose is not copied.
- [makotech222/suiko2edit](https://github.com/makotech222/suiko2edit), credited by faospark for Suikoden II data research. No code was copied directly during this phase.
- [nesrak1/UABEA](https://github.com/nesrak1/UABEA), named by asilverthorn as the extraction tool used during reference research. It is credited for provenance but is not a dependency and no code is copied.
- [Cyril's Suikoden Guide and Walkthrough](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/80674/part-10-to-live-and-die-freely), used to corroborate that Suikoden I headquarters level 4 is its final development. Only that fact was used; guide prose is not redistributed.
- [Shiro's Suikoden Character Power-Up FAQ](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/10601), used for factual Suikoden I level/weapon caps and per-character end-game equipment recommendations. It has no software-license grant; no prose is copied.
- [DHolmes's Suikoden II Game Save Hacking Guide](https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/7234), used to corroborate one-byte base-stat storage, level/HP field widths, MP encoding, and the useful weapon-level-16 cap. It has no software-license grant; no prose or code is copied.
- [Feral's Suikoden II Armor/Equipment List](https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/6620), used to compare armor classes, defensive values, side effects, and known fixed equipment against the upstream editor data. It has no software-license grant; no prose or table is copied.
- [Gensopedia's Suikoden II equipment reference](https://gensopedia.org/w/Equipment_%28Suikoden_II%29), available under CC BY-NC-SA unless otherwise noted, used only as a factual cross-check for equipment effects and compatibility. Its prose and tables are not redistributed.
- WiduraGoez's Suikoden I & II HD Remaster 1.0.3 runtime-code research, [archived at NSboy](https://www.nsboy.net/thread-31928-1-1.html), was used only to corroborate the remaster's 255 stored-stat and 9,999 HP behavior. No cheat code or site prose is distributed, and no reuse license is claimed.

## Evidence policy

Factual schema signatures and array lengths were checked across every supplied encrypted `gsd1` and `gsd2` save after decrypting temporary copies with the upstream oracle. Player-entered strings and unrelated private values are not recorded. Undocumented fields remain preserved and read-only unless their meaning and edit constraints can be supported by source code or repeatable evidence.
