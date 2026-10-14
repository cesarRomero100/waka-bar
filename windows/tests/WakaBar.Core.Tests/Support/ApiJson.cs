using System.Globalization;
using System.Text.Json.Nodes;

namespace WakaBar.Core.Tests.Support;

/// <summary>
/// Per-scenario API responses shaped like the real WakaTime payloads, including fields the app ignores.
/// Mirrors <c>macos/Tests/Support/Fixtures.swift</c>.
/// </summary>
internal static class ApiJson
{
    /// <summary>Friday 2026-10-09 15:00 UTC. Tests use the UTC time zone.</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Today = new(2026, 10, 9);

    /// <summary><paramref name="count"/> consecutive days ending today.</summary>
    public static IReadOnlyList<DateOnly> Dates(int count) =>
        Enumerable.Range(0, count).Select(offset => Today.AddDays(offset - (count - 1))).ToList();

    /// <summary>One day of a summaries response. Null lists are left out of the JSON.</summary>
    public sealed record Day(
        DateOnly? Date,
        double Seconds,
        IReadOnlyList<(string Name, double Seconds)>? Languages = null,
        IReadOnlyList<(string Name, double Seconds)>? Projects = null);

    /// <summary>Days ending today with the given hours of coding each, oldest first.</summary>
    public static List<Day> History(params double[] hours) =>
        Dates(hours.Length).Zip(hours, (date, h) => new Day(date, h * 3600)).ToList();

    /// <summary><c>GET users/current/status_bar/today</c></summary>
    public static string StatusBar(double seconds, IReadOnlyList<(string Name, double Seconds)>? projects = null)
    {
        var day = new Day(Today, seconds, new[] { ("Swift", seconds) }, projects ?? Array.Empty<(string, double)>());
        return new JsonObject
        {
            ["cached_at"] = "2026-10-09T15:00:00Z",
            ["has_team_features"] = false,
            ["data"] = DayJson(day),
        }.ToJsonString();
    }

    /// <summary><c>GET users/current/summaries?range=...</c></summary>
    public static string Summaries(IEnumerable<Day> days)
    {
        var list = days.ToList();
        var total = list.Sum(day => day.Seconds);
        return new JsonObject
        {
            ["data"] = new JsonArray(list.Select(day => (JsonNode?)DayJson(day)).ToArray()),
            ["cumulative_total"] = new JsonObject { ["seconds"] = total, ["text"] = "0 hrs" },
            ["daily_average"] = new JsonObject { ["seconds"] = total / Math.Max(list.Count, 1) },
            ["start"] = "2026-10-03T00:00:00Z",
            ["end"] = "2026-10-09T23:59:59Z",
        }.ToJsonString();
    }

    private static JsonObject DayJson(Day day)
    {
        var result = new JsonObject
        {
            ["grand_total"] = new JsonObject
            {
                ["total_seconds"] = day.Seconds,
                ["text"] = $"{(int)(day.Seconds / 3600)} hrs",
                ["digital"] = "0:00",
            },
            ["categories"] = Items(new[] { ("Coding", day.Seconds) }),
            ["editors"] = Items(new[] { ("Visual Studio", day.Seconds) }),
        };

        var range = new JsonObject { ["text"] = "Fri Oct 9th 2026", ["timezone"] = "UTC" };
        if (day.Date is { } date)
        {
            range["date"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        result["range"] = range;
        if (day.Languages is { } languages)
        {
            result["languages"] = Items(languages);
        }

        if (day.Projects is { } projects)
        {
            result["projects"] = Items(projects);
        }

        return result;
    }

    private static JsonArray Items(IEnumerable<(string Name, double Seconds)> items) =>
        new(items
            .Select(item => (JsonNode?)new JsonObject
            {
                ["name"] = item.Name,
                ["total_seconds"] = item.Seconds,
                ["percent"] = 0,
            })
            .ToArray());
}
