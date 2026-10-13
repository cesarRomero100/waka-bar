import Foundation
import Testing

/// Xcode adds every new user-facing string to the catalog when building; these tests fail until it is translated.
struct LocalizationTests {
    static let languages = ["es"]

    private struct Catalog: Decodable {
        struct Entry: Decodable {
            struct Localization: Decodable {
                struct StringUnit: Decodable {
                    let state: String
                    let value: String
                }
                let stringUnit: StringUnit?
            }
            let localizations: [String: Localization]?
            let shouldTranslate: Bool?
        }
        let sourceLanguage: String
        let strings: [String: Entry]
    }

    private static let catalog: Catalog = {
        let url = URL(fileURLWithPath: #filePath)
            .deletingLastPathComponent()
            .deletingLastPathComponent()
            .appending(path: "Shared/Localizable.xcstrings")
        return try! JSONDecoder().decode(Catalog.self, from: Data(contentsOf: url))
    }()

    private static var translatableKeys: [String] {
        catalog.strings.filter { $0.value.shouldTranslate != false }.keys.sorted()
    }

    @Test func sourceLanguageIsEnglish() {
        #expect(Self.catalog.sourceLanguage == "en")
    }

    @Test(arguments: languages)
    func everyStringIsTranslated(language: String) {
        let missing = Self.translatableKeys.filter { key in
            let unit = Self.catalog.strings[key]?.localizations?[language]?.stringUnit
            return unit?.state != "translated" || unit?.value.isEmpty != false
        }
        #expect(missing.isEmpty, "Missing \(language) translations: \(missing)")
    }

    @Test(arguments: languages)
    func translationsKeepTheirPlaceholders(language: String) {
        for key in Self.translatableKeys {
            guard let value = Self.catalog.strings[key]?.localizations?[language]?.stringUnit?.value else { continue }
            #expect(placeholders(in: value) == placeholders(in: key), "\(language): \"\(value)\" for \"\(key)\"")
        }
    }

    /// Format specifiers such as %@, %lld or %1$@, in order.
    private func placeholders(in string: String) -> [String] {
        let pattern = #/%(?:\d+\$)?(?:@|l{0,2}[dfu])/#
        return string.matches(of: pattern).map { String($0.output) }
    }
}
