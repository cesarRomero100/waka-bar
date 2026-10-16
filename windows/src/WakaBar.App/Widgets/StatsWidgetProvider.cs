using Microsoft.UI.Dispatching;
using Microsoft.Windows.Widgets.Providers;
using WakaBar.App.Services;
using WakaBar.Core;

namespace WakaBar.App.Widgets;

/// <summary>
/// Serves the "Coding Time" widget to the Widgets Board. The board reaches it through COM
/// (<see cref="WidgetRegistration"/>), so these methods run on background threads; the stats
/// come from the tray app's <see cref="StatsService"/>, and the widget updates whenever it changes.
/// </summary>
internal sealed partial class StatsWidgetProvider : IWidgetProvider
{
    /// <summary>The same on every update: rendered once.</summary>
    private static readonly Lazy<string> AppIcon = new(WidgetImages.AppIcon);

    private readonly StatsService stats;
    private readonly DispatcherQueue dispatcher;
    private readonly Action showSettings;
    private readonly HashSet<string> widgetIds = [];
    private readonly Lock gate = new();

    public StatsWidgetProvider(StatsService stats, DispatcherQueue dispatcher, Action showSettings)
    {
        this.stats = stats;
        this.dispatcher = dispatcher;
        this.showSettings = showSettings;

        // Widgets pinned before this launch (restart, crash) keep getting updates.
        // GetWidgetInfos returns null, not an empty array, while no widget is pinned.
        foreach (var info in WidgetManager.GetDefault().GetWidgetInfos() ?? [])
        {
            widgetIds.Add(info.WidgetContext.Id);
        }

        stats.Changed += (_, _) => UpdateAll();
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        var widgetId = widgetContext.Id;
        lock (gate)
        {
            widgetIds.Add(widgetId);
        }

        Send(widgetId, BuildData());
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        lock (gate)
        {
            widgetIds.Remove(widgetId);
        }
    }

    /// <summary>A click anywhere on the widget: the dashboard, or Settings while there's no API key.</summary>
    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb != "open")
        {
            return;
        }

        dispatcher.TryEnqueue(() =>
        {
            if (!stats.HasApiKey)
            {
                showSettings();
            }
            else if (stats.DashboardUrl is { } url)
            {
                _ = Windows.System.Launcher.LaunchUriAsync(url);
            }
        });
    }

    /// <summary>The user resized the widget. One template serves every size, but resend so it redraws.</summary>
    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs) =>
        Send(contextChangedArgs.WidgetContext.Id, BuildData());

    /// <summary>The board is showing the widget: give it fresh numbers.</summary>
    public void Activate(WidgetContext widgetContext)
    {
        var widgetId = widgetContext.Id;
        lock (gate)
        {
            widgetIds.Add(widgetId);
        }

        Send(widgetId, BuildData());
    }

    /// <summary>Hidden widgets still get the 5-minute updates: they're small and cheap.</summary>
    public void Deactivate(string widgetId)
    {
    }

    private void UpdateAll()
    {
        string[] ids;
        lock (gate)
        {
            ids = [.. widgetIds];
        }

        if (ids.Length == 0)
        {
            return;
        }

        var data = BuildData();
        foreach (var id in ids)
        {
            Send(id, data);
        }
    }

    private string BuildData()
    {
        var snapshot = stats.Snapshot;
        var data = WidgetData.Build(snapshot, stats.HasApiKey, stats.ErrorMessage, DateTimeOffset.Now, TimeZoneInfo.Local);
        data["appIcon"] = AppIcon.Value;
        if (snapshot is null || data["hasData"]?.GetValue<bool>() != true)
        {
            return data.ToJsonString();
        }

        data["chartLight"] = WidgetImages.Chart(snapshot.Days, dark: false);
        data["chartDark"] = WidgetImages.Chart(snapshot.Days, dark: true);
        if (data["hasGoal"]?.GetValue<bool>() == true)
        {
            data["goalImage"] = WidgetImages.GoalBar(
                data["goalProgress"]!.GetValue<double>(),
                data["goalReached"]!.GetValue<bool>());
        }

        return data.ToJsonString();
    }

    private static void Send(string widgetId, string data)
    {
        try
        {
            WidgetManager.GetDefault().UpdateWidget(new WidgetUpdateRequestOptions(widgetId)
            {
                Template = WidgetTemplate.Json,
                Data = data,
            });
        }
        catch (Exception)
        {
            // The widget was removed in the meantime, or the board is restarting.
        }
    }
}
