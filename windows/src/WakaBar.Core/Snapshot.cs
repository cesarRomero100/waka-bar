namespace WakaBar.Core;

/// <summary>
/// Everything the tray app and the widget display.
/// Mirrors <c>macos/Shared/Snapshot.swift</c>; keep both in sync.
/// </summary>
public sealed record Snapshot
{
    public required DateTimeOffset UpdatedAt { get; init; }

    public required double TodaySeconds { get; init; }

    /// <summary>Last 7 days, oldest first. The last element is today.</summary>
    public required EquatableArray<DayStat> Days { get; init; }

    /// <summary>Languages over the last 7 days, most used first.</summary>
    public EquatableArray<UsageStat> Languages { get; init; } = EquatableArray<UsageStat>.Empty;

    /// <summary>Projects over the last 7 days, most used first.</summary>
    public EquatableArray<UsageStat> Projects { get; init; } = EquatableArray<UsageStat>.Empty;

    /// <summary>Most worked-on project today.</summary>
    public UsageStat? TopProjectToday { get; init; }

    /// <summary>Consecutive active days up to today, within the fetched history (up to 30 days).</summary>
    public int StreakDays { get; init; }

    /// <summary>Daily goal from Settings, null when off.</summary>
    public double? DailyGoalSeconds { get; init; }

    public double WeekSeconds => Days.Sum(day => day.Seconds);

    public double DailyAverage => Days.Count == 0 ? 0 : WeekSeconds / Days.Count;

    /// <summary>Same numbers, regardless of when they were fetched. Compares every field but <see cref="UpdatedAt"/>.</summary>
    public bool HasSameStats(Snapshot other) => this with { UpdatedAt = other.UpdatedAt } == other;

    /// <summary>Today's total, or 0 if the snapshot was taken on an earlier day.</summary>
    public double TodaySecondsAsOf(DateTimeOffset now, TimeZoneInfo zone) =>
        LocalDate(UpdatedAt, zone) == LocalDate(now, zone) ? TodaySeconds : 0;

    /// <summary>
    /// Counts consecutive days with at least <paramref name="minimumSeconds"/> of coding, ending today.
    /// A quiet today doesn't break the streak yet: it then counts up to yesterday.
    /// </summary>
    public static int Streak(IReadOnlyList<DayStat> days, DateOnly today, double minimumSeconds = 60)
    {
        var end = days.Count;
        if (end > 0 && days[end - 1].Date == today && days[end - 1].Seconds < minimumSeconds)
        {
            end--;
        }

        var count = 0;
        for (var i = end - 1; i >= 0 && days[i].Seconds >= minimumSeconds; i--)
        {
            count++;
        }

        return count;
    }

    /// <summary>The calendar day of <paramref name="time"/> in <paramref name="zone"/>.</summary>
    public static DateOnly LocalDate(DateTimeOffset time, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time, zone).DateTime);
}

public readonly record struct DayStat(DateOnly Date, double Seconds);

/// <summary>Time spent on a language or a project.</summary>
public sealed record UsageStat(string Name, double Seconds);
