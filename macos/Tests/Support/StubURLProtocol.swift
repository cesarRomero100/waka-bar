import Foundation

/// Intercepts every request of a URLSession built with `StubURLProtocol.session()`.
/// Tests using it must be `.serialized`: the handler is shared.
final class StubURLProtocol: URLProtocol {
    nonisolated(unsafe) static var handler: ((URLRequest) -> (status: Int, body: Data))?
    static let requests = RequestLog()

    static func session() -> URLSession {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StubURLProtocol.self]
        return URLSession(configuration: configuration)
    }

    override class func canInit(with request: URLRequest) -> Bool { true }

    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        Self.requests.append(request)
        let (status, body) = Self.handler?(request) ?? (500, Data())
        let response = HTTPURLResponse(url: request.url!, statusCode: status, httpVersion: nil, headerFields: nil)!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: body)
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}
}

/// Thread-safe list of requests; the client sends two of them concurrently.
final class RequestLog: @unchecked Sendable {
    private let lock = NSLock()
    private var items: [URLRequest] = []

    var all: [URLRequest] { lock.withLock { items } }

    func append(_ request: URLRequest) { lock.withLock { items.append(request) } }

    func removeAll() { lock.withLock { items.removeAll() } }
}
