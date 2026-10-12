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
                    if let hours = value.as(Double.self) { Text("\(Int(hours))h") }
                }
            }
        }
    }
}

/// Top languages with a proportional bar each.
struct LanguageList: View {
    let languages: [LanguageStat]
    var limit = 5

    var body: some View {
        let total = max(languages.reduce(0) { $0 + $1.seconds }, 1)
        VStack(alignment: .leading, spacing: 6) {
            ForEach(languages.prefix(limit)) { language in
                VStack(alignment: .leading, spacing: 2) {
                    HStack {
                        Text(language.name).lineLimit(1)
                        Spacer()
                        Text(Format.duration(language.seconds))
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
                                    .frame(width: geo.size.width * language.seconds / total)
                            }
                    }
                    .frame(height: 4)
                }
            }
        }
    }
}

/// Small labelled number, e.g. "Today / 3h 12m".
struct StatBlock: View {
    let title: String
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
