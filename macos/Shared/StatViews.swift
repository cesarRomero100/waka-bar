import Charts
import SwiftUI

/// Bar chart of the last 7 days. Today is highlighted.
struct WeekChart: View {
    let days: [DayStat]

    var body: some View {
        Chart(days) { day in
            BarMark(
                x: .value("Day", day.date, unit: .day),
                y: .value("Hours", day.seconds / 3600)
            )
            .cornerRadius(3)
            .foregroundStyle(
                Calendar.current.isDateInToday(day.date) ? Color.accentColor : Color.accentColor.opacity(0.45))
        }
        .chartXAxis {
            AxisMarks(values: .stride(by: .day)) {
                AxisValueLabel(format: .dateTime.weekday(.narrow), centered: true)
            }
        }
        .chartYAxis {
            AxisMarks(position: .leading) { value in
                AxisGridLine()
                AxisValueLabel {
                    if let hours = value.as(Double.self) { Text(verbatim: "\(Int(hours))h") }
                }
            }
        }
    }
}

/// Top languages or projects, each with a bar proportional to its share.
struct UsageList: View {
    let items: [UsageStat]
    var limit = 5

    var body: some View {
        let total = max(items.reduce(0) { $0 + $1.seconds }, 1)
        VStack(alignment: .leading, spacing: 6) {
            ForEach(items.prefix(limit)) { item in
                VStack(alignment: .leading, spacing: 2) {
                    HStack {
                        Text(item.name).lineLimit(1)
                        Spacer()
                        Text(Format.duration(item.seconds))
                            .foregroundStyle(.secondary)
                            .monospacedDigit()
                    }
                    .font(.caption)
                    GeometryReader { geo in
                        Capsule()
                            .fill(.quaternary)
                            .overlay(alignment: .leading) {
                                Capsule()
                                    .fill(Color.accentColor)
                                    .frame(width: geo.size.width * item.seconds / total)
                            }
                    }
                    .frame(height: 4)
                }
            }
        }
    }
}

/// Progress towards the daily goal; turns green once reached.
struct GoalProgress: View {
    let seconds: Double
    let goal: Double
    /// The small widget only has room for the bar.
    var showsLabel = true

    var body: some View {
        let reached = seconds >= goal
        VStack(alignment: .leading, spacing: 3) {
            ProgressView(value: min(seconds / goal, 1))
                .tint(reached ? .green : .accentColor)
            if showsLabel {
                Group {
                    if reached {
                        Label("Goal reached", systemImage: "checkmark.circle.fill")
                    } else {
                        Text("\(Format.duration(goal - seconds)) to go")
                    }
                }
                .font(.caption2)
                .foregroundStyle(reached ? .green : .secondary)
            }
        }
    }
}

/// "🔥 12-day streak"
struct StreakLabel: View {
    let days: Int

    var body: some View {
        Label {
            Text("\(days)-day streak")
        } icon: {
            Image(systemName: "flame.fill").foregroundStyle(.orange)
        }
    }
}

/// Small labelled number, e.g. "Today / 3h 12m".
struct StatBlock: View {
    let title: LocalizedStringKey
    let seconds: Double
    var prominent = false

    var body: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text(title)
                .font(.caption)
                .foregroundStyle(.secondary)
            Text(Format.duration(seconds))
                .font(prominent ? .title.bold() : .headline)
                .monospacedDigit()
        }
    }
}
