# WakaBar for Windows 11

[![Windows](https://github.com/cesarRomero100/waka-bar/actions/workflows/windows.yml/badge.svg)](https://github.com/cesarRomero100/waka-bar/actions/workflows/windows.yml)

System tray app and Widgets Board widget, in C# and WinUI 3. **In progress.**

| Phase | Scope | Status |
|---|---|---|
| 1 | `WakaBar.Core`: API client, snapshot, streak, goal, projects; tests | Done |
| 2 | Tray icon and flyout window, Settings (API key in Credential Manager, server, goal, launch at login) | Planned |
| 3 | Widgets Board widget (Adaptive Cards) | Planned |
| 4 | Spanish, MSIX packaging, publishing | Planned |

## Layout

```
src/WakaBar.Core/           platform-independent logic (net10.0): no Windows APIs
tests/WakaBar.Core.Tests/   xUnit tests, including shared/fixtures from the repo root
```

`WakaBar.Core` mirrors the macOS logic (`macos/App/WakaTimeClient.swift`, `macos/Shared/Snapshot.swift`).
Both apps parse the same responses in [`../shared/fixtures`](../shared/fixtures) and must get the
numbers listed there; when one side changes behavior, change the other too.

## Build and test

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). `WakaBar.Core` and its tests also
build on macOS and Linux. From this `windows/` folder:

```sh
dotnet test
```

Before opening a PR, also check formatting (CI fails on any difference):

```sh
dotnet format --verify-no-changes
```
