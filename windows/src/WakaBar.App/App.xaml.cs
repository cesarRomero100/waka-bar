using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WakaBar.App.Services;
using WakaBar.App.Widgets;
using WakaBar.Core;

namespace WakaBar.App;

/// <summary>
/// A tray-only app: no main window. Left click on the icon toggles the stats window,
/// right click opens the menu. Windows are hidden, never closed, so the app keeps running.
/// </summary>
public partial class App : Application
{
    private DispatcherQueue? dispatcher;
    private StatsService? stats;
    private TaskbarIcon? trayIcon;
    private FlyoutWindow? flyout;
    private SettingsWindow? settingsWindow;
    private StatsWidgetProvider? widgetProvider;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>Another launch (Start menu, startup task) was redirected here: show the stats.</summary>
    public void OnRedirectedActivation() => dispatcher?.TryEnqueue(ShowStats);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Strings.UseAppLanguage();
        dispatcher = DispatcherQueue.GetForCurrentThread();
        stats = new StatsService(new SettingsStore(), dispatcher);
        stats.Changed += (_, _) => UpdateTrayIcon();

        Command("ShowStatsCommand", "CommandShowStats").ExecuteRequested += (_, _) => ToggleStats();
        Command("RefreshCommand", "CommandRefresh").ExecuteRequested += (_, _) => _ = stats.RefreshAsync();
        Command("OpenDashboardCommand", "CommandOpenDashboard").ExecuteRequested += (_, _) => OpenDashboard();
        Command("SettingsCommand", "CommandSettings").ExecuteRequested += (_, _) => ShowSettings();
        Command("QuitCommand", "CommandQuit").ExecuteRequested += (_, _) => Quit();

        trayIcon = (TaskbarIcon)Resources["TrayIcon"];
        UpdateTrayIcon();
        // Efficiency mode would also slow down the stats window; refreshes are light anyway.
        trayIcon.ForceCreate(enablesEfficiencyMode: false);

        RegisterWidget();
        _ = stats.RefreshAsync();
        if (!stats.HasApiKey)
        {
            ShowSettings();
        }
    }

    /// <summary>Makes the "Coding Time" widget available on the Widgets Board, served by this process.</summary>
    private void RegisterWidget()
    {
        try
        {
            widgetProvider = new StatsWidgetProvider(stats!, dispatcher!, ShowSettings);
            WidgetRegistration.Register(widgetProvider);
        }
        catch (Exception)
        {
            // Widgets unavailable (e.g. turned off on this PC): the tray app works the same.
            widgetProvider = null;
        }
    }

    /// <summary>A tray menu command from App.xaml, with its label in the app's language.</summary>
    private XamlUICommand Command(string key, string label)
    {
        var command = (XamlUICommand)Resources[key];
        command.Label = Strings.Get(label);
        return command;
    }

    private FlyoutWindow Flyout => flyout ??= new FlyoutWindow(stats!, ShowSettings, OpenDashboard, Quit);

    private void ToggleStats() => Flyout.Toggle();

    private void ShowStats() => Flyout.ShowNearTray();

    private void ShowSettings()
    {
        settingsWindow ??= new SettingsWindow(stats!);
        settingsWindow.ShowCentered();
    }

    private void OpenDashboard()
    {
        if (stats?.DashboardUrl is { } url)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(url);
        }
    }

    private void Quit()
    {
        WidgetRegistration.Revoke();
        trayIcon?.Dispose();
        stats?.Dispose();
        // Windows are only ever hidden, so nothing would end the app on its own.
        Environment.Exit(0);
    }

    /// <summary>Icon with the goal ring; today's time and the goal go in the tooltip.</summary>
    private void UpdateTrayIcon()
    {
        if (trayIcon is null || stats is null)
        {
            return;
        }

        var snapshot = stats.Snapshot;
        var today = snapshot?.TodaySecondsAsOf(DateTimeOffset.Now, TimeZoneInfo.Local);
        var goal = snapshot?.DailyGoalSeconds;
        // Setting Icon disposes the previous one.
        trayIcon.Icon = TrayIconRenderer.Render(goal is > 0 ? (today ?? 0) / goal : null);

        var tooltip = "WakaBar";
        if (today is { } todaySeconds)
        {
            tooltip += "\n" + Strings.Format("TooltipTodayFormat", DurationText.Format(todaySeconds));
            if (goal is > 0 and { } goalSeconds)
            {
                tooltip += "\n" + (todaySeconds >= goalSeconds
                    ? Strings.Get("TooltipGoalReached")
                    : Strings.Format("TooltipGoalLeftFormat", DurationText.Format(goalSeconds - todaySeconds)));
            }
        }

        if (snapshot is { StreakDays: > 0 })
        {
            tooltip += "\n" + Strings.Plural("TooltipStreak", snapshot.StreakDays);
        }

        if (stats.ErrorMessage is { } error)
        {
            tooltip += $"\n{error}";
        }
        else if (!stats.HasApiKey)
        {
            tooltip += "\n" + Strings.Get("TooltipAddKey");
        }

        trayIcon.ToolTipText = tooltip;
    }
}
