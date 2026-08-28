# Third-party notices

This fan project is independent. It is not affiliated with, authorized by, sponsored by, or endorsed by Konami or any upstream author. Suikoden and related names are trademarks of their respective owners. The application contains no Konami logos, extracted artwork, music, fonts, or other game assets.

## Save-format and editor research

### SuikodenSaveDecrypter

Copyright (c) 2025 d3xMachina. MIT licensed. Its encryption/decryption implementation established the exact `GR_DATA:` envelope, password-based SHA-256 derivation, AES settings, salt layout, and compatibility oracle. See `LICENSES/SuikodenSaveDecrypter-MIT.txt`.

### Suikoden-Fix

Copyright (c) 2025 d3xMachina. MIT licensed. It enabled and documented decrypted-save workflows and informed backup and optional-mod compatibility behavior. See `LICENSES/Suikoden-Fix-MIT.txt`.

### Suikoden II HD Remaster Save Editor

Copyright (c) 2025 faospark. MIT licensed. Its current source, schemas, constants, changelog, renderers, and game-data tables informed Suikoden II paths, mappings, restrictions, and editor behavior. Substantially ported factual tables and behavior retain this notice. See `LICENSES/suisaveeditor-MIT.txt`.

### Suikoden reference documentation

asilverthorn's [suikoden_ref](https://github.com/asilverthorn/suikoden_ref) had no obvious license file at inspection time. It is credited as research documentation for Suikoden I and factual mapping provenance. This project does not label it MIT and does not reproduce its prose wholesale. Normal editor fields are independently verified against supplied save schemas and corroborating sources.

faospark also credits the [Suikosource item digits guide](https://suikosource.com/games/gs2/guides/itemdigits.php) and [makotech222/suiko2edit](https://github.com/makotech222/suiko2edit) for Suikoden II research. asilverthorn credits [UABEA](https://github.com/nesrak1/UABEA) as an extraction tool. These sources are credited for provenance; they are not bundled dependencies.

Cyril's [Suikoden Guide and Walkthrough](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/80674/part-10-to-live-and-die-freely) was used only to corroborate the factual Suikoden I headquarters cap: its level-4 form is described as the final development. No guide prose or other copyrighted material is distributed.

Shiro's [Suikoden Character Power-Up FAQ](https://gamefaqs.gamespot.com/ps/198843-suikoden/faqs/10601), DHolmes's [Suikoden II Game Save Hacking Guide](https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/7234), and Feral's [Suikoden II Armor/Equipment List](https://gamefaqs.gamespot.com/ps/198844-suikoden-ii/faqs/6620) informed factual level/stat/weapon limits, armor classes, known locked items, and end-game equipment recommendations. These copyrighted guides provide no software-license grant. No guide prose, code, or table is distributed.

[Gensopedia's Suikoden II equipment reference](https://gensopedia.org/w/Equipment_%28Suikoden_II%29) is available under Creative Commons Attribution-NonCommercial-ShareAlike unless otherwise noted. It was consulted only to cross-check factual equipment effects and compatibility; its prose and tables are not incorporated into the application. WiduraGoez's remaster 1.0.3 runtime-code research, hosted at [NSboy](https://www.nsboy.net/thread-31928-1-1.html), was used only to corroborate the remaster's stored-stat and HP limits. No cheat code or site prose is distributed, and no reuse license is claimed.

Exact inspected commits and usage details are recorded in `docs/UPSTREAM_SOURCES.md`.

## Packaging and Gitea automation references

The immutable package-publication and native Gitea release helpers are adapted from nintendogamer15's MIT-licensed [FFIX Save Editor](https://github.com/nintendogamer15/ffix-save-editor), copyright (c) 2026 ffix-save-editor contributors. Its notice is preserved in `LICENSES/ffix-save-editor-MIT.txt` and in the substantially adapted scripts.

nintendogamer15's [Final Fantasy IV 3D Remake Save Editor](https://github.com/nintendogamer15/Final-Fantasy-IV-3D-Remake-Save-Editor), licensed LGPL-3.0-or-later, was also inspected to corroborate the Docker-in-Docker Gitea workflow, Arch repository, RPM repository, installation-validation, and documentation conventions. No FFIV-specific application code or LGPL-covered source was copied into this project.

## Distributed runtime dependencies

The standalone application distributes Avalonia and a self-contained .NET runtime, plus their transitive components. AvaloniaUI OÜ and contributors license Avalonia under MIT; see `LICENSES/Avalonia-MIT.txt`. Avalonia's desktop graph also distributes ANGLE, Inter, HarfBuzzSharp, MicroCom.Runtime, SkiaSharp, and Tmds.DBus.Protocol under the terms identified in `LICENSES/DEPENDENCIES.md`, with each applicable license text in `LICENSES/`. The .NET Foundation and contributors license .NET under MIT; see `LICENSES/dotnet-MIT.txt` and `LICENSES/dotnet-THIRD-PARTY-NOTICES.txt`. The dependency inventory and applicable license texts are included in every release archive.

The standalone application also distributes the shared save-editor GUI framework `SaveEditor.Ui`, consumed as a git submodule under `external/save-editor-gui-framework` and pinned at commit a50464b. nintendogamer15 licenses it under the Zero-Clause BSD license (`0BSD`); see `LICENSES/SaveEditor.Ui-0BSD.txt`. It in turn distributes `CommunityToolkit.Mvvm`, which the .NET Foundation and contributors license under MIT; see `LICENSES/CommunityToolkit.Mvvm-MIT.txt`.

Test-only packages are not included in standalone application executables. Their package metadata and licenses remain visible through the locked NuGet dependency graph and source project files.

## Original project code

Unless a source file says otherwise, original code in this repository is licensed under the Zero-Clause BSD license (`0BSD`); see `LICENSE`. Third-party terms above remain in force for copied or substantially ported portions.
