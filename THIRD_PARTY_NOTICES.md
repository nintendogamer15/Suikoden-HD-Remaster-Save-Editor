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

Exact inspected commits and usage details are recorded in `docs/UPSTREAM_SOURCES.md`.

## Distributed runtime dependencies

The standalone application distributes Avalonia and a self-contained .NET runtime, plus their transitive components. AvaloniaUI OÜ and contributors license Avalonia under MIT; see `LICENSES/Avalonia-MIT.txt`. The .NET Foundation and contributors license .NET under MIT; see `LICENSES/dotnet-MIT.txt` and `LICENSES/dotnet-THIRD-PARTY-NOTICES.txt`. A generated dependency inventory and applicable license texts are included in every release archive.

Test-only packages are not included in standalone application archives. Their package metadata and licenses remain visible through the locked NuGet dependency graph and source project files.

## Original project code

Unless a source file says otherwise, original code in this repository is licensed under the Zero-Clause BSD license (`0BSD`); see `LICENSE`. Third-party terms above remain in force for copied or substantially ported portions.

