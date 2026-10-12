import Foundation

/// Minimal client for the WakaTime API. Also works with Wakapi
/// (base URL `https://<host>/api/compat/wakatime/v1`).
struct WakaTimeClient {
    static let defaultBaseURL = "https://api.wakatime.com/api/v1"

    var baseURL: URL
    var apiKey: String

    enum ClientError: LocalizedError {
        case unauthorized
        case http(Int)

        var errorDescription: String? {
            switch self {
            case .unauthorized: "Invalid API key."
            case .http(let code): "Server returned HTTP \(code)."
            }
        }
    }

    func fetchSnapshot() async throws -> Snapshot {
        async let today: StatusBarResponse = get("users/current/status_bar/today")
        async let week: SummariesResponse = get(
            "users/current/summaries",
            query: [URLQueryItem(name: "range", value: "last_7_days")]
        )
        let (todayData, weekData) = try await (today.data, week.data)
        let todaySeconds = todayData.grandTotal.totalSeconds

        let calendar = Calendar.current
        let startOfToday = calendar.startOfDay(for: .now)
        var days = weekData.enumerated().map { index, day in
            DayStat(
                date: day.range?.date.flatMap(Self.parseDay)
                    ?? calendar.date(byAdding: .day, value: index - (weekData.count - 1), to: startOfToday)!,
                seconds: day.grandTotal.totalSeconds
            )
        }
        // Summaries can lag behind status_bar; keep today's bar in sync with the menu bar number.
        if let last = days.indices.last, calendar.isDateInToday(days[last].date) {
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

        return Snapshot(updatedAt: .now, todaySeconds: todaySeconds, days: days, languages: languages)
    }

    private func get<T: Decodable>(_ path: String, query: [URLQueryItem] = []) async throws -> T {
        var request = URLRequest(url: baseURL.appending(path: path).appending(queryItems: query))
        request.setValue("Basic \(Data(apiKey.utf8).base64EncodedString())", forHTTPHeaderField: "Authorization")

        let (data, response) = try await URLSession.shared.data(for: request)
        let status = (response as? HTTPURLResponse)?.statusCode ?? 0
        guard status != 401 else { throw ClientError.unauthorized }
        guard (200..<300).contains(status) else { throw ClientError.http(status) }

        let decoder = JSONDecoder()
        decoder.keyDecodingStrategy = .convertFromSnakeCase
        return try decoder.decode(T.self, from: data)
    }

    private static func parseDay(_ string: String) -> Date? {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = Calendar.current
        formatter.timeZone = .current
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
