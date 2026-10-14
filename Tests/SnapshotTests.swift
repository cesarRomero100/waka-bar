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

    @Test func roundTripsThroughJSON() throws {
        let original = Snapshot.sample
        let decoded = try JSONDecoder().decode(Snapshot.self, from: JSONEncoder().encode(original))
        #expect(decoded == original)
    }
}
