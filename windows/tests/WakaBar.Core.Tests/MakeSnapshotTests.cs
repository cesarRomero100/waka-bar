using System.Text.Json;
using WakaBar.Core.Tests.Support;
using Day = WakaBar.Core.Tests.Support.ApiJson.Day;

namespace WakaBar.Core.Tests;

/// <summary>Building a snapshot from API responses. Mirrors <c>MakeSnapshotTests</c> on macOS.</summary>
public class MakeSnapshotTests
{
    /// <summary>Hours 1...7 over the week, with languages and projects proportional to them.</summary>
    private static readonly List<Day> Week = ApiJson.Dates(7)
        .Select((date, index) => new Day(
            date,
            (index + 1) * 3600.0,
            new[] { ("Swift", (index + 1) * 2400.0), ("TypeScript", 1200.0), ("YAML", 5.0) },
            new[] { ("waka-bar", (index + 1) * 1800.0), ("dotfiles", 600.0) }))
        .ToList();

    [Fact]
    public void UsesStatusBarForToday()
    {
        Assert.Equal(11520, Make(11520, Week).TodaySeconds);
    }

    [Fact]
    public void ParsesTheSevenDaysInOrder()
    {
        var days = Make(0, Week).Days;
        Assert.Equal(ApiJson.Dates(7), days.Select(day => day.Date));
        Assert.Equal(3600, days[0].Seconds);
    }

    [Fact]
    public void ShowsOnlyTheLastSevenDaysOfALongerHistory()
    {
        var days = Make(0, ApiJson.History(Enumerable.Repeat(1.0, 30).ToArray())).Days;
        Assert.Equal(ApiJson.Dates(7), days.Select(day => day.Date));
    }

    [Fact]
    public void TodaysBarMatchesTheStatusBarTotal()
    {
        // Summaries says 7h for today, status_bar is fresher and says 7h 30m.
        Assert.Equal(7.5 * 3600, Make(7.5 * 3600, Week).Days[^1].Seconds);
    }

    [Fact]
    public void KeepsTheLastBarWhenItIsNotToday()
    {
        // Just after midnight: the week still ends yesterday.
        var snapshot = Make(600, Week, ApiJson.Now.AddDays(1));
        Assert.Equal(7 * 3600, snapshot.Days[^1].Seconds);
    }

    [Fact]
    public void FallsBackToConsecutiveDaysWithoutRangeDates()
    {
        var undated = Week.Select(day => new Day(null, day.Seconds));
        Assert.Equal(ApiJson.Dates(7), Make(0, undated).Days.Select(day => day.Date));
    }

    [Fact]
    public void SumsLanguagesAcrossTheWeekSortedByTime()
    {
        var languages = Make(0, Week).Languages;
        Assert.Equal(new[] { "Swift", "TypeScript" }, languages.Select(language => language.Name));
        Assert.Equal(Enumerable.Range(1, 7).Sum(i => i * 2400.0), languages[0].Seconds);
        Assert.Equal(7 * 1200, languages[1].Seconds);
    }

    [Fact]
    public void IgnoresLanguagesAndProjectsOlderThanAWeek()
    {
        var history = ApiJson.History(Enumerable.Repeat(1.0, 30).ToArray());
        history[0] = history[0] with
        {
            Languages = new[] { ("COBOL", 10.0 * 3600) },
            Projects = new[] { ("legacy", 10.0 * 3600) },
        };
        var snapshot = Make(0, history);
        Assert.Empty(snapshot.Languages);
        Assert.Empty(snapshot.Projects);
    }

    [Fact]
    public void DropsLanguagesUnderOneMinute()
    {
        Assert.DoesNotContain(Make(0, Week).Languages, language => language.Name == "YAML");
    }

    [Fact]
    public void ToleratesDaysWithoutLanguagesOrProjects()
    {
        var bare = Week.Select(day => new Day(day.Date, day.Seconds));
        var snapshot = Make(0, bare);
        Assert.Empty(snapshot.Languages);
        Assert.Empty(snapshot.Projects);
        Assert.Null(snapshot.TopProjectToday);
    }

    [Fact]
    public void SumsProjectsAcrossTheWeekSortedByTime()
    {
        var projects = Make(0, Week).Projects;
        Assert.Equal(new[] { "waka-bar", "dotfiles" }, projects.Select(project => project.Name));
        Assert.Equal(7 * 600, projects[1].Seconds);
    }

    [Fact]
    public void PicksTodaysTopProjectFromTheStatusBar()
    {
        var snapshot = WakaTimeClient.MakeSnapshot(
            ApiJson.StatusBar(3 * 3600, new[] { ("dotfiles", 1800.0), ("waka-bar", 9000.0), ("scratch", 30.0) }),
            ApiJson.Summaries(Week),
            ApiJson.Now,
            TimeZoneInfo.Utc);
        Assert.Equal(new UsageStat("waka-bar", 9000), snapshot.TopProjectToday);
    }

    [Fact]
    public void CountsTheStreakAcrossTheWholeHistory()
    {
        // 12 active days in a row, after a day off 13 days ago.
        var hours = Enumerable.Repeat(2.0, 17).Append(0).Concat(Enumerable.Repeat(1.0, 12)).ToArray();
        Assert.Equal(12, Make(3600, ApiJson.History(hours)).StreakDays);
    }

    [Fact]
    public void RejectsMalformedResponses()
    {
        Assert.Throws<JsonException>(() =>
            WakaTimeClient.MakeSnapshot("{}", ApiJson.Summaries(Week), ApiJson.Now, TimeZoneInfo.Utc));
    }

    private static Snapshot Make(double today, IEnumerable<Day> history, DateTimeOffset? now = null) =>
        WakaTimeClient.MakeSnapshot(ApiJson.StatusBar(today), ApiJson.Summaries(history), now ?? ApiJson.Now, TimeZoneInfo.Utc);
}
