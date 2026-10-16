using System.Text.Json.Nodes;

namespace WakaBar.Core;

/// <summary>
/// The data bound to <see cref="WidgetTemplate"/> for the Widgets Board. Platform-independent so it is
/// unit-tested; the app adds the chart and goal bar images (<see cref="AppImageKeys"/>).
/// </summary>
public static class WidgetData
{
    /// <summary>Keys the app sets with rendered images (PNG data URIs).</summary>
    public static readonly IReadOnlyList<string> AppImageKeys = ["appIcon", "chartLight", "chartDark", "goalImage"];

    /// <summary>
    /// Shown on the large widget when there's no activity this week, instead of empty lists.
    /// One per day, so it changes now and then without flickering between updates.
    /// </summary>
    public static readonly IReadOnlyList<string> EmptyWeekMessages =
    [
        "Nothing tracked this week yet. Open your editor and ship a commit: some open-source project is waiting for you. 🚀",
        "Your keyboard misses you. Pick an issue, write some code, and watch these bars grow. ⌨️",
        "Zero lines so far. The best time to start coding was yesterday; the second best is now. 💡",
    ];

    public static JsonObject Build(Snapshot? snapshot, bool hasApiKey, string? error, DateTimeOffset now, TimeZoneInfo zone)
    {
        var data = new JsonObject
        {
            ["needsKey"] = !hasApiKey,
            ["hasData"] = hasApiKey && snapshot is not null,
            ["status"] = error ?? "Loading…",
        };
        if (!hasApiKey || snapshot is null)
        {
            return data;
        }

        var today = snapshot.TodaySecondsAsOf(now, zone);
        data["today"] = DurationText.Format(today);
        data["week"] = DurationText.Format(snapshot.WeekSeconds);
        data["average"] = $"avg {DurationText.Format(snapshot.DailyAverage)}/day";

        var goal = snapshot.DailyGoalSeconds is { } seconds and > 0 ? seconds : 0;
        var reached = goal > 0 && today >= goal;
        data["hasGoal"] = goal > 0;
        data["goalReached"] = reached;
        data["goalProgress"] = goal > 0 ? Math.Clamp(today / goal, 0, 1) : 0;
        data["goalText"] = goal <= 0 ? "" : reached ? "Goal reached ✓" : $"{DurationText.Format(goal - today)} to go";

        data["hasStreak"] = snapshot.StreakDays > 0;
        data["streak"] = snapshot.StreakDays > 0 ? $"🔥 {snapshot.StreakDays}-day streak" : "";
        data["hasProject"] = snapshot.TopProjectToday is not null;
        data["project"] = snapshot.TopProjectToday is { } project
            ? $"📁 {project.Name} · {DurationText.Format(project.Seconds)}"
            : "";

        data["languages"] = Usage(snapshot.Languages);
        data["projects"] = Usage(snapshot.Projects);
        var hasUsage = snapshot.Languages.Count > 0 || snapshot.Projects.Count > 0;
        data["hasUsage"] = hasUsage;
        data["emptyMessage"] = hasUsage
            ? ""
            : EmptyWeekMessages[Snapshot.LocalDate(now, zone).DayOfYear % EmptyWeekMessages.Count];
        return data;
    }

    /// <summary>Top 4 as { name, time } rows: what fits side by side on the large widget.</summary>
    private static JsonArray Usage(IEnumerable<UsageStat> items) =>
        new(items
            .Take(4)
            .Select(item => (JsonNode?)new JsonObject
            {
                ["name"] = item.Name,
                ["time"] = DurationText.Format(item.Seconds),
            })
            .ToArray());
}
