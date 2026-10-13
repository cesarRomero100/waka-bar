import SwiftUI

/// Popover shown when clicking the menu bar item.
struct MenuContentView: View {
    let model: StatsModel
    @Environment(\.openSettings) private var openSettings
    @Environment(\.openURL) private var openURL

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            if !model.hasAPIKey {
                ContentUnavailableView {
                    Label("No API key", systemImage: "key")
                } description: {
                    Text("Add your WakaTime API key to see your stats.")
                } actions: {
                    Button("Open Settings…", action: showSettings)
                }
            } else if let snapshot = model.snapshot {
                stats(snapshot)
            } else {
                ProgressView().frame(maxWidth: .infinity, minHeight: 120)
            }

            if let error = model.errorMessage {
                Label(error, systemImage: "exclamationmark.triangle")
                    .font(.caption)
                    .foregroundStyle(.orange)
            }

            Divider()
            footer
        }
        .padding(14)
        .frame(width: 300)
    }

    @ViewBuilder
    private func stats(_ snapshot: Snapshot) -> some View {
        HStack(alignment: .firstTextBaseline) {
            StatBlock(title: "Today", seconds: snapshot.todaySeconds(), prominent: true)
            Spacer()
            VStack(alignment: .trailing, spacing: 4) {
                StatBlock(title: "Last 7 days", seconds: snapshot.weekSeconds)
                Text("avg \(Format.duration(snapshot.dailyAverage))/day")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }

        WeekChart(days: snapshot.days)
            .frame(height: 110)

        if !snapshot.languages.isEmpty {
            Text("Languages")
                .font(.subheadline.bold())
            LanguageList(languages: snapshot.languages)
        }
    }

    private var footer: some View {
        HStack {
            if let updated = model.snapshot?.updatedAt {
                Text("Updated \(updated.formatted(.relative(presentation: .named)))")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }
            Spacer()
            Button {
                Task { await model.refresh() }
            } label: {
                Image(systemName: "arrow.clockwise")
            }
            .disabled(model.isLoading)
            .help("Refresh")

            Menu {
                if let url = model.dashboardURL {
                    Button("Open Dashboard") { openURL(url) }
                }
                Button("Settings…", action: showSettings)
                Divider()
                Button("Quit WakaBar") { NSApplication.shared.terminate(nil) }
            } label: {
                Image(systemName: "ellipsis.circle")
            }
            .menuStyle(.borderlessButton)
            .menuIndicator(.hidden)
            .fixedSize()
        }
        .buttonStyle(.borderless)
    }

    private func showSettings() {
        // Menu bar-only apps are never frontmost; bring the Settings window forward.
        NSApplication.shared.activate()
        openSettings()
    }
}

#Preview {
    MenuContentView(model: StatsModel())
}
