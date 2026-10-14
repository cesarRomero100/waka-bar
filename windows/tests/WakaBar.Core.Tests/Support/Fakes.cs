using System.Collections.Concurrent;
using System.Net;

namespace WakaBar.Core.Tests.Support;

/// <summary>Answers every request with <paramref name="respond"/> and records it. The client sends two requests concurrently.</summary>
internal sealed class FakeHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> respond) : HttpMessageHandler
{
    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        var (status, body) = respond(request);
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}

/// <summary>A fixed "now", in UTC, so tests don't depend on the machine's clock or time zone.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
