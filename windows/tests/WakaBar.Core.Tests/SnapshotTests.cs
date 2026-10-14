using WakaBar.Core.Tests.Support;

namespace WakaBar.Core.Tests;

public class SnapshotTests
{
    [Fact]
    public void WeekTotalAndDailyAverage()
    {
        var snapshot = Make(1, 2, 3, 4, 5, 6, 7);
        Assert.Equal(28 * 3600, snapshot.WeekSeconds);
        Assert.Equal(4 * 3600, snapshot.DailyAverage);
    }

    [Fact]
    public void DailyAverageIsZeroWithoutDays()
    {
        Assert.Equal(0, Make().DailyAverage);
    }

    [Fact]
    public void TodayIsKeptOnTheSameDay()
    {
        var snapshot = Make() with { TodaySeconds = 5400 };
        Assert.Equal(5400, snapshot.TodaySecondsAsOf(ApiJson.Now, TimeZoneInfo.Utc));
    }

    [Fact]
    public void TodayResetsOnTheNextDay()
    {
        var snapshot = Make() with { TodaySeconds = 5400 };
        Assert.Equal(0, snapshot.TodaySecondsAsOf(ApiJson.Now.AddDays(1), TimeZoneInfo.Utc));
    }

    [Fact]
    public void SameStatsIgnoresWhenTheyWereFetched()
    {
        var earlier = Make(1, 2, 3) with { UpdatedAt = ApiJson.Now.AddMinutes(-5) };
        Assert.True(earlier.HasSameStats(Make(1, 2, 3)));
        Assert.False(earlier.HasSameStats(Make(1, 2, 4)));
    }

    [Fact]
    public void GoalStreakAndProjectChangesCount()
    {
        var baseline = Make(1, 2, 3);
        Assert.False(baseline.HasSameStats(baseline with { DailyGoalSeconds = 4 * 3600 }));
        Assert.False(baseline.HasSameStats(baseline with { StreakDays = 5 }));
        Assert.False(baseline.HasSameStats(baseline with { TopProjectToday = new UsageStat("waka-bar", 3600) }));
        Assert.False(baseline.HasSameStats(baseline with
        {
            Projects = new EquatableArray<UsageStat>(new[] { new UsageStat("waka-bar", 3600) }),
        }));
    }

    [Fact]
    public void EqualListsMakeEqualSnapshots()
    {
        // Two separately built snapshots: equality must compare the lists' items, not references.
        Assert.Equal(Make(1, 2, 3), Make(1, 2, 3));
    }

    /// <summary>A snapshot with the given hours per day, oldest first, ending today.</summary>
    private static Snapshot Make(params double[] hours) => new()
    {
        UpdatedAt = ApiJson.Now,
        TodaySeconds = 0,
        Days = new EquatableArray<DayStat>(
            ApiJson.Dates(hours.Length).Zip(hours, (date, h) => new DayStat(date, h * 3600))),
    };
}

public class StreakTests
{
    [Fact]
    public void CountsConsecutiveActiveDaysEndingToday()
    {
        Assert.Equal(3, Streak(0, 1, 2, 3));
    }

    [Fact]
    public void AQuietTodayDoesNotBreakTheStreakYet()
    {
        Assert.Equal(3, Streak(1, 1, 1, 0));
    }

    [Fact]
    public void ADayOffBreaksTheStreak()
    {
        Assert.Equal(2, Streak(1, 1, 0, 1, 1));
    }

    [Fact]
    public void LessThanAMinuteDoesNotCount()
    {
        Assert.Equal(1, Streak(1, 1, 30.0 / 3600, 1));
    }

    [Fact]
    public void NoActivityMeansNoStreak()
    {
        Assert.Equal(0, Streak(0, 0, 0));
        Assert.Equal(0, Streak());
    }

    [Fact]
    public void CoversTheWholeHistory()
    {
        Assert.Equal(30, Streak(Enumerable.Repeat(1.0, 30).ToArray()));
    }

    /// <summary>Streak for the given hours per day, oldest first, ending today.</summary>
    private static int Streak(params double[] hours) =>
        Snapshot.Streak(
            ApiJson.Dates(hours.Length).Zip(hours, (date, h) => new DayStat(date, h * 3600)).ToList(),
            ApiJson.Today);
}

public class DurationTextTests
{
    [Theory]
    [InlineData(0.0, "0m")]
    [InlineData(59.0, "0m")]
    [InlineData(60.0, "1m")]
    [InlineData(2700.0, "45m")]
    [InlineData(3600.0, "1h")]
    [InlineData(3660.0, "1h 1m")]
    [InlineData(11520.0, "3h 12m")]
    [InlineData(36000.0, "10h")]
    public void FormatsSecondsAsHoursAndMinutes(double seconds, string expected)
    {
        Assert.Equal(expected, DurationText.Format(seconds));
    }
}
