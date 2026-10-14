import SwiftUI
import WidgetKit

struct StatsEntry: TimelineEntry {
    let date: Date
    let snapshot: Snapshot?
}

/// The widget never calls the API: it reads the snapshot the menu bar app saves
/// in the App Group and gets reloaded by the app after each refresh.
struct StatsProvider: TimelineProvider {
    func placeholder(in context: Context) -> StatsEntry {
        StatsEntry(date: .now, snapshot: .sample)
    }

    func getSnapshot(in context: Context, completion: @escaping (StatsEntry) -> Void) {
        // Sample data only in the widget gallery; otherwise the same view as the timeline.
        let snapshot: Snapshot? = context.isPreview ? .sample : SnapshotStore.load()
        completion(StatsEntry(date: .now, snapshot: snapshot))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<StatsEntry>) -> Void) {
        let now = Date.now
        let snapshot = SnapshotStore.load()
        // Resets "Today" at midnight even if WidgetKit runs the reload below late.
        let calendar = Calendar.current
        let midnight = calendar.date(byAdding: .day, value: 1, to: calendar.startOfDay(for: now))!
        let entries = [StatsEntry(date: now, snapshot: snapshot), StatsEntry(date: midnight, snapshot: snapshot)]
        // Fallback in case the app is not running.
        let next = now.addingTimeInterval(15 * 60)
        completion(Timeline(entries: entries, policy: .after(next)))
    }
}

@main
struct WakaBarWidget: Widget {
    var body: some WidgetConfiguration {
        StaticConfiguration(kind: "WakaBarWidget", provider: StatsProvider()) { entry in
            StatsWidgetView(entry: entry)
                .containerBackground(.fill.tertiary, for: .widget)
                .widgetURL(DeepLink.dashboard)
        }
        .configurationDisplayName("Coding Time")
        .description("Today, your goal and streak, and top languages from WakaTime.")
        .supportedFamilies([.systemSmall, .systemMedium, .systemLarge])
    }
}

struct StatsWidgetView: View {
    let entry: StatsEntry
    @Environment(\.widgetFamily) private var family

    var body: some View {
        if let snapshot = entry.snapshot {
            switch family {
            case .systemSmall: small(snapshot)
            case .systemMedium: medium(snapshot)
            default: large(snapshot)
            }
        } else {
            VStack(spacing: 6) {
                Image(systemName: "chart.bar.fill").font(.title2)
                Text("Open WakaBar and add your API key.")
                    .font(.caption)
                    .multilineTextAlignment(.center)
                    .foregroundStyle(.secondary)
            }
        }
    }

    /// Today's total, with the goal bar when a goal is set.
    @ViewBuilder
    private func today(_ snapshot: Snapshot, showsGoalLabel: Bool = true) -> some View {
        let seconds = snapshot.todaySeconds(asOf: entry.date)
        VStack(alignment: .leading, spacing: 4) {
            StatBlock(title: "Today", seconds: seconds, prominent: true)
            if let goal = snapshot.dailyGoalSeconds {
                GoalProgress(seconds: seconds, goal: goal, showsLabel: showsGoalLabel)
            }
        }
    }

    private func small(_ snapshot: Snapshot) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            today(snapshot, showsGoalLabel: false)
            StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
            Spacer(minLength: 0)
            if snapshot.streakDays > 0 {
                StreakLabel(days: snapshot.streakDays)
                    .font(.caption)
                    .lineLimit(1)
            } else if let top = snapshot.languages.first {
                Label(top.name, systemImage: "chevron.left.forwardslash.chevron.right")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
    }

    private func medium(_ snapshot: Snapshot) -> some View {
        HStack(alignment: .top, spacing: 16) {
            VStack(alignment: .leading, spacing: 8) {
                today(snapshot)
                StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
                if snapshot.streakDays > 0 {
                    StreakLabel(days: snapshot.streakDays)
                        .font(.caption)
                        .lineLimit(1)
                }
            }
            .frame(width: 130, alignment: .leading)
            UsageList(items: snapshot.languages, limit: 3)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
    }

    private func large(_ snapshot: Snapshot) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(alignment: .top) {
                today(snapshot)
                    .frame(width: 150, alignment: .leading)
                Spacer()
                StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
            }
            HStack {
                if snapshot.streakDays > 0 {
                    StreakLabel(days: snapshot.streakDays)
                }
                Spacer()
                if let project = snapshot.topProjectToday {
                    Label(project.name, systemImage: "folder")
                        .lineLimit(1)
                }
            }
            .font(.caption)
            WeekChart(days: snapshot.days)
                .frame(maxHeight: .infinity)
            UsageList(items: snapshot.languages, limit: 4)
        }
    }
}

#Preview(as: .systemMedium) {
    WakaBarWidget()
} timeline: {
    StatsEntry(date: .now, snapshot: .sample)
}
