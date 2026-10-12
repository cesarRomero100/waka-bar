import Foundation

/// Everything the menu bar and the widget display. The app fetches it,
/// the widget only reads the copy saved in the shared App Group container.
struct Snapshot: Codable, Equatable {
    var updatedAt: Date
    var todaySeconds: Double
    /// Last 7 days, oldest first. The last element is today.
    var days: [DayStat]
    /// Languages over the last 7 days, most used first.
    var languages: [LanguageStat]

    var weekSeconds: Double { days.reduce(0) { $0 + $1.seconds } }
    var dailyAverage: Double { days.isEmpty ? 0 : weekSeconds / Double(days.count) }

    /// Today's total, or 0 if the snapshot was taken on an earlier day.
    func todaySeconds(asOf now: Date = .now) -> Double {
        Calendar.current.isDate(updatedAt, inSameDayAs: now) ? todaySeconds : 0
    }
}

struct DayStat: Codable, Equatable, Identifiable {
    var date: Date
    var seconds: Double
    var id: Date { date }
}

struct LanguageStat: Codable, Equatable, Identifiable {
    var name: String
    var seconds: Double
    var id: String { name }
}

extension Snapshot {
    static let sample: Snapshot = {
        let calendar = Calendar.current
        let today = calendar.startOfDay(for: .now)
        let hours: [Double] = [3.2, 5.1, 4.4, 1.0, 6.3, 2.7, 3.5]
        let days = hours.enumerated().map { index, h in
            DayStat(
                date: calendar.date(byAdding: .day, value: index - (hours.count - 1), to: today)!,
                seconds: h * 3600
            )
        }
        return Snapshot(
            updatedAt: .now,
            todaySeconds: 3.5 * 3600,
            days: days,
            languages: [
                LanguageStat(name: "Swift", seconds: 11 * 3600),
                LanguageStat(name: "TypeScript", seconds: 8 * 3600),
                LanguageStat(name: "Python", seconds: 4 * 3600),
                LanguageStat(name: "Markdown", seconds: 2 * 3600),
                LanguageStat(name: "YAML", seconds: 1 * 3600),
            ]
        )
    }()
}
