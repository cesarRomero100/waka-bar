using System.Globalization;
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

    /// <summary>Static texts of the template, bound as <c>${text.key}</c>.</summary>
    private static readonly (string Key, string Resource)[] TemplateTexts =
    [
        ("needsKey", "WidgetNeedsKey"),
        ("openSettings", "WidgetOpenSettings"),
        ("today", "WidgetToday"),
        ("lastSevenDays", "WidgetLastSevenDays"),
        ("languages", "WidgetLanguages"),
        ("projects", "WidgetProjects"),
        ("dashboardHint", "WidgetDashboardHint"),
        ("chartAltText", "WidgetChartAltText"),
    ];

    /// <summary>
    /// Shown on the large widget when there's no activity this week, instead of empty lists.
    /// One per day, so it changes now and then without flickering between updates.
    /// </summary>
    public static IReadOnlyList<string> EmptyWeekMessages(CultureInfo culture) =>
        [CoreStrings.Get("EmptyWeek1", culture), CoreStrings.Get("EmptyWeek2", culture), CoreStrings.Get("EmptyWeek3", culture)];

    /// <param name="culture">Language of the texts; the UI language when null.</param>
    public static JsonObject Build(
        Snapshot? snapshot,
        bool hasApiKey,
        string? error,
        DateTimeOffset now,
        TimeZoneInfo zone,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var text = new JsonObject();
        foreach (var (key, resource) in TemplateTexts)
        {
            text[key] = CoreStrings.Get(resource, culture);
        }

        var data = new JsonObject
        {
            ["text"] = text,
            ["needsKey"] = !hasApiKey,
            ["hasData"] = hasApiKey && snapshot is not null,
            ["status"] = error ?? CoreStrings.Get("Loading", culture),
        };
        if (!hasApiKey || snapshot is null)
        {
            return data;
        }

        var today = snapshot.TodaySecondsAsOf(now, zone);
        data["today"] = DurationText.Format(today);
        data["week"] = DurationText.Format(snapshot.WeekSeconds);
        data["average"] = CoreStrings.Format("AverageFormat", culture, DurationText.Format(snapshot.DailyAverage));

        var goal = snapshot.DailyGoalSeconds is { } seconds and > 0 ? seconds : 0;
        var reached = goal > 0 && today >= goal;
        data["hasGoal"] = goal > 0;
        data["goalReached"] = reached;
        data["goalProgress"] = goal > 0 ? Math.Clamp(today / goal, 0, 1) : 0;
        data["goalText"] = goal <= 0 ? ""
            : reached ? CoreStrings.Get("GoalReached", culture)
            : CoreStrings.Format("GoalLeftFormat", culture, DurationText.Format(goal - today));

        data["hasStreak"] = snapshot.StreakDays > 0;
        data["streak"] = snapshot.StreakDays > 0 ? CoreStrings.Plural("Streak", snapshot.StreakDays, culture) : "";
        data["hasProject"] = snapshot.TopProjectToday is not null;
        data["project"] = snapshot.TopProjectToday is { } project
            ? CoreStrings.Format("ProjectFormat", culture, project.Name, DurationText.Format(project.Seconds))
            : "";

        data["languages"] = Usage(snapshot.Languages);
        data["projects"] = Usage(snapshot.Projects);
        var hasUsage = snapshot.Languages.Count > 0 || snapshot.Projects.Count > 0;
        data["hasUsage"] = hasUsage;
        var messages = EmptyWeekMessages(culture);
        data["emptyMessage"] = hasUsage ? "" : messages[Snapshot.LocalDate(now, zone).DayOfYear % messages.Count];
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
