import Foundation

/// Minimal client for the WakaTime API. Also works with Wakapi
/// (base URL `https://<host>/api/compat/wakatime/v1`).
struct WakaTimeClient {
    static let defaultBaseURL = "https://api.wakatime.com/api/v1"

    var baseURL: URL
    var apiKey: String
    var session: URLSession = .shared

    enum ClientError: LocalizedError, Equatable {
        case unauthorized
        case http(Int)

        var errorDescription: String? {
            switch self {
            case .unauthorized: String(localized: "Invalid API key.")
            case .http(let code): String(localized: "Server returned HTTP \(code).")
            }
        }
    }

    func fetchSnapshot() async throws -> Snapshot {
        async let today = get("users/current/status_bar/today")
        async let week = get(
            "users/current/summaries",
            query: [URLQueryItem(name: "range", value: "last_7_days")]
        )
        return try await Self.makeSnapshot(today: today, week: week)
    }

    /// Builds a snapshot from raw `status_bar/today` and `summaries` responses. No network, so it is unit-testable.
    static func makeSnapshot(
        today todayJSON: Data,
        week weekJSON: Data,
        now: Date = .now,
        calendar: Calendar = .current
    ) throws -> Snapshot {
        let decoder = JSONDecoder()
        decoder.keyDecodingStrategy = .convertFromSnakeCase
        let todayData = try decoder.decode(StatusBarResponse.self, from: todayJSON).data
        let weekData = try decoder.decode(SummariesResponse.self, from: weekJSON).data
        let todaySeconds = todayData.grandTotal.totalSeconds

        let startOfToday = calendar.startOfDay(for: now)
        var days = weekData.enumerated().map { index, day in
            DayStat(
                date: day.range?.date.flatMap { parseDay($0, calendar: calendar) }
                    ?? calendar.date(byAdding: .day, value: index - (weekData.count - 1), to: startOfToday)!,
                seconds: day.grandTotal.totalSeconds
            )
        }
        // Summaries can lag behind status_bar; keep today's bar in sync with the menu bar number.
        if let last = days.indices.last, calendar.isDate(days[last].date, inSameDayAs: now) {
            days[last].seconds = todaySeconds
        }

        var languageTotals: [String: Double] = [:]
        for day in weekData {
            for language in day.languages ?? [] {
                languageTotals[language.name, default: 0] += language.totalSeconds
            }
        }
        let languages =
            languageTotals
            .map { LanguageStat(name: $0.key, seconds: $0.value) }
            .filter { $0.seconds >= 60 }
            .sorted { $0.seconds > $1.seconds }

        return Snapshot(updatedAt: now, todaySeconds: todaySeconds, days: days, languages: languages)
    }

    private func get(_ path: String, query: [URLQueryItem] = []) async throws -> Data {
        var url = baseURL.appending(path: path)
        // appending(queryItems: []) would still add an empty "?".
        if !query.isEmpty { url.append(queryItems: query) }
        var request = URLRequest(url: url)
        request.setValue("Basic \(Data(apiKey.utf8).base64EncodedString())", forHTTPHeaderField: "Authorization")

        let (data, response) = try await session.data(for: request)
        let status = (response as? HTTPURLResponse)?.statusCode ?? 0
        guard status != 401 else { throw ClientError.unauthorized }
        guard (200..<300).contains(status) else { throw ClientError.http(status) }
        return data
    }

    private static func parseDay(_ string: String, calendar: Calendar) -> Date? {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = calendar
        formatter.timeZone = calendar.timeZone
        formatter.dateFormat = "yyyy-MM-dd"
        return formatter.date(from: string)
    }
}

// MARK: - API payloads (only the fields we use)

private struct StatusBarResponse: Decodable {
    let data: APIDay
}

private struct SummariesResponse: Decodable {
    let data: [APIDay]
}

private struct APIDay: Decodable {
    struct Total: Decodable { let totalSeconds: Double }
    struct Item: Decodable {
        let name: String
        let totalSeconds: Double
    }
    struct Range: Decodable { let date: String? }

    let grandTotal: Total
    let languages: [Item]?
    let range: Range?
}
