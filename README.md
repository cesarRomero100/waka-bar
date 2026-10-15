# WakaBar

[![macOS](https://github.com/cesarRomero100/waka-bar/actions/workflows/macos.yml/badge.svg)](https://github.com/cesarRomero100/waka-bar/actions/workflows/macos.yml)
[![Windows](https://github.com/cesarRomero100/waka-bar/actions/workflows/windows.yml/badge.svg)](https://github.com/cesarRomero100/waka-bar/actions/workflows/windows.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Open-source desktop apps and widgets for your [WakaTime](https://wakatime.com) stats
(also works with self-hosted [Wakapi](https://github.com/muety/wakapi)).

- **Always visible:** today's coding time. Refreshes every 5 minutes and when the computer wakes.
- **Details:** today, last 7 days (total, daily average, bar chart), and top languages or projects.
- **Daily goal** with a progress bar, set in Settings.
- **Streak:** consecutive days coding (up to 30), and today's top project.
- **Widget:** small / medium / large. Click it to open your dashboard.
- **Launch at login**, from Settings.
- **English and Spanish**, following your system language.

## Platforms

| Platform | Status | Folder |
|---|---|---|
| macOS 14+ | Available: menu bar app and desktop widget | [`macos/`](macos) |
| Windows 11 | In progress: system tray app done; Widgets Board widget next | [`windows/`](windows) |

Each platform is a native app with its own README, build steps and CI.

## Repository layout

```
macos/            Swift / SwiftUI app and widget
windows/          C# / .NET app (in progress)
shared/fixtures/  WakaTime API responses that every platform's tests parse
```

## License

MIT
