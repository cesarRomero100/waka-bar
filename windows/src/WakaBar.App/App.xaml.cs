using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WakaBar.App.Services;
using WakaBar.Core;

namespace WakaBar.App;

/// <summary>
/// A tray-only app: no main window. Left click on the icon toggles the stats window,
/// right click opens the menu. Windows are hidden, never closed, so the app keeps running.
/// </summary>
public partial class App : Application
{
    /// <summary>Same indigo as the app icon.</summary>
    private static readonly Windows.UI.Color Brand = Windows.UI.Color.FromArgb(255, 91, 76, 224);

    private DispatcherQueue? dispatcher;
    private StatsService? stats;
    private TaskbarIcon? trayIcon;
    private FlyoutWindow? flyout;
    private SettingsWindow? settingsWindow;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>Another launch (Start menu, startup task) was redirected here: show the stats.</summary>
    public void OnRedirectedActivation() => dispatcher?.TryEnqueue(ShowStats);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        dispatcher = DispatcherQueue.GetForCurrentThread();
        stats = new StatsService(new SettingsStore(), dispatcher);
        stats.Changed += (_, _) => UpdateTrayIcon();

        Command("ShowStatsCommand").ExecuteRequested += (_, _) => ToggleStats();
        Command("RefreshCommand").ExecuteRequested += (_, _) => _ = stats.RefreshAsync();
        Command("OpenDashboardCommand").ExecuteRequested += (_, _) => OpenDashboard();
        Command("SettingsCommand").ExecuteRequested += (_, _) => ShowSettings();
        Command("QuitCommand").ExecuteRequested += (_, _) => Quit();

        trayIcon = (TaskbarIcon)Resources["TrayIcon"];
        UpdateTrayIcon();
        // Efficiency mode would also slow down the stats window; refreshes are light anyway.
        trayIcon.ForceCreate(enablesEfficiencyMode: false);

        _ = stats.RefreshAsync();
        if (!stats.HasApiKey)
        {
            ShowSettings();
        }
    }

    private XamlUICommand Command(string key) => (XamlUICommand)Resources[key];

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
        trayIcon?.Dispose();
        stats?.Dispose();
        // Windows are only ever hidden, so nothing would end the app on its own.
        Environment.Exit(0);
    }

    /// <summary>Draws today's time on the icon ("3h", "45m") and puts the details in the tooltip.</summary>
    private void UpdateTrayIcon()
    {
        if (trayIcon is null || stats is null)
        {
            return;
        }

        var snapshot = stats.Snapshot;
        var today = snapshot?.TodaySecondsAsOf(DateTimeOffset.Now, TimeZoneInfo.Local);
        trayIcon.IconSource = MakeIcon(today is { } seconds ? ShortDuration(seconds) : "W");

        var tooltip = today is { } todaySeconds ? $"WakaBar\nToday: {DurationText.Format(todaySeconds)}" : "WakaBar";
        if (snapshot is { StreakDays: > 0 })
        {
            tooltip += $"\nStreak: {snapshot.StreakDays} days";
        }

        if (stats.ErrorMessage is { } error)
        {
            tooltip += $"\n{error}";
        }
        else if (!stats.HasApiKey)
        {
            tooltip += "\nAdd your API key in Settings.";
        }

        trayIcon.ToolTipText = tooltip;
    }

    /// <summary>Fits in a small icon: whole hours, or minutes under an hour.</summary>
    private static string ShortDuration(double seconds)
    {
        var minutes = (int)(seconds / 60);
        return minutes >= 60 ? $"{minutes / 60}h" : $"{minutes}m";
    }

    private static GeneratedIconSource MakeIcon(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Colors.White),
        Background = new SolidColorBrush(Brand),
        BackgroundType = BackgroundType.RoundedRectangle,
        CornerRadius = new CornerRadius(28),
        FontWeight = FontWeights.Bold,
        FontSize = text.Length > 2 ? 54 : 70,
    };
}
