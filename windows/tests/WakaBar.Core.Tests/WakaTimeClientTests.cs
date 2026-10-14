using System.Net;
using System.Text;
using WakaBar.Core.Tests.Support;

namespace WakaBar.Core.Tests;

/// <summary>HTTP requests. Mirrors <c>WakaTimeClientHTTPTests</c> on macOS.</summary>
public class WakaTimeClientTests
{
    [Fact]
    public async Task RequestsTodayAndTheLastThirtyDays()
    {
        var (client, handler) = Client(HttpStatusCode.OK);
        await client.FetchSnapshotAsync();
        Assert.Equal(
            new[]
            {
                "https://example.com/api/v1/users/current/status_bar/today",
                "https://example.com/api/v1/users/current/summaries?range=last_30_days",
            },
            RequestedUrls(handler));
    }

    [Fact]
    public async Task FallsBackToSevenDaysWhenThirtyAreRefused()
    {
        var (client, handler) = Client(HttpStatusCode.OK, thirtyDays: HttpStatusCode.PaymentRequired);
        var snapshot = await client.FetchSnapshotAsync();
        Assert.Equal(7, snapshot.Days.Count);
        Assert.Contains("https://example.com/api/v1/users/current/summaries?range=last_7_days", RequestedUrls(handler));
    }

    [Fact]
    public async Task AuthenticatesWithBasicAuth()
    {
        var (client, handler) = Client(HttpStatusCode.OK);
        await client.FetchSnapshotAsync();
        var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("waka_test_key"));
        Assert.All(handler.Requests, request => Assert.Equal(expected, request.Headers.Authorization?.ToString()));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task UsesTheInjectedClock()
    {
        var (client, _) = Client(HttpStatusCode.OK);
        Assert.Equal(ApiJson.Now, (await client.FetchSnapshotAsync()).UpdatedAt);
    }

    [Fact]
    public async Task ReportsAnInvalidApiKeyWithoutFallingBack()
    {
        var (client, handler) = Client(HttpStatusCode.Unauthorized);
        var error = await Assert.ThrowsAsync<WakaTimeApiException>(() => client.FetchSnapshotAsync());
        Assert.True(error.IsUnauthorized);
        Assert.DoesNotContain(RequestedUrls(handler), url => url.EndsWith("range=last_7_days", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReportsServerErrors()
    {
        var (client, _) = Client(HttpStatusCode.ServiceUnavailable);
        var error = await Assert.ThrowsAsync<WakaTimeApiException>(() => client.FetchSnapshotAsync());
        Assert.Equal(503, error.StatusCode);
    }

    /// <summary>
    /// A client whose server answers every request with <paramref name="status"/>,
    /// except <c>range=last_30_days</c>, answered with <paramref name="thirtyDays"/> when given.
    /// </summary>
    private static (WakaTimeClient Client, FakeHandler Handler) Client(HttpStatusCode status, HttpStatusCode? thirtyDays = null)
    {
        var handler = new FakeHandler(request =>
        {
            var url = request.RequestUri!;
            var code = thirtyDays is { } refused && url.Query.Contains("range=last_30_days", StringComparison.Ordinal)
                ? refused
                : status;
            if (code != HttpStatusCode.OK)
            {
                return (code, "");
            }

            var body = url.AbsolutePath.EndsWith("status_bar/today", StringComparison.Ordinal)
                ? ApiJson.StatusBar(3600)
                : ApiJson.Summaries(ApiJson.History(1, 1, 1, 1, 1, 1, 1));
            return (HttpStatusCode.OK, body);
        });
        var client = new WakaTimeClient(
            new HttpClient(handler),
            new Uri("https://example.com/api/v1"),
            "waka_test_key",
            new FixedTimeProvider(ApiJson.Now));
        return (client, handler);
    }

    private static List<string> RequestedUrls(FakeHandler handler) =>
        handler.Requests.Select(request => request.RequestUri!.AbsoluteUri).Order(StringComparer.Ordinal).ToList();
}

public class UrlTests
{
    [Theory]
    [InlineData("https://api.wakatime.com/api/v1", "https://wakatime.com/dashboard")]
    [InlineData("https://wakatime.com/api/v1", "https://wakatime.com/dashboard")]
    [InlineData("https://wakapi.dev/api/compat/wakatime/v1", "https://wakapi.dev/")]
    [InlineData("http://localhost:3000/api/compat/wakatime/v1", "http://localhost:3000/")]
    [InlineData("https://example.com/wakapi/api/compat/wakatime/v1", "https://example.com/wakapi/")]
    [InlineData("https://notwakatime.com/api/compat/wakatime/v1", "https://notwakatime.com/")]
    [InlineData("https://user:pass@example.com:8443/api/v1?x=1#top", "https://example.com:8443/")]
    [InlineData("not a url", null)]
    [InlineData("", null)]
    public void OpensTheDashboardOfTheConfiguredServer(string baseUrl, string? dashboard)
    {
        Assert.Equal(dashboard, WakaTimeClient.DashboardUrl(baseUrl)?.AbsoluteUri);
    }

    [Theory]
    [InlineData("https://api.wakatime.com/api/v1")]
    [InlineData("http://localhost:3000/api/compat/wakatime/v1")]
    [InlineData("HTTPS://wakapi.dev/api/compat/wakatime/v1")]
    public void AcceptsAbsoluteHttpUrls(string value)
    {
        Assert.NotNull(WakaTimeClient.ParseBaseUrl(value));
    }

    [Theory]
    [InlineData("wakapi.example.com/api/compat/wakatime/v1")]
    [InlineData("localhost:3000/api/compat/wakatime/v1")]
    [InlineData("ftp://example.com/api/v1")]
    [InlineData("https://")]
    [InlineData("not a url")]
    [InlineData("")]
    public void RejectsUrlsWithoutAnHttpSchemeOrAHost(string value)
    {
        Assert.Null(WakaTimeClient.ParseBaseUrl(value));
    }
}
