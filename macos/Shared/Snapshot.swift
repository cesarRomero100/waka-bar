import Foundation

/// Everything the menu bar and the widget display. The app fetches it,
/// the widget only reads the copy saved in the shared App Group container.
struct Snapshot: Codable, Equatable {
    var updatedAt: Date
    var todaySeconds: Double
    /// Last 7 days, oldest first. The last element is today.
    var days: [DayStat]
    /// Languages over the last 7 days, most used first.
    var languages: [UsageStat]
    /// Projects over the last 7 days, most used first.
    var projects: [UsageStat] = []
    /// Most worked-on project today.
    var topProjectToday: UsageStat? = nil
    /// Consecutive active days up to today, within the fetched history (up to 30 days).
    var streakDays = 0
    /// Daily goal from Settings, nil when off. Stored here so the widget sees it too.
    var dailyGoalSeconds: Double? = nil

    var weekSeconds: Double { days.reduce(0) { $0 + $1.seconds } }
    var dailyAverage: Double { days.isEmpty ? 0 : weekSeconds / Double(days.count) }

    /// Same numbers, regardless of when they were fetched. Compares every field but `updatedAt`,
    /// so new fields (goal, streak, projects...) are covered without touching this.
    func hasSameStats(as other: Snapshot) -> Bool {
        var other = other
        other.updatedAt = updatedAt
        return self == other
    }

    /// Today's total, or 0 if the snapshot was taken on an earlier day.
    func todaySeconds(asOf now: Date = .now) -> Double {
        Calendar.current.isDate(updatedAt, inSameDayAs: now) ? todaySeconds : 0
    }

    /// Counts consecutive days with at least `minimumSeconds` of coding, ending today.
    /// A quiet today doesn't break the streak yet: it then counts up to yesterday.
    static func streak(
        in days: [DayStat],
        now: Date = .now,
        calendar: Calendar = .current,
        minimumSeconds: Double = 60
    ) -> Int {
        var remaining = days[...]
        if let last = remaining.last, calendar.isDate(last.date, inSameDayAs: now), last.seconds < minimumSeconds {
            remaining = remaining.dropLast()
        }
        var count = 0
        for day in remaining.reversed() {
            guard day.seconds >= minimumSeconds else { break }
            count += 1
        }
        return count
    }
}

struct DayStat: Codable, Equatable, Identifiable {
    var date: Date
    var seconds: Double
    var id: Date { date }
}

/// Time spent on a language or a project.
struct UsageStat: Codable, Equatable, Identifiable {
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
                UsageStat(name: "Swift", seconds: 11 * 3600),
                UsageStat(name: "TypeScript", seconds: 8 * 3600),
                UsageStat(name: "Python", seconds: 4 * 3600),
                UsageStat(name: "Markdown", seconds: 2 * 3600),
                UsageStat(name: "YAML", seconds: 1 * 3600),
            ],
            projects: [
                UsageStat(name: "waka-bar", seconds: 14 * 3600),
                UsageStat(name: "marketplace", seconds: 9 * 3600),
                UsageStat(name: "dotfiles", seconds: 3 * 3600),
            ],
            topProjectToday: UsageStat(name: "waka-bar", seconds: 2.5 * 3600),
            streakDays: 12,
            dailyGoalSeconds: 4 * 3600
        )
    }()
}
