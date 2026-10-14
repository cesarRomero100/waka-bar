using WakaBar.Core.Tests.Support;

namespace WakaBar.Core.Tests;

/// <summary>
/// Parses the API responses in <c>shared/fixtures/</c>, which the macOS tests parse too.
/// Expected values are listed in <c>shared/fixtures/README.md</c>.
/// </summary>
public class ApiContractTests
{
    private readonly Snapshot snapshot = WakaTimeClient.MakeSnapshot(
        Fixture("status_bar_today.json"),
        Fixture("summaries_last_7_days.json"),
        ApiJson.Now,
        TimeZoneInfo.Utc);

    [Fact]
    public void Today()
    {
        Assert.Equal(11520, snapshot.TodaySeconds);
        Assert.Equal(new UsageStat("waka-bar", 7200), snapshot.TopProjectToday);
    }

    [Fact]
    public void Week()
    {
        Assert.Equal(ApiJson.Dates(7), snapshot.Days.Select(day => day.Date));
        // Summaries lag behind status_bar for today.
        Assert.Equal(11520, snapshot.Days[^1].Seconds);
        Assert.Equal(92520, snapshot.WeekSeconds);
    }

    [Fact]
    public void Languages()
    {
        UsageStat[] expected = [new("Swift", 54600), new("TypeScript", 27300), new("Markdown", 9100)];
        Assert.Equal(expected, snapshot.Languages);
    }

    [Fact]
    public void Projects()
    {
        UsageStat[] expected = [new("waka-bar", 45500), new("marketplace", 36400), new("dotfiles", 9100)];
        Assert.Equal(expected, snapshot.Projects);
    }

    [Fact]
    public void Streak()
    {
        Assert.Equal(7, snapshot.StreakDays);
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", name));
}
