import Foundation

/// API responses shaped like the real WakaTime payloads, including fields the app ignores.
enum Fixtures {
    /// UTC calendar so tests don't depend on the machine's time zone.
    static let calendar: Calendar = {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "UTC")!
        return calendar
    }()

    /// Friday 2026-10-09 15:00 UTC.
    static let now = ISO8601DateFormatter().date(from: "2026-10-09T15:00:00Z")!

    /// Start of the given `yyyy-MM-dd` day in UTC.
    static func day(_ ymd: String) -> Date {
        ISO8601DateFormatter().date(from: "\(ymd)T00:00:00Z")!
    }

    struct Day {
        var date: String?
        var seconds: Double
        var languages: [(name: String, seconds: Double)]? = []
    }

    /// `GET users/current/status_bar/today`
    static func statusBar(seconds: Double, date: String = "2026-10-09") -> Data {
        json([
            "cached_at": "\(date)T15:00:00Z",
            "has_team_features": false,
            "data": payload(Day(date: date, seconds: seconds, languages: [("Swift", seconds)])),
        ])
    }

    /// `GET users/current/summaries?range=last_7_days`
    static func summaries(_ days: [Day]) -> Data {
        let total = days.reduce(0) { $0 + $1.seconds }
        return json([
            "data": days.map(payload),
            "cumulative_total": ["seconds": total, "text": "\(Int(total / 3600)) hrs", "decimal": "0.00"],
            "daily_average": ["seconds": total / Double(max(days.count, 1)), "days_including_holidays": days.count],
            "start": "2026-10-03T00:00:00Z",
            "end": "2026-10-09T23:59:59Z",
        ])
    }

    /// The seven days ending today (2026-10-03 ... 2026-10-09).
    static let weekDates = (3...9).map { String(format: "2026-10-%02d", $0) }

    private static func payload(_ day: Day) -> [String: Any] {
        var result: [String: Any] = [
            "grand_total": [
                "total_seconds": day.seconds,
                "text": "\(Int(day.seconds / 3600)) hrs",
                "digital": "0:00",
                "hours": Int(day.seconds / 3600),
                "minutes": Int(day.seconds / 60) % 60,
            ],
            "categories": [["name": "Coding", "total_seconds": day.seconds]],
            "editors": [["name": "Xcode", "total_seconds": day.seconds]],
        ]
        var range: [String: Any] = ["text": "Fri Oct 9th 2026", "timezone": "UTC"]
        if let date = day.date {
            range["date"] = date
            range["start"] = "\(date)T00:00:00Z"
        }
        result["range"] = range
        if let languages = day.languages {
            result["languages"] = languages.map { ["name": $0.name, "total_seconds": $0.seconds, "percent": 0] }
        }
        return result
    }

    private static func json(_ object: [String: Any]) -> Data {
        try! JSONSerialization.data(withJSONObject: object)
    }
}
