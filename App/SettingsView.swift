import SwiftUI

struct SettingsView: View {
    let model: StatsModel
    @State private var apiKey = Keychain.apiKey ?? ""
    @State private var baseURL = UserDefaults.standard.string(forKey: StatsModel.baseURLKey) ?? ""
    @State private var saving = false

    var body: some View {
        Form {
            Section {
                SecureField("API key", text: $apiKey, prompt: Text("waka_…"))
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
    }
}
