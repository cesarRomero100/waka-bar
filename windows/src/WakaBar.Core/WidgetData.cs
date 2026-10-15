using System.Text.Json.Nodes;

namespace WakaBar.Core;

/// <summary>
/// The data bound to <see cref="WidgetTemplate"/> for the Widgets Board. Platform-independent so it is
/// unit-tested; the app adds the chart and goal bar images (<see cref="AppImageKeys"/>).
/// </summary>
public static class WidgetData
{
    /// <summary>Keys the app sets with rendered images (PNG data URIs).</summary>
    public static readonly IReadOnlyList<string> AppImageKeys = ["chartLight", "chartDark", "goalImage"];

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
        return data;
    }

    /// <summary>Top 5 as { name, time } items for a FactSet.</summary>
    private static JsonArray Usage(IEnumerable<UsageStat> items) =>
        new(items
            .Take(5)
            .Select(item => (JsonNode?)new JsonObject
            {
                ["name"] = item.Name,
                ["time"] = DurationText.Format(item.Seconds),
            })
            .ToArray());
}
