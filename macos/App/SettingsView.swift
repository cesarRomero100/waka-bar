import SwiftUI

struct SettingsView: View {
    /// Daily goal choices, in hours.
    static let goalHours: [Double] = [0.5, 1, 1.5, 2, 3, 4, 5, 6, 8]

    let model: StatsModel
    @State private var apiKey = Keychain.apiKey ?? ""
    @State private var baseURL = UserDefaults.standard.string(forKey: StatsModel.baseURLKey) ?? ""
    @State private var saving = false
    @State private var launchAtLogin = LoginItem.isEnabled
    @State private var loginItemError: String?

    var body: some View {
        Form {
            Section {
                // Custom binding: the toggle always reflects the real system state, even if registering fails.
                Toggle(
                    "Launch at login",
                    isOn: Binding(
                        get: { launchAtLogin },
                        set: { enabled in
                            do {
                                try LoginItem.setEnabled(enabled)
                                loginItemError = nil
                            } catch {
                                loginItemError = error.localizedDescription
                            }
                            launchAtLogin = LoginItem.isEnabled
                        }
                    )
                )
                if LoginItem.requiresApproval {
                    HStack {
                        Text("Allow WakaBar in Login Items to launch at login.")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                        Spacer()
                        Button("Open Login Items") { LoginItem.openSystemSettings() }
                            .controlSize(.small)
                    }
                }
                if let loginItemError {
                    Text(loginItemError)
                        .font(.caption)
                        .foregroundStyle(.orange)
                }
                Picker(
                    "Daily goal",
                    selection: Binding(get: { model.dailyGoal }, set: { model.setDailyGoal($0) })
                ) {
                    Text("Off").tag(0.0)
                    ForEach(Self.goalHours, id: \.self) { hours in
                        Text(verbatim: Format.duration(hours * 3600)).tag(hours * 3600)
                    }
                }
            }
            Section {
                SecureField("API key", text: $apiKey, prompt: Text(verbatim: "waka_…"))
                Link("Get your API key", destination: URL(string: "https://wakatime.com/settings/api-key")!)
                    .font(.caption)
            }
            Section {
                TextField("Server URL", text: $baseURL, prompt: Text(WakaTimeClient.defaultBaseURL))
                Text("Leave empty for WakaTime. For Wakapi use https://<host>/api/compat/wakatime/v1")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            if let error = model.errorMessage {
                Label(error, systemImage: "exclamationmark.triangle")
                    .foregroundStyle(.orange)
            }
            HStack {
                Spacer()
                if saving { ProgressView().controlSize(.small) }
                Button("Save") {
                    saving = true
                    Task {
                        await model.save(apiKey: apiKey, baseURL: baseURL)
                        saving = false
                    }
                }
                .keyboardShortcut(.defaultAction)
                .disabled(saving)
            }
        }
        .formStyle(.grouped)
        .frame(width: 420)
        .fixedSize(horizontal: false, vertical: true)
        // The user may have changed it in System Settings while this window was closed.
        .onAppear { launchAtLogin = LoginItem.isEnabled }
    }
}
