import Foundation
import Testing

@Suite("Building a snapshot from API responses")
struct MakeSnapshotTests {
    private func makeSnapshot(
        today: Double,
        projectsToday: [(name: String, seconds: Double)] = [],
        history: [Fixtures.Day],
        now: Date = Fixtures.now
    ) throws -> Snapshot {
        try WakaTimeClient.makeSnapshot(
            today: Fixtures.statusBar(seconds: today, projects: projectsToday),
            history: Fixtures.summaries(history),
            now: now,
            calendar: Fixtures.calendar
        )
    }

    private var week: [Fixtures.Day] {
        Fixtures.weekDates.enumerated().map { index, date in
            Fixtures.Day(
                date: date,
                seconds: Double(index + 1) * 3600,
                languages: [("Swift", Double(index + 1) * 2400), ("TypeScript", 1200), ("YAML", 5)],
                projects: [("waka-bar", Double(index + 1) * 1800), ("dotfiles", 600)]
            )
        }
    }

    @Test func usesStatusBarForToday() throws {
        #expect(try makeSnapshot(today: 11520, history: week).todaySeconds == 11520)
    }

    @Test func parsesTheSevenDaysInOrder() throws {
        let days = try makeSnapshot(today: 0, history: week).days
        #expect(days.map(\.date) == Fixtures.weekDates.map(Fixtures.day))
        #expect(days.first?.seconds == 3600)
    }

    @Test func showsOnlyTheLastSevenDaysOfALongerHistory() throws {
        let days = try makeSnapshot(today: 0, history: Fixtures.history(hours: Array(repeating: 1, count: 30))).days
        #expect(days.map(\.date) == Fixtures.weekDates.map(Fixtures.day))
    }

    @Test func todaysBarMatchesTheStatusBarTotal() throws {
        // Summaries says 7h for today, status_bar is fresher and says 7h 30m.
        let snapshot = try makeSnapshot(today: 7.5 * 3600, history: week)
        #expect(snapshot.days.last?.seconds == 7.5 * 3600)
    }

    @Test func keepsTheLastBarWhenItIsNotToday() throws {
        // Just after midnight: the week still ends yesterday.
        let tomorrow = Fixtures.calendar.date(byAdding: .day, value: 1, to: Fixtures.now)!
        let snapshot = try makeSnapshot(today: 600, history: week, now: tomorrow)
        #expect(snapshot.days.last?.seconds == 7.0 * 3600)
    }

    @Test func fallsBackToConsecutiveDaysWithoutRangeDates() throws {
        let undated = week.map { Fixtures.Day(date: nil, seconds: $0.seconds) }
        let days = try makeSnapshot(today: 0, history: undated).days
        #expect(days.map(\.date) == Fixtures.weekDates.map(Fixtures.day))
    }

    @Test func sumsLanguagesAcrossTheWeekSortedByTime() throws {
        let languages = try makeSnapshot(today: 0, history: week).languages
        #expect(languages.map(\.name) == ["Swift", "TypeScript"])
        #expect(languages.first?.seconds == (1...7).reduce(0) { $0 + Double($1) * 2400 })
        #expect(languages.last?.seconds == 7.0 * 1200)
    }

    @Test func ignoresLanguagesAndProjectsOlderThanAWeek() throws {
        var history = Fixtures.history(hours: Array(repeating: 1, count: 30))
        history[0].languages = [("COBOL", 10 * 3600)]
        history[0].projects = [("legacy", 10 * 3600)]
        let snapshot = try makeSnapshot(today: 0, history: history)
        #expect(snapshot.languages.isEmpty)
        #expect(snapshot.projects.isEmpty)
    }

    @Test func dropsLanguagesUnderOneMinute() throws {
        let languages = try makeSnapshot(today: 0, history: week).languages
        #expect(!languages.contains { $0.name == "YAML" })
    }

    @Test func toleratesDaysWithoutLanguagesOrProjects() throws {
        let bare = week.map { Fixtures.Day(date: $0.date, seconds: $0.seconds, languages: nil, projects: nil) }
        let snapshot = try makeSnapshot(today: 0, history: bare)
        #expect(snapshot.languages.isEmpty)
        #expect(snapshot.projects.isEmpty)
        #expect(snapshot.topProjectToday == nil)
    }

    @Test func sumsProjectsAcrossTheWeekSortedByTime() throws {
        let projects = try makeSnapshot(today: 0, history: week).projects
        #expect(projects.map(\.name) == ["waka-bar", "dotfiles"])
        #expect(projects.last?.seconds == 7.0 * 600)
    }

    @Test func picksTodaysTopProjectFromTheStatusBar() throws {
        let snapshot = try makeSnapshot(
            today: 3 * 3600,
            projectsToday: [("dotfiles", 1800), ("waka-bar", 9000), ("scratch", 30)],
            history: week
        )
        #expect(snapshot.topProjectToday == UsageStat(name: "waka-bar", seconds: 9000))
    }

    @Test func countsTheStreakAcrossTheWholeHistory() throws {
        // 12 active days in a row, after a day off 13 days ago.
        let hours = Array(repeating: 2.0, count: 17) + [0] + Array(repeating: 1.0, count: 12)
        #expect(try makeSnapshot(today: 3600, history: Fixtures.history(hours: hours)).streakDays == 12)
    }

