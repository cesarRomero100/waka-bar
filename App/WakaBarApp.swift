import SwiftUI

@main
struct WakaBarApp: App {
    @State private var model = StatsModel()

    var body: some Scene {
        MenuBarExtra {
            MenuContentView(model: model)
        } label: {
            Image(systemName: "chart.bar.fill")
            if let snapshot = model.snapshot {
                Text(Format.duration(snapshot.todaySeconds()))
            }
        }
        .menuBarExtraStyle(.window)

        Settings {
            SettingsView(model: model)
        }
    }
}
