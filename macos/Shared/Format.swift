import Foundation

enum Format {
    /// 3h 12m, 45m, 2h
    static func duration(_ seconds: Double) -> String {
        let minutes = Int(seconds / 60)
        let h = minutes / 60
        let m = minutes % 60
        if h == 0 { return "\(m)m" }
        return m == 0 ? "\(h)h" : "\(h)h \(m)m"
    }
}