    @Test func rejectsMalformedResponses() {
        #expect(throws: DecodingError.self) {
            try WakaTimeClient.makeSnapshot(today: Data("{}".utf8), history: Fixtures.summaries(week))
        }
    }
}

@Suite("HTTP requests", .serialized)
struct WakaTimeClientHTTPTests {
    private let client = WakaTimeClient(
        baseURL: URL(string: "https://example.com/api/v1")!,
        apiKey: "waka_test_key",
        session: StubURLProtocol.session()
    )

    init() {
        StubURLProtocol.requests.removeAll()
    }

    /// Answers every request with `status`, except `overrides` keyed by the `range` query value.
    private func respond(status: Int, overrides: [String: Int] = [:]) {
        StubURLProtocol.handler = { request in
            let url = request.url!
            let range = URLComponents(url: url, resolvingAgainstBaseURL: false)?
                .queryItems?.first { $0.name == "range" }?.value
            let code = range.flatMap { overrides[$0] } ?? status
            guard code == 200 else { return (code, Data()) }
            let body =
                url.path.hasSuffix("status_bar/today")
                ? Fixtures.statusBar(seconds: 3600)
                : Fixtures.summaries(Fixtures.history(hours: Array(repeating: 1, count: 7)))
            return (200, body)
        }
    }

    private var requestedURLs: [String] {
        StubURLProtocol.requests.all.compactMap(\.url).map(\.absoluteString).sorted()
    }

    @Test func requestsTodayAndTheLastThirtyDays() async throws {
        respond(status: 200)
        _ = try await client.fetchSnapshot()
        #expect(
            requestedURLs == [
                "https://example.com/api/v1/users/current/status_bar/today",
                "https://example.com/api/v1/users/current/summaries?range=last_30_days",
            ]
        )
    }

    @Test func fallsBackToSevenDaysWhenThirtyAreRefused() async throws {
        respond(status: 200, overrides: ["last_30_days": 402])
        let snapshot = try await client.fetchSnapshot()
        #expect(snapshot.days.count == 7)
        #expect(requestedURLs.contains("https://example.com/api/v1/users/current/summaries?range=last_7_days"))
    }

    @Test func authenticatesWithBasicAuth() async throws {
        respond(status: 200)
        _ = try await client.fetchSnapshot()

        let expected = "Basic \(Data("waka_test_key".utf8).base64EncodedString())"
        let headers = StubURLProtocol.requests.all.map { $0.value(forHTTPHeaderField: "Authorization") }
        #expect(headers == [expected, expected])
    }

    @Test func reportsAnInvalidAPIKeyWithoutFallingBack() async {
        respond(status: 401)
        await #expect(throws: WakaTimeClient.ClientError.unauthorized) {
            try await client.fetchSnapshot()
        }
        #expect(!requestedURLs.contains { $0.hasSuffix("range=last_7_days") })
    }

    @Test func reportsServerErrors() async {
        respond(status: 503)
        await #expect(throws: WakaTimeClient.ClientError.http(503)) {
            try await client.fetchSnapshot()
        }
    }
}

struct DashboardURLTests {
    // Typed constant: an inline tuple literal is too slow for the type checker inside @Test.
    static let examples: [(baseURL: String, dashboard: String?)] = [
        ("https://api.wakatime.com/api/v1", "https://wakatime.com/dashboard"),
        ("https://wakatime.com/api/v1", "https://wakatime.com/dashboard"),
        ("https://wakapi.dev/api/compat/wakatime/v1", "https://wakapi.dev/"),
        ("http://localhost:3000/api/compat/wakatime/v1", "http://localhost:3000/"),
        ("https://example.com/wakapi/api/compat/wakatime/v1", "https://example.com/wakapi/"),
        ("https://notwakatime.com/api/compat/wakatime/v1", "https://notwakatime.com/"),
        ("https://user:pass@example.com:8443/api/v1?x=1#top", "https://example.com:8443/"),
        ("not a url", nil),
        ("", nil),
    ]

    @Test("Opens the dashboard of the configured server", arguments: examples)
    func dashboard(_ example: (baseURL: String, dashboard: String?)) {
        #expect(WakaTimeClient.dashboardURL(forBaseURL: example.baseURL)?.absoluteString == example.dashboard)
    }
}

struct BaseURLTests {
    static let valid = [
        "https://api.wakatime.com/api/v1",
        "http://localhost:3000/api/compat/wakatime/v1",
        "HTTPS://wakapi.dev/api/compat/wakatime/v1",
    ]

    static let invalid = [
        "wakapi.example.com/api/compat/wakatime/v1",
        "localhost:3000/api/compat/wakatime/v1",
        "ftp://example.com/api/v1",
        "https://",
        "not a url",
        "",
    ]

    @Test("Accepts absolute http(s) URLs", arguments: valid)
    func accepts(_ string: String) {
        #expect(WakaTimeClient.baseURL(from: string)?.absoluteString == string)
    }

    @Test("Rejects URLs without an http(s) scheme or a host", arguments: invalid)
    func rejects(_ string: String) {
        #expect(WakaTimeClient.baseURL(from: string) == nil)
    }
}
