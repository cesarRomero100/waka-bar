import Foundation

/// Links of the "Support WakaBar" section in Settings. Same values in windows/src/WakaBar.App/SupportLinks.cs.
enum SupportLinks {
    static let gitHub = URL(string: "https://github.com/cesarRomero100/waka-bar")!

    /// Buy Me a Coffee page, e.g. "https://buymeacoffee.com/<username>". The button stays hidden while nil.
    static let buyMeACoffee: URL? = nil

    /// "0.1.0", from MARKETING_VERSION.
    static var version: String {
        Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "?"
    }
}
