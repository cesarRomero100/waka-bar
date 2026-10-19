# WakaBar for Windows 11

[![Windows](https://github.com/cesarRomero100/waka-bar/actions/workflows/windows.yml/badge.svg)](https://github.com/cesarRomero100/waka-bar/actions/workflows/windows.yml)

System tray app and Widgets Board widget, in C# and WinUI 3. **In progress.**

| Phase | Scope | Status |
|---|---|---|
| 1 | `WakaBar.Core`: API client, snapshot, streak, goal, projects; tests | Done |
| 2 | Tray icon and stats window, Settings (API key in Credential Manager, server, goal, launch at startup) | Done |
| 3 | Widgets Board widget (Adaptive Cards), small / medium / large | Done |
| 4a | English and Spanish, following the Windows language | Done |
| 4b | Distribution (Microsoft Store) | Planned |

## Run it

1. Turn on **Developer Mode**: Settings › System › For developers. Visual Studio needs it to install
   the app package.
2. Install **Visual Studio 2022 (17.14+) or 2026** with the **WinUI application development** workload,
   and the [.NET 10 SDK](https://dotnet.microsoft.com/download).
3. Open `windows/WakaBar.slnx`, set **WakaBar.App** as the startup project, pick **x64** (or **ARM64**
   on an ARM PC) and press **F5**.
4. WakaBar lives in the system tray, next to the clock. If you don't see it, open the **^** overflow
   and drag it onto the taskbar. The first launch opens Settings: paste your
   [API key](https://wakatime.com/settings/api-key).

- **Left click** the icon: stats window. It hides when you click elsewhere.
- **Right click**: Refresh, Open dashboard, Settings, Quit.
- With a daily goal, a ring around the icon fills up as you code and turns green when you reach it.
  Hover the icon for today's time, the goal and the streak.
- **Widget**: open the Widgets Board (**Win+W**) › **+ Add widgets** › **WakaBar · Coding Time**. Small shows
  today and the goal; medium adds the last 7 days; large adds languages and projects. Click it to open
  your dashboard. It's served by the tray app (started automatically if needed) and updates with it.

**Widget missing from the picker after a deploy?** The Widgets Board caches its list of widgets.
Restart it, then open it again with Win+W:

```powershell
Stop-Process -Name Widgets, WidgetService -Force -ErrorAction SilentlyContinue
```

## Layout

```
src/WakaBar.Core/           platform-independent logic (net10.0): no Windows APIs
src/WakaBar.App/            WinUI 3 tray app, packaged as MSIX
  Services/                 settings, refresh loop, snapshot cache, launch at startup
  Widgets/                  Widgets Board provider (COM), chart and goal bar images
  Public/                   widget icon and widget picker screenshots
  Assets/                   package images (Scripts/generate-assets.swift, needs a Mac)
tests/WakaBar.Core.Tests/   xUnit tests, including shared/fixtures from the repo root
```

`WakaBar.Core` mirrors the macOS logic (`macos/App/WakaTimeClient.swift`, `macos/Shared/Snapshot.swift`).
Both apps parse the same responses in [`../shared/fixtures`](../shared/fixtures) and must get the
numbers listed there; when one side changes behavior, change the other too.

The app is tray-only: its windows are hidden, never closed, and **Quit** ends the process.
The widget's Adaptive Card template and data live in `WakaBar.Core` (`WidgetTemplate.json`, `WidgetData`),
so they're unit-tested; a test checks that every `${field}` the template binds exists in the data.
Launching it again while it runs shows the stats of the running instance (`Program.cs`).

## Build and test

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). `WakaBar.Core` and its tests also
build on macOS and Linux. From this `windows/` folder:

```sh
dotnet test tests/WakaBar.Core.Tests
```

The app builds on Windows only, for a real platform:

```sh
dotnet build src/WakaBar.App -p:Platform=x64
```

Before opening a PR, also check formatting (CI fails on any difference):

```sh
dotnet format src/WakaBar.Core/WakaBar.Core.csproj --verify-no-changes
dotnet format tests/WakaBar.Core.Tests/WakaBar.Core.Tests.csproj --verify-no-changes
dotnet format whitespace src/WakaBar.App --folder --verify-no-changes
```

## Translations

The app follows the Windows display language (English or Spanish; anything else falls back to English).

- **Windows and code** (`src/WakaBar.App/Strings/<language>/Resources.resw`): XAML elements get their text
  through `x:Uid` (keys like `TodayLabel.Text`); code uses `Strings.Get` / `Strings.Format`. The package
  manifest uses `ms-resource:` keys from the same files.
- **Core** (`src/WakaBar.Core/Resources/Strings.resx` and `Strings.es.resx`): the widget's texts, via
  `CoreStrings`. The app passes its language to Core at startup, so both always match.
- Shared wording comes from the macOS catalog (`macos/Shared/Localizable.xcstrings`).
- `TranslationTests` fails if a string is missing in one language or a translation loses a placeholder
  like `{0}`. To add a language, add a `Strings/<language>` folder and a `Strings.<language>.resx`,
  then list them in `TranslationTests.Files`.
- Widget picker screenshots per language live in `Public/<language>/` (`Scripts/generate-assets.swift`).
