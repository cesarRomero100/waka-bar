using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WakaBar.Core;

/// <summary>
/// Minimal client for the WakaTime API. Also works with Wakapi
/// (base URL <c>https://&lt;host&gt;/api/compat/wakatime/v1</c>).
/// Mirrors <c>macos/App/WakaTimeClient.swift</c>; keep both in sync.
/// </summary>
public sealed class WakaTimeClient
{
    public const string DefaultBaseUrl = "https://api.wakatime.com/api/v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
    };

    private readonly HttpClient http;
    private readonly Uri baseUrl;
    private readonly string apiKey;
    private readonly TimeProvider time;

    public WakaTimeClient(HttpClient http, Uri baseUrl, string apiKey, TimeProvider? time = null)
    {
        this.http = http;
        this.baseUrl = baseUrl;
        this.apiKey = apiKey;
        this.time = time ?? TimeProvider.System;
    }

    /// <summary>
    /// The server URL from Settings, if it is an absolute http(s) URL with a host.
    /// Rejects <c>wakapi.example.com</c> (no scheme), which HttpClient would fail on with a vague error.
    /// </summary>
    public static Uri? ParseBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var isHttp = uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
        return isHttp && !string.IsNullOrEmpty(uri.Host) ? uri : null;
    }

    /// <summary>
    /// Web dashboard for an API base URL: wakatime.com's, or the root of a self-hosted server.
    /// For Wakapi that is everything before <c>/api</c>, keeping the port and any path prefix:
    /// <c>http://localhost:3000/api/compat/wakatime/v1</c> → <c>http://localhost:3000/</c>.
    /// </summary>
    public static Uri? DashboardUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return null;
        }

        if (uri.Host == "wakatime.com" || uri.Host.EndsWith(".wakatime.com", StringComparison.Ordinal))
        {
            return new Uri("https://wakatime.com/dashboard");
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var apiIndex = Array.IndexOf(segments, "api");
        var prefix = apiIndex >= 0 ? segments[..apiIndex] : Array.Empty<string>();
        var path = "/" + string.Concat(prefix.Select(segment => segment + "/"));
        return new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port, path).Uri;
    }

    public async Task<Snapshot> FetchSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var today = GetAsync("users/current/status_bar/today", range: null, cancellationToken);
        var history = FetchHistoryAsync(cancellationToken);
        await Task.WhenAll(today, history).ConfigureAwait(false);
        return MakeSnapshot(await today.ConfigureAwait(false), await history.ConfigureAwait(false), time.GetUtcNow(), time.LocalTimeZone);
    }

    /// <summary>
    /// Builds a snapshot from raw <c>status_bar/today</c> and <c>summaries</c> responses.
    /// No network, so it is unit-testable.
    /// </summary>
    /// <exception cref="JsonException">A response is malformed or misses required fields.</exception>
    public static Snapshot MakeSnapshot(string todayJson, string historyJson, DateTimeOffset now, TimeZoneInfo zone)
    {
        var todayData = Deserialize<StatusBarResponse>(todayJson).Data;
        var history = Deserialize<SummariesResponse>(historyJson).Data;
        var todaySeconds = todayData.GrandTotal.TotalSeconds;
        var today = Snapshot.LocalDate(now, zone);

        var days = history
            .Select((day, index) => new DayStat(
                ParseDay(day.Range?.Date) ?? today.AddDays(index - (history.Count - 1)),
                day.GrandTotal.TotalSeconds))
            .ToList();
        // Summaries can lag behind status_bar; keep today's bar in sync with the tray number.
        if (days.Count > 0 && days[^1].Date == today)
        {
            days[^1] = days[^1] with { Seconds = todaySeconds };
        }

        var week = history.TakeLast(7).ToList();
        return new Snapshot
        {
            UpdatedAt = now,
            TodaySeconds = todaySeconds,
            Days = new EquatableArray<DayStat>(days.TakeLast(7)),
            Languages = Totals(week.Select(day => day.Languages)),
            Projects = Totals(week.Select(day => day.Projects)),
            TopProjectToday = Totals(new[] { todayData.Projects }).FirstOrDefault(),
            StreakDays = Snapshot.Streak(days, today),
        };
    }

    /// <summary>
    /// 30 days so the streak can go past a week. Falls back to 7 days if the server refuses the
    /// longer range (e.g. plan limits); an invalid key still fails right away.
    /// </summary>
    private async Task<string> FetchHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await GetAsync("users/current/summaries", "last_30_days", cancellationToken).ConfigureAwait(false);
        }
        catch (WakaTimeApiException error) when (!error.IsUnauthorized)
        {
            return await GetAsync("users/current/summaries", "last_7_days", cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> GetAsync(string path, string? range, CancellationToken cancellationToken)
    {
        var url = baseUrl.AbsoluteUri.TrimEnd('/') + "/" + path;
        if (range is not null)
        {
            url += "?range=" + Uri.EscapeDataString(range);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new WakaTimeApiException((int)response.StatusCode);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sums items by name across days, drops anything under a minute, most used first.</summary>
    private static EquatableArray<UsageStat> Totals(IEnumerable<IReadOnlyList<ApiItem>?> days) =>
        new(days
            .SelectMany(items => items ?? Array.Empty<ApiItem>())
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Select(group => new UsageStat(group.Key, group.Sum(item => item.TotalSeconds)))
            .Where(usage => usage.Seconds >= 60)
            .OrderByDescending(usage => usage.Seconds)
            .ThenBy(usage => usage.Name, StringComparer.Ordinal));

    private static DateOnly? ParseDay(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new JsonException($"Empty {typeof(T).Name}.");

    // API payloads: only the fields we use. Missing required fields throw JsonException.

    private sealed class StatusBarResponse
    {
        public required ApiDay Data { get; init; }
    }

    private sealed class SummariesResponse
    {
        public required IReadOnlyList<ApiDay> Data { get; init; }
    }

    private sealed class ApiDay
    {
        public required ApiTotal GrandTotal { get; init; }

        public IReadOnlyList<ApiItem>? Languages { get; init; }

        public IReadOnlyList<ApiItem>? Projects { get; init; }

        public ApiRange? Range { get; init; }
    }

    private sealed class ApiTotal
    {
        public required double TotalSeconds { get; init; }
    }

    private sealed class ApiItem
    {
        public required string Name { get; init; }

        public required double TotalSeconds { get; init; }
    }

    private sealed class ApiRange
    {
        public string? Date { get; init; }
    }
}
