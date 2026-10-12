import Foundation

/// Persists the latest snapshot in the App Group container shared by the app and the widget.
enum SnapshotStore {
    private static let fileName = "snapshot.json"

    private static var fileURL: URL? {
        guard
            let groupID = Bundle.main.object(forInfoDictionaryKey: "AppGroupID") as? String,
            let container = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: groupID)
        else { return nil }
        return container.appending(path: fileName)
    }

    static func load() -> Snapshot? {
        guard let url = fileURL, let data = try? Data(contentsOf: url) else { return nil }
        return try? JSONDecoder().decode(Snapshot.self, from: data)
    }

    static func save(_ snapshot: Snapshot) {
        guard let url = fileURL, let data = try? JSONEncoder().encode(snapshot) else { return }
        try? data.write(to: url, options: .atomic)
    }
}
