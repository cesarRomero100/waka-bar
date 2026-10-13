import SwiftUI

@main
struct WakaBarApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate

    var body: some Scene {
        MenuBarExtra {
            MenuContentView(model: appDelegate.model)
        } label: {
            Image(systemName: "chart.bar.fill")
            if let snapshot = appDelegate.model.snapshot {
                Text(Format.duration(snapshot.todaySeconds()))
            }
        }
        .menuBarExtraStyle(.window)

        Settings {
            SettingsView(model: appDelegate.model)
        }
    }
}
