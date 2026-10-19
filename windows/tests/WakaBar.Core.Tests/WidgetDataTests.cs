using System.Globalization;
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

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-MX");

    /// <summary>English unless a test asks otherwise, whatever the machine's language.</summary>
    private static JsonObject Build(Snapshot? snapshot, bool hasApiKey = true, string? error = null, CultureInfo? culture = null) =>
        WidgetData.Build(snapshot, hasApiKey, error, ApiJson.Now, TimeZoneInfo.Utc, culture ?? English);

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
        var data = WidgetData.Build(Sample(), true, null, ApiJson.Now.AddDays(1), TimeZoneInfo.Utc, English);
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
    public void HasUsageWhenThereIsActivity()
    {
        var data = Build(Sample());
        Assert.True(data["hasUsage"]!.GetValue<bool>());
        Assert.Equal("", data["emptyMessage"]!.GetValue<string>());
    }

    [Fact]
    public void EncouragesCodingWhenTheWeekIsEmpty()
    {
        var empty = new Snapshot
        {
            UpdatedAt = ApiJson.Now,
            TodaySeconds = 0,
            Days = new EquatableArray<DayStat>(ApiJson.Dates(7).Select(date => new DayStat(date, 0))),
        };

        var data = Build(empty);
        Assert.False(data["hasUsage"]!.GetValue<bool>());
        Assert.Contains(data["emptyMessage"]!.GetValue<string>(), WidgetData.EmptyWeekMessages(English));
    }

    [Fact]
    public void ListsAtMostFourLanguages()
    {
        var snapshot = Sample() with
        {
            Languages = new EquatableArray<UsageStat>(
                Enumerable.Range(1, 6).Select(i => new UsageStat($"Language {i}", 3600 * (7 - i)))),
        };

        Assert.Equal(4, Build(snapshot)["languages"]!.AsArray().Count);
    }

    [Fact]
    public void SpeaksSpanish()
    {
        var data = Build(Sample(goal: 5 * 3600), culture: Spanish);
        Assert.Equal("Hoy", data["text"]!["today"]!.GetValue<string>());
        Assert.Equal("prom. 2h 10m/día", data["average"]!.GetValue<string>());
        Assert.Equal("Faltan 1h 48m", data["goalText"]!.GetValue<string>());
        Assert.Equal("🔥 Racha de 7 días", data["streak"]!.GetValue<string>());
        Assert.Equal("Cargando…", Build(null, culture: Spanish)["status"]!.GetValue<string>());
    }

    [Fact]
    public void UsesTheSingularForAOneDayStreak()
    {
        var oneDay = Sample() with { StreakDays = 1 };
        Assert.Equal("🔥 Racha de 1 día", Build(oneDay, culture: Spanish)["streak"]!.GetValue<string>());
        Assert.Equal("🔥 1-day streak", Build(oneDay)["streak"]!.GetValue<string>());
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
        var expressions = Expressions().Matches(WidgetTemplate.Json)
            .Select(match => QuotedText().Replace(match.Groups[1].Value, ""))
            .ToList();

        // ${text.key}: the key must be one of the translated template texts.
        var texts = Build(Sample())["text"]!.AsObject().Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        var unknownTexts = expressions
            .SelectMany(expression => TextKey().Matches(expression).Select(match => match.Groups[1].Value))
            .Where(key => !texts.Contains(key))
            .ToList();
        Assert.Empty(unknownTexts);

        var unknown = expressions
            .Select(expression => TextKey().Replace(expression, "text"))
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

    [GeneratedRegex(@"\btext\.(\w+)")]
    private static partial Regex TextKey();

    [GeneratedRegex(@"\b[A-Za-z_]\w*\b")]
    private static partial Regex Identifier();
}
