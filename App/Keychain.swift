import Foundation
import Security

/// Stores the API key in the user's keychain instead of UserDefaults.
enum Keychain {
    private static let service = "WakaBar"
    private static let account = "apiKey"

    static var apiKey: String? {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: account,
            kSecReturnData: true,
            kSecMatchLimit: kSecMatchLimitOne,
        ]
        var result: CFTypeRef?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
            let data = result as? Data
        else { return nil }
        return String(decoding: data, as: UTF8.self)
    }

    static func setAPIKey(_ key: String?) {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: account,
        ]
        SecItemDelete(query as CFDictionary)
        guard let key, !key.isEmpty else { return }
        var item = query
        item[kSecValueData] = Data(key.utf8)
        SecItemAdd(item as CFDictionary, nil)
    }
}
