import AppKit

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    let model = StatsModel()

    /// Handles `wakabar://` links, e.g. a click on the widget.
    func application(_ application: NSApplication, open urls: [URL]) {
        for url in urls where url.scheme == DeepLink.scheme {
            switch url.host() {
            case "dashboard":
                if let dashboard = model.dashboardURL { NSWorkspace.shared.open(dashboard) }
            default:
                break
            }
        }
    }
}
