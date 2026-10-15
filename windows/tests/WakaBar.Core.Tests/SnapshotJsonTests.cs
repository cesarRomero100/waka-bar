using WakaBar.Core.Tests.Support;

namespace WakaBar.Core.Tests;

public class SnapshotJsonTests
{
    [Fact]
    public void RoundTripsEveryField()
    {
        var fetched = WakaTimeClient.MakeSnapshot(
            ApiJson.StatusBar(11520, new[] { ("waka-bar", 7200.0) }),
            ApiJson.Summaries(ApiJson.History(1, 2, 3, 4, 5, 6, 7)),
            ApiJson.Now,
            TimeZoneInfo.Utc);
        var original = fetched with { DailyGoalSeconds = 4 * 3600 };

        Assert.Equal(original, SnapshotJson.Deserialize(SnapshotJson.Serialize(original)));
    }

    [Fact]
    public void RoundTripsAnEmptySnapshot()
    {
        var empty = new Snapshot
        {
            UpdatedAt = ApiJson.Now,
            TodaySeconds = 0,
            Days = EquatableArray<DayStat>.Empty,
        };

        Assert.Equal(empty, SnapshotJson.Deserialize(SnapshotJson.Serialize(empty)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"UpdatedAt":"2026-10-09T15:00:00+00:00","TodaySeconds":1,"Days":{}}""")]
    public void ReturnsNullForMissingOrCorruptData(string? json)
    {
        Assert.Null(SnapshotJson.Deserialize(json));
    }
}
