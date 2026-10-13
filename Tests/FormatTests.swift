import Testing

struct FormatTests {
    // Typed constant: an inline tuple literal is too slow for the type checker inside @Test.
    static let examples: [(seconds: Double, text: String)] = [
        (0, "0m"),
        (59, "0m"),
        (60, "1m"),
        (45 * 60, "45m"),
        (3600, "1h"),
        (3660, "1h 1m"),
        (3 * 3600 + 12 * 60, "3h 12m"),
        (10 * 3600, "10h"),
    ]

    @Test("Formats seconds as hours and minutes", arguments: examples)
    func duration(_ example: (seconds: Double, text: String)) {
        #expect(Format.duration(example.seconds) == example.text)
    }
}
