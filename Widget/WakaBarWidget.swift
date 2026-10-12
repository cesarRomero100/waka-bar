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
        let snapshot = context.isPreview ? .sample : SnapshotStore.load() ?? .sample
        completion(StatsEntry(date: .now, snapshot: snapshot))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<StatsEntry>) -> Void) {
        let entry = StatsEntry(date: .now, snapshot: SnapshotStore.load())
        // Fallback in case the app is not running; also resets "today" after midnight.
        let next = Date.now.addingTimeInterval(15 * 60)
        completion(Timeline(entries: [entry], policy: .after(next)))
    }
}

@main
struct WakaBarWidget: Widget {
    var body: some WidgetConfiguration {
        StaticConfiguration(kind: "WakaBarWidget", provider: StatsProvider()) { entry in
            StatsWidgetView(entry: entry)
                .containerBackground(.fill.tertiary, for: .widget)
        }
        .configurationDisplayName("Coding Time")
        .description("Today, last 7 days and top languages from WakaTime.")
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

    private func small(_ snapshot: Snapshot) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            StatBlock(title: "Today", seconds: snapshot.todaySeconds(asOf: entry.date), prominent: true)
            StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
            Spacer(minLength: 0)
            if let top = snapshot.languages.first {
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
                StatBlock(title: "Today", seconds: snapshot.todaySeconds(asOf: entry.date), prominent: true)
                StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
            }
            LanguageList(languages: snapshot.languages, limit: 3)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
    }

    private func large(_ snapshot: Snapshot) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(alignment: .firstTextBaseline) {
                StatBlock(title: "Today", seconds: snapshot.todaySeconds(asOf: entry.date), prominent: true)
                Spacer()
                StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
            }
            WeekChart(days: snapshot.days)
                .frame(maxHeight: .infinity)
            LanguageList(languages: snapshot.languages, limit: 4)
        }
    }
}

#Preview(as: .systemMedium) {
    WakaBarWidget()
} timeline: {
    StatsEntry(date: .now, snapshot: .sample)
}
