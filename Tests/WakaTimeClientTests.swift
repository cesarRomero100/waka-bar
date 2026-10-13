import Foundation
import Testing

@Suite("Building a snapshot from API responses")
struct MakeSnapshotTests {
    private func makeSnapshot(today: Double, week: [Fixtures.Day], now: Date = Fixtures.now) throws -> Snapshot {
        try WakaTimeClient.makeSnapshot(
            today: Fixtures.statusBar(seconds: today),
            week: Fixtures.summaries(week),
            now: now,
            calendar: Fixtures.calendar
        )
    }

    private var week: [Fixtures.Day] {
        Fixtures.weekDates.enumerated().map { index, date in
            Fixtures.Day(
                date: date,
                seconds: Double(index + 1) * 3600,
                languages: [("Swift", Double(index + 1) * 2400), ("TypeScript", 1200), ("YAML", 5)]
            )
        }
    }

    @Test func usesStatusBarForToday() throws {
        #expect(try makeSnapshot(today: 11520, week: week).todaySeconds == 11520)
    }

    @Test func parsesTheSevenDaysInOrder() throws {
        let days = try makeSnapshot(today: 0, week: week).days
        #expect(days.map(\.date) == Fixtures.weekDates.map(Fixtures.day))
        #expect(days.first?.seconds == 3600)
    }

    @Test func todaysBarMatchesTheStatusBarTotal() throws {
        // Summaries says 7h for today, status_bar is fresher and says 7h 30m.
        let snapshot = try makeSnapshot(today: 7.5 * 3600, week: week)
        #expect(snapshot.days.last?.seconds == 7.5 * 3600)
    }

    @Test func keepsTheLastBarWhenItIsNotToday() throws {
        // Just after midnight: the week still ends yesterday.
        let tomorrow = Fixtures.calendar.date(byAdding: .day, value: 1, to: Fixtures.now)!
        let snapshot = try makeSnapshot(today: 600, week: week, now: tomorrow)
        #expect(snapshot.days.last?.seconds == 7.0 * 3600)
    }

    @Test func fallsBackToConsecutiveDaysWithoutRangeDates() throws {
        let undated = week.map { Fixtures.Day(date: nil, seconds: $0.seconds) }
        let days = try makeSnapshot(today: 0, week: undated).days
        #expect(days.map(\.date) == Fixtures.weekDates.map(Fixtures.day))
    }

    @Test func sumsLanguagesAcrossTheWeekSortedByTime() throws {
        let languages = try makeSnapshot(today: 0, week: week).languages
        #expect(languages.map(\.name) == ["Swift", "TypeScript"])
        #expect(languages.first?.seconds == (1...7).reduce(0) { $0 + Double($1) * 2400 })
        #expect(languages.last?.seconds == 7.0 * 1200)
    }

    @Test func dropsLanguagesUnderOneMinute() throws {
        let languages = try makeSnapshot(today: 0, week: week).languages
        #expect(!languages.contains { $0.name == "YAML" })
    }

    @Test func toleratesDaysWithoutLanguages() throws {
        let noLanguages = week.map { Fixtures.Day(date: $0.date, seconds: $0.seconds, languages: nil) }
        #expect(try makeSnapshot(today: 0, week: noLanguages).languages.isEmpty)
    }

    @Test func rejectsMalformedResponses() {
        #expect(throws: DecodingError.self) {
            try WakaTimeClient.makeSnapshot(today: Data("{}".utf8), week: Fixtures.summaries(week))
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

    private func respond(status: Int) {
        StubURLProtocol.handler = { request in
            guard status == 200 else { return (status, Data()) }
            let isToday = request.url!.path.hasSuffix("status_bar/today")
            let body =
                isToday
                ? Fixtures.statusBar(seconds: 3600)
                : Fixtures.summaries(Fixtures.weekDates.map { .init(date: $0, seconds: 3600) })
            return (200, body)
        }
    }

    @Test func requestsTodayAndTheLastSevenDays() async throws {
        respond(status: 200)
        _ = try await client.fetchSnapshot()

        let urls = StubURLProtocol.requests.all.compactMap(\.url).map(\.absoluteString).sorted()
        #expect(
            urls == [
                "https://example.com/api/v1/users/current/status_bar/today",
                "https://example.com/api/v1/users/current/summaries?range=last_7_days",
            ]
        )
    }

    @Test func authenticatesWithBasicAuth() async throws {
        respond(status: 200)
        _ = try await client.fetchSnapshot()

        let expected = "Basic \(Data("waka_test_key".utf8).base64EncodedString())"
        let headers = StubURLProtocol.requests.all.map { $0.value(forHTTPHeaderField: "Authorization") }
        #expect(headers == [expected, expected])
    }

    @Test func reportsAnInvalidAPIKey() async {
        respond(status: 401)
        await #expect(throws: WakaTimeClient.ClientError.unauthorized) {
            try await client.fetchSnapshot()
        }
    }

    @Test func reportsServerErrors() async {
        respond(status: 503)
        await #expect(throws: WakaTimeClient.ClientError.http(503)) {
            try await client.fetchSnapshot()
        }
    }
}
