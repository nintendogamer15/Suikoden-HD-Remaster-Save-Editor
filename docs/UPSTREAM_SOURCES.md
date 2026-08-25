# Upstream sources

Inspected on 2026-08-25. Research clones live only in ignored `reference/`; no clone is part of this repository or its releases.

| Project | Inspected commit | License/status | Material use |
|---|---|---|---|
| [d3xMachina/SuikodenSaveDecrypter](https://github.com/d3xMachina/SuikodenSaveDecrypter) | `1757dfaf6bb78c848eafb67fa585d51d46fa586e` | MIT; copyright d3xMachina | Cryptographic file format and compatibility oracle. The clean-room project implementation retains an upstream attribution/SPDX notice. |
| [d3xMachina/Suikoden-Fix](https://github.com/d3xMachina/Suikoden-Fix) | `24fdc3eed7cd934a1482a785f206f6168c47877a` | MIT; copyright d3xMachina | Decrypted-save workflow, backup behavior, and optional-mod behavior research. |
| [faospark/suisaveeditor](https://github.com/faospark/suisaveeditor) | `efcf89e89ccc446c4a7e79919ac9d2f7e405d2a0` | MIT; copyright faospark | Suikoden II schema paths, display mappings, item/rune/equipment data, restrictions, editor behavior, README, changelog, and feature research. Ported mappings retain attribution. |
| [asilverthorn/suikoden_ref](https://github.com/asilverthorn/suikoden_ref) | `9a216b93a7bc3d9c062700e4e12b18f9e36cbdf6` | No obvious license file at inspection time | Credited research documentation for Suikoden I and factual Suikoden II mappings. No prose or code is redistributed under an invented license; exposed fields are independently checked against supplied saves. |

## Additional sources credited through upstream work

- [Suikosource Suikoden II item digits guide](https://suikosource.com/games/gs2/guides/itemdigits.php), credited by faospark for item-reference research. Facts are cross-checked against the MIT-licensed editor data and supplied save schemas; site prose is not copied.
- [makotech222/suiko2edit](https://github.com/makotech222/suiko2edit), credited by faospark for Suikoden II data research. No code was copied directly during this phase.
- [nesrak1/UABEA](https://github.com/nesrak1/UABEA), named by asilverthorn as the extraction tool used during reference research. It is credited for provenance but is not a dependency and no code is copied.
- [Cyril's Suikoden Guide and Walkthrough](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/80674/part-10-to-live-and-die-freely), used to corroborate that Suikoden I headquarters level 4 is its final development. Only that fact was used; guide prose is not redistributed.

## Evidence policy

Factual schema signatures and array lengths were checked across every supplied encrypted `gsd1` and `gsd2` save after decrypting temporary copies with the upstream oracle. Player-entered strings and unrelated private values are not recorded. Undocumented fields remain preserved and read-only unless their meaning and edit constraints can be supported by source code or repeatable evidence.
