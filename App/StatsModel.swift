import Foundation
import Observation
import WidgetKit

@MainActor
@Observable
final class StatsModel {
    static let refreshInterval: Duration = .seconds(5 * 60)
    static let baseURLKey = "baseURL"

    private(set) var snapshot: Snapshot? = SnapshotStore.load()
    private(set) var errorMessage: String?
    private(set) var isLoading = false
    private(set) var hasAPIKey = Keychain.apiKey != nil

    @ObservationIgnored private var loop: Task<Void, Never>?

    init() {
        loop = Task { [weak self] in
            while !Task.isCancelled {
                await self?.refresh()
                try? await Task.sleep(for: Self.refreshInterval)
            }
        }
    }

    var baseURL: String {
        UserDefaults.standard.string(forKey: Self.baseURLKey) ?? WakaTimeClient.defaultBaseURL
    }

    /// Web dashboard matching the configured server.
    var dashboardURL: URL? {
        guard let components = URLComponents(string: baseURL), let host = components.host else { return nil }
        if host.hasSuffix("wakatime.com") { return URL(string: "https://wakatime.com/dashboard") }
        return URL(string: "\(components.scheme ?? "https")://\(host)")
    }

    func save(apiKey: String, baseURL: String) async {
        Keychain.setAPIKey(apiKey.trimmingCharacters(in: .whitespacesAndNewlines))
        let trimmed = baseURL.trimmingCharacters(in: .whitespacesAndNewlines)
        UserDefaults.standard.set(trimmed.isEmpty ? nil : trimmed, forKey: Self.baseURLKey)
        await refresh()
    }

    func refresh() async {
        guard let apiKey = Keychain.apiKey, !apiKey.isEmpty else {
            hasAPIKey = false
            return
        }
        hasAPIKey = true
        guard let url = URL(string: baseURL) else {
            errorMessage = "Invalid server URL."
            return
        }

        isLoading = true
        defer { isLoading = false }
        do {
            let snapshot = try await WakaTimeClient(baseURL: url, apiKey: apiKey).fetchSnapshot()
            self.snapshot = snapshot
            errorMessage = nil
            SnapshotStore.save(snapshot)
            WidgetCenter.shared.reloadAllTimelines()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
