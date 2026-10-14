import Foundation
import Testing

struct SnapshotTests {
    private func snapshot(days hours: [Double], today: Double = 0, updatedAt: Date = .now) -> Snapshot {
        Snapshot(
            updatedAt: updatedAt,
            todaySeconds: today,
            days: hours.enumerated().map {
                DayStat(date: Date(timeIntervalSince1970: Double($0) * 86400), seconds: $1 * 3600)
            },
            languages: []
        )
    }

    @Test func weekTotalAndDailyAverage() {
        let snapshot = snapshot(days: [1, 2, 3, 4, 5, 6, 7])
        #expect(snapshot.weekSeconds == 28 * 3600)
        #expect(snapshot.dailyAverage == 4 * 3600)
    }

    @Test func dailyAverageIsZeroWithoutDays() {
        #expect(snapshot(days: []).dailyAverage == 0)
    }

    @Test func todayIsKeptOnTheSameDay() {
        let now = Date.now
        #expect(snapshot(days: [], today: 5400, updatedAt: now).todaySeconds(asOf: now) == 5400)
    }

    @Test func todayResetsOnTheNextDay() {
        let now = Date.now
        let tomorrow = Calendar.current.date(byAdding: .day, value: 1, to: now)!
        #expect(snapshot(days: [], today: 5400, updatedAt: now).todaySeconds(asOf: tomorrow) == 0)
    }

    @Test func sameStatsIgnoresWhenTheyWereFetched() {
        let earlier = snapshot(days: [1, 2, 3], today: 5400, updatedAt: .now.addingTimeInterval(-300))
        #expect(earlier.hasSameStats(as: snapshot(days: [1, 2, 3], today: 5400)))
        #expect(!earlier.hasSameStats(as: snapshot(days: [1, 2, 3], today: 5460)))
        #expect(!earlier.hasSameStats(as: snapshot(days: [1, 2, 4], today: 5400)))
    }

    @Test func goalStreakAndProjectChangesCount() {
        let base = snapshot(days: [1, 2, 3], today: 5400)
        var goal = base
        goal.dailyGoalSeconds = 4 * 3600
        var streak = base
        streak.streakDays = 5
        var project = base
        project.topProjectToday = UsageStat(name: "waka-bar", seconds: 3600)
        for changed in [goal, streak, project] {
            #expect(!base.hasSameStats(as: changed))
        }
    }

    @Test func roundTripsThroughJSON() throws {
        let original = Snapshot.sample
        let decoded = try JSONDecoder().decode(Snapshot.self, from: JSONEncoder().encode(original))
        #expect(decoded == original)
    }
}

struct StreakTests {
    /// Days ending today with the given hours each, oldest first.
    private func streak(_ hours: [Double]) -> Int {
        let days = hours.enumerated().map { index, h in
            DayStat(
                date: Fixtures.calendar.date(byAdding: .day, value: index - (hours.count - 1), to: Fixtures.now)!,
                seconds: h * 3600
            )
        }
        return Snapshot.streak(in: days, now: Fixtures.now, calendar: Fixtures.calendar)
    }

    @Test func countsConsecutiveActiveDaysEndingToday() {
        #expect(streak([0, 1, 2, 3]) == 3)
    }

    @Test func aQuietTodayDoesNotBreakTheStreakYet() {
        #expect(streak([1, 1, 1, 0]) == 3)
    }

    @Test func aDayOffBreaksTheStreak() {
        #expect(streak([1, 1, 0, 1, 1]) == 2)
    }

    @Test func lessThanAMinuteDoesNotCount() {
        #expect(streak([1, 1, 30.0 / 3600, 1]) == 1)
    }

    @Test func noActivityMeansNoStreak() {
        #expect(streak([0, 0, 0]) == 0)
        #expect(streak([]) == 0)
    }

    @Test func coversTheWholeHistory() {
        #expect(streak(Array(repeating: 1, count: 30)) == 30)
    }
}
