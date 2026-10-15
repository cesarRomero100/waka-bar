using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WakaBar.Core.Tests.Support;

namespace WakaBar.Core.Tests;

public partial class WidgetDataTests
{
    /// <summary>Today 3h 12m (status_bar), 2h on each of the other days.</summary>
    private static Snapshot Sample(double? goal = null)
    {
        var snapshot = WakaTimeClient.MakeSnapshot(
            ApiJson.StatusBar(3 * 3600 + 12 * 60, new[] { ("waka-bar", 7200.0) }),
            ApiJson.Summaries(ApiJson.Dates(7).Select(date => new ApiJson.Day(
                date,
                2 * 3600,
                new[] { ("Swift", 3600.0), ("C#", 1800.0) },
                new[] { ("waka-bar", 3600.0) }))),
            ApiJson.Now,
            TimeZoneInfo.Utc);
        return snapshot with { DailyGoalSeconds = goal };
    }

    private static JsonObject Build(Snapshot? snapshot, bool hasApiKey = true, string? error = null) =>
        WidgetData.Build(snapshot, hasApiKey, error, ApiJson.Now, TimeZoneInfo.Utc);

    [Fact]
    public void AsksForTheApiKey()
    {
        var data = Build(Sample(), hasApiKey: false);
        Assert.True(data["needsKey"]!.GetValue<bool>());
        Assert.False(data["hasData"]!.GetValue<bool>());
    }

    [Fact]
    public void ShowsLoadingOrTheErrorWithoutData()
    {
        Assert.Equal("Loading…", Build(null)["status"]!.GetValue<string>());
        Assert.Equal("Invalid API key.", Build(null, error: "Invalid API key.")["status"]!.GetValue<string>());
    }

    [Fact]
    public void FormatsTodayAndTheWeek()
    {
        var data = Build(Sample());
        Assert.True(data["hasData"]!.GetValue<bool>());
        Assert.Equal("3h 12m", data["today"]!.GetValue<string>());
        Assert.Equal("15h 12m", data["week"]!.GetValue<string>());
        Assert.Equal("avg 2h 10m/day", data["average"]!.GetValue<string>());
    }

    [Fact]
    public void TodayResetsOnTheNextDay()
    {
        var data = WidgetData.Build(Sample(), true, null, ApiJson.Now.AddDays(1), TimeZoneInfo.Utc);
        Assert.Equal("0m", data["today"]!.GetValue<string>());
    }

    [Fact]
    public void ShowsWhatIsLeftOfTheGoal()
    {
        var data = Build(Sample(goal: 5 * 3600));
        Assert.True(data["hasGoal"]!.GetValue<bool>());
        Assert.False(data["goalReached"]!.GetValue<bool>());
        Assert.Equal("1h 48m to go", data["goalText"]!.GetValue<string>());
        Assert.Equal(11520.0 / 18000, data["goalProgress"]!.GetValue<double>(), 6);
    }

    [Fact]
    public void ShowsAReachedGoal()
    {
        var data = Build(Sample(goal: 2 * 3600));
        Assert.True(data["goalReached"]!.GetValue<bool>());
        Assert.Equal("Goal reached ✓", data["goalText"]!.GetValue<string>());
        Assert.Equal(1, data["goalProgress"]!.GetValue<double>());
    }

    [Fact]
    public void HidesTheGoalWhenOff()
    {
        Assert.False(Build(Sample())["hasGoal"]!.GetValue<bool>());
    }

    [Fact]
    public void ListsStreakProjectAndUsage()
    {
        var data = Build(Sample());
        Assert.Equal("🔥 7-day streak", data["streak"]!.GetValue<string>());
        Assert.Equal("📁 waka-bar · 2h", data["project"]!.GetValue<string>());
        Assert.Equal("Swift", data["languages"]![0]!["name"]!.GetValue<string>());
        Assert.Equal("7h", data["languages"]![0]!["time"]!.GetValue<string>());
        Assert.Single(data["projects"]!.AsArray());
    }

    [Fact]
    public void TemplateIsAnAdaptiveCard()
    {
        var template = JsonNode.Parse(WidgetTemplate.Json)!;
        Assert.Equal("AdaptiveCard", template["type"]!.GetValue<string>());
        Assert.Equal("1.6", template["version"]!.GetValue<string>());
    }

    /// <summary>
    /// A misspelled field doesn't fail in Adaptive Cards: the widget just shows nothing. So every name the
    /// template binds must be produced by <see cref="WidgetData"/>, the app (images) or a list item.
    /// </summary>
    [Fact]
    public void TemplateOnlyBindsFieldsThatExist()
    {
        var known = Build(Sample(goal: 3600)).Select(field => field.Key)
            .Concat(WidgetData.AppImageKeys)
            .Concat(["name", "time"])
            .ToHashSet(StringComparer.Ordinal);
        string[] functions = ["if", "count"];

        var unknown = Expressions().Matches(WidgetTemplate.Json)
            .Select(match => QuotedText().Replace(match.Groups[1].Value, ""))
            .Select(expression => HostProperty().Replace(expression, ""))
            .SelectMany(expression => Identifier().Matches(expression).Select(identifier => identifier.Value))
            .Where(identifier => !functions.Contains(identifier) && !known.Contains(identifier))
            .Distinct()
            .ToList();

        Assert.Empty(unknown);
    }

    [GeneratedRegex(@"\$\{(.*?)\}")]
    private static partial Regex Expressions();

    [GeneratedRegex(@"'[^']*'")]
    private static partial Regex QuotedText();

    [GeneratedRegex(@"\$host\.\w+")]
    private static partial Regex HostProperty();

    [GeneratedRegex(@"\b[A-Za-z_]\w*\b")]
    private static partial Regex Identifier();
}
