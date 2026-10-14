import Foundation

/// URLs the widget sends to the app (registered under CFBundleURLTypes).
enum DeepLink {
    static let scheme = "wakabar"
    static let dashboard = URL(string: "\(scheme)://dashboard")!
}
