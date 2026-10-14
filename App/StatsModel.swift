import AppKit
import Observation
import WidgetKit

@MainActor
@Observable
final class StatsModel {
    static let refreshInterval: Duration = .seconds(5 * 60)
    static let baseURLKey = "baseURL"
    private static var invalidURLMessage: String {
        String(localized: "Invalid server URL. It must start with https:// or http://.")
    }

    private(set) var snapshot: Snapshot? = SnapshotStore.load()
    private(set) var errorMessage: String?
    private(set) var isLoading = false
    private(set) var hasAPIKey = Keychain.apiKey != nil

    @ObservationIgnored private var loop: Task<Void, Never>?
    @ObservationIgnored private var wakeObserver: Task<Void, Never>?
    @ObservationIgnored private var refreshTask: Task<Void, Never>?

    init() {
        loop = Task { [weak self] in
            while !Task.isCancelled {
                await self?.refresh()
                // Suspending clock: time asleep doesn't count, so this doesn't also fire on wake.
                try? await Task.sleep(for: Self.refreshInterval, clock: .suspending)
            }
        }
        // After sleep the numbers can be hours old; refresh as soon as the Mac wakes.
        wakeObserver = Task { [weak self] in
            let wakes = NSWorkspace.shared.notificationCenter.notifications(named: NSWorkspace.didWakeNotification)
            for await _ in wakes {
                // Give Wi-Fi a moment to reconnect.
                try? await Task.sleep(for: .seconds(5))
                await self?.refresh()
            }
        }
    }

    var baseURL: String {
        UserDefaults.standard.string(forKey: Self.baseURLKey) ?? WakaTimeClient.defaultBaseURL
    }

    /// Web dashboard matching the configured server.
    var dashboardURL: URL? { WakaTimeClient.dashboardURL(forBaseURL: baseURL) }

    func save(apiKey: String, baseURL: String) async {
        let apiKey = apiKey.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedURL = baseURL.trimmingCharacters(in: .whitespacesAndNewlines)
        let newBaseURL = trimmedURL.isEmpty ? nil : trimmedURL
        if let newBaseURL, WakaTimeClient.baseURL(from: newBaseURL) == nil {
            errorMessage = Self.invalidURLMessage
            return
        }
        let keyChanged = apiKey != (Keychain.apiKey ?? "")
        let serverChanged = newBaseURL != UserDefaults.standard.string(forKey: Self.baseURLKey)

        do {
            try Keychain.setAPIKey(apiKey)
        } catch {
            errorMessage = error.localizedDescription
            return
        }
        UserDefaults.standard.set(newBaseURL, forKey: Self.baseURLKey)
        // The previous account's numbers must not stay on screen, even if the new ones fail to load.
        if keyChanged || serverChanged { clearSnapshot() }
        await refresh()
    }

    /// Fetches fresh stats. A new call cancels the one in flight, so an older response
    /// (e.g. for the previous API key) never overwrites a newer one.
    func refresh() async {
        refreshTask?.cancel()
        let task = Task { await load() }
        refreshTask = task
        isLoading = true
        await task.value
        guard refreshTask == task else { return }
        refreshTask = nil
        isLoading = false
    }

    private func load() async {
        guard let apiKey = Keychain.apiKey, !apiKey.isEmpty else {
            hasAPIKey = false
            return
        }
        hasAPIKey = true
        guard let url = WakaTimeClient.baseURL(from: baseURL) else {
            errorMessage = Self.invalidURLMessage
            return
        }

        do {
            let snapshot = try await WakaTimeClient(baseURL: url, apiKey: apiKey).fetchSnapshot()
            try Task.checkCancellation()
            let changed = self.snapshot.map { !$0.hasSameStats(as: snapshot) } ?? true
            self.snapshot = snapshot
            errorMessage = nil
            SnapshotStore.save(snapshot)
            // Widget reloads have a daily budget; don't spend it while nothing changes (idle, overnight).
            if changed { WidgetCenter.shared.reloadAllTimelines() }
        } catch {
            // Replaced by a newer refresh, which reports its own result.
            guard !Task.isCancelled else { return }
            errorMessage = error.localizedDescription
        }
    }

    private func clearSnapshot() {
        refreshTask?.cancel()
        snapshot = nil
        errorMessage = nil
        SnapshotStore.clear()
        WidgetCenter.shared.reloadAllTimelines()
    }
}
