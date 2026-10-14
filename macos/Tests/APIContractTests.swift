import Foundation
import Testing

/// Parses the API responses in `shared/fixtures/`, which every platform's tests use.
/// Expected values are listed in `shared/fixtures/README.md`.
struct APIContractTests {
    private static let fixtures = URL(fileURLWithPath: #filePath)
        .deletingLastPathComponent()  // Tests
        .deletingLastPathComponent()  // macos
        .deletingLastPathComponent()  // repo root
        .appending(path: "shared/fixtures")

    private let snapshot: Snapshot

    init() throws {
        snapshot = try WakaTimeClient.makeSnapshot(
            today: Data(contentsOf: Self.fixtures.appending(path: "status_bar_today.json")),
            history: Data(contentsOf: Self.fixtures.appending(path: "summaries_last_7_days.json")),
            now: Fixtures.now,
            calendar: Fixtures.calendar
        )
    }

    @Test func today() {
        #expect(snapshot.todaySeconds == 11520)
        #expect(snapshot.topProjectToday == UsageStat(name: "waka-bar", seconds: 7200))
    }

    @Test func week() {
        #expect(snapshot.days.map(\.date) == Fixtures.weekDates.map(Fixtures.day))
        // Summaries lag behind status_bar for today.
        #expect(snapshot.days.last?.seconds == 11520)
        #expect(snapshot.weekSeconds == 92520)
    }

    @Test func languages() {
        #expect(
            snapshot.languages == [
                UsageStat(name: "Swift", seconds: 54600),
                UsageStat(name: "TypeScript", seconds: 27300),
                UsageStat(name: "Markdown", seconds: 9100),
            ]
        )
    }

    @Test func projects() {
        #expect(
            snapshot.projects == [
                UsageStat(name: "waka-bar", seconds: 45500),
                UsageStat(name: "marketplace", seconds: 36400),
                UsageStat(name: "dotfiles", seconds: 9100),
            ]
        )
    }

    @Test func streak() {
        #expect(snapshot.streakDays == 7)
    }
}
