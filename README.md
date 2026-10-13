# WakaBar

[![CI](https://github.com/cesarRomero100/waka-bar/actions/workflows/ci.yml/badge.svg)](https://github.com/cesarRomero100/waka-bar/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Open-source macOS menu bar app and desktop widget for your [WakaTime](https://wakatime.com) stats
(also works with self-hosted [Wakapi](https://github.com/muety/wakapi)).

- **Menu bar:** today's coding time, always visible. Refreshes every 5 minutes and when the Mac wakes.
- **Popover:** today, last 7 days (total, daily average, bar chart) and top languages.
- **Widget:** small / medium / large, for the desktop or Notification Center. Click it to open your dashboard.
- **Launch at login**, from Settings.

Requires macOS 14+.

## Build

```sh
brew install xcodegen
cp Config/Local.xcconfig.example Config/Local.xcconfig
xcodegen
open WakaBar.xcodeproj
```

1. In `Config/Local.xcconfig` (git-ignored), set your `DEVELOPMENT_TEAM` and an `APP_BUNDLE_ID` you
   own. A team is required because the app and the widget share data through an App Group; a free
   Personal Team works. Don't set signing in Xcode's Signing tab: `xcodegen` overwrites it.
2. Run the `WakaBar` scheme. Open Settings from the menu bar item and paste your
   [API key](https://wakatime.com/settings/api-key).
3. Add the widget: right-click the desktop → Edit Widgets → WakaBar.

**Widget still shows the old version after a rebuild?** macOS keeps the previous widget extension
running. Stop it, then click ↻ in the menu bar popover so the widget redraws with the new code:

```sh
pkill -x WakaBarWidget
```

If it still looks old, remove the widget from the desktop and add it again.

## How it works

```
App/        menu bar app: API client, Keychain, refresh loop, UI
Widget/     WidgetKit extension: reads the cached snapshot only
Shared/     models, snapshot store (App Group), formatting, shared chart views
Config/     xcconfig build settings; Local.xcconfig holds your signing values
Scripts/    generate-icon.swift regenerates the app icon
Tests/      unit tests (Swift Testing): API parsing, snapshot logic, formatting
```

The app polls the API every 5 minutes (`status_bar/today` + `summaries?range=last_7_days`),
saves a `Snapshot` as JSON in the App Group container and reloads the widget timelines.
The widget never sees your API key.

## Contributing

Code follows the [Swift API Design Guidelines](https://www.swift.org/documentation/api-design-guidelines/)
and is formatted with [swift-format](https://github.com/swiftlang/swift-format) (bundled with Swift 6+),
configured in `.swift-format`. Xcode runs the linter on every build and shows violations as warnings.
Before opening a PR:

```sh
swift format format --in-place --recursive --parallel App Shared Widget Scripts Tests
swift format lint --strict --recursive --parallel App Shared Widget Scripts Tests
xcodebuild test -project WakaBar.xcodeproj -scheme WakaBar -destination 'platform=macOS'
```

Tests run without a network, Keychain or signing team: API responses are fixtures in
`Tests/Support/Fixtures.swift`. In Xcode, press ⌘U. CI runs lint, build and tests on every PR.

## License

MIT
