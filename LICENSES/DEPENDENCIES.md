# Distributed dependency inventory

This inventory reflects the locked `net10.0` standalone application graph inspected on 2026-08-25. Runtime identifiers select the matching native assets; test-only packages are not embedded in the standalone executables.

| Component | Locked version | Role | License material |
|---|---:|---|---|
| Avalonia core, Desktop, Fluent, FreeDesktop, X11, Win32, Native, Skia, HarfBuzz, MicroCom integration, and Remote Protocol | 12.1.1 | UI framework and platform backends | `Avalonia-MIT.txt` |
| Avalonia.Angle.Windows.Natives | 2.1.27548.20260419 | Windows graphics backend | `ANGLE-BSD-3-Clause.txt` |
| Avalonia.Fonts.Inter | 12.1.1 | Avalonia font integration | `Avalonia-MIT.txt`; embedded Inter font under `Inter-OFL-1.1.txt` |
| HarfBuzzSharp and native assets | 8.3.1.3 | Text shaping | `HarfBuzzSharp-MIT.txt` |
| MicroCom.Runtime | 0.11.6 | Native interop | `MicroCom-MIT.txt` |
| SkiaSharp and native assets | 3.119.4 | Graphics | `SkiaSharp-MIT.txt` |
| Tmds.DBus.Protocol | 0.94.1 | Linux desktop integration | `Tmds.DBus-MIT.txt` |
| SaveEditor.Ui (submodule, 6ee70c4) | 1.0.0-alpha.3 | Shared save-editor shell, theming, field editing, and safe-write workflow | `SaveEditor.Ui-0BSD.txt` |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM primitives used by SaveEditor.Ui | `CommunityToolkit.Mvvm-MIT.txt` |
| .NET self-contained runtime | .NET 10 runtime selected by SDK 10.0.400 | Managed runtime and base class libraries | `dotnet-MIT.txt`, `dotnet-THIRD-PARTY-NOTICES.txt` |
| Microsoft.NET.ILLink.Tasks | 10.0.11 | Single-file publish infrastructure; trimming is disabled | `dotnet-MIT.txt`, `dotnet-THIRD-PARTY-NOTICES.txt` |

The package lock files are authoritative for exact dependency resolution. Avalonia.BuildServices 11.3.2 and Microsoft.NET.ILLink.Tasks are build-time infrastructure and are not embedded in published application executables.

The source repository's Gitea release and immutable package-publication helpers are adapted from the MIT-licensed `ffix-save-editor`; its source notice is preserved in `ffix-save-editor-MIT.txt`. It is build/release automation, not an application runtime dependency.
