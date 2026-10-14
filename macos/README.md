# WakaBar for macOS

[![macOS](https://github.com/cesarRomero100/waka-bar/actions/workflows/macos.yml/badge.svg)](https://github.com/cesarRomero100/waka-bar/actions/workflows/macos.yml)

Menu bar app and WidgetKit widget, in Swift and SwiftUI. Requires macOS 14+.
Features are listed in the [main README](../README.md).

## Build

From this `macos/` folder:

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
Tests/      unit tests (Swift Testing): API parsing, snapshot logic, formatting, translations
```

The app polls the API every 5 minutes (`status_bar/today` + `summaries?range=last_30_days`, falling back
to `last_7_days` if the server refuses 30 days),
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

Tests run without a network, Keychain or signing team. API responses come from
`Tests/Support/Fixtures.swift` (per-scenario data) and from [`../shared/fixtures`](../shared/fixtures)
(real-shaped responses every platform's tests parse). In Xcode, press ⌘U. CI runs lint, build and
tests on every PR that touches `macos/` or `shared/`.

### Translations

User-facing strings live in `Shared/Localizable.xcstrings` (shared by the app and the widget). Write UI
text in English; Xcode adds new strings to the catalog when you build. Translate them in Xcode's catalog
editor: `LocalizationTests` fails if any string is missing a translation or loses a placeholder like `%@`.
To add a language, add it in the catalog editor and to `LocalizationTests.languages`.
