using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WakaBar.App.Services;
using WakaBar.Core;
using WinRT.Interop;

namespace WakaBar.App;

/// <summary>
/// The stats window above the tray icon, like the macOS popover (MenuContentView.swift):
/// borderless, always on top, hidden as soon as it loses focus.
/// </summary>
public sealed partial class FlyoutWindow : Window
{
    private const int WidthDip = 360;
    private const int HeightDip = 600;
    private const int MarginDip = 12;

    private readonly StatsService stats;
    private readonly Action openSettings;
    private readonly Action openDashboard;
    private readonly Action quit;
    private readonly DispatcherQueueTimer clock;
    private DateTimeOffset lastHidden;

    public FlyoutWindow(StatsService stats, Action openSettings, Action openDashboard, Action quit)
    {
        // Set before InitializeComponent: XAML events (SelectorBar selection) can fire during it.
        this.stats = stats;
        this.openSettings = openSettings;
        this.openDashboard = openDashboard;
        this.quit = quit;
        InitializeComponent();
        Title = "WakaBar";

        SystemBackdrop = new DesktopAcrylicBackdrop();
        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(true, false);
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;

        Activated += OnActivated;
        Closed += (_, e) =>
        {
            e.Handled = true;
            HideFlyout();
        };

        // Keeps "Updated 3 min ago" current while the window is open.
        clock = DispatcherQueue.CreateTimer();
        clock.Interval = TimeSpan.FromMinutes(1);
        clock.Tick += (_, _) => RenderUpdated();

        stats.Changed += (_, _) => Render();
        Render();
    }

    /// <summary>Tray icon click: show, or hide if already open.</summary>
    public void Toggle()
    {
        if (AppWindow.IsVisible)
        {
            HideFlyout();
            return;
        }

        // Clicking the icon first deactivates (and hides) an open window; don't reopen it right away.
        if (DateTimeOffset.Now - lastHidden < TimeSpan.FromMilliseconds(300))
        {
            return;
        }

        ShowNearTray();
    }

    /// <summary>Shows the window in the bottom-right corner of the primary screen, above the tray.</summary>
    public void ShowNearTray()
    {
        var scale = Native.ScaleFor(WindowNative.GetWindowHandle(this));
        var width = (int)(WidthDip * scale);
        var height = (int)(HeightDip * scale);
        var margin = (int)(MarginDip * scale);
        var workArea = DisplayArea.Primary.WorkArea;
        AppWindow.MoveAndResize(new RectInt32(
            workArea.X + workArea.Width - width - margin,
            workArea.Y + workArea.Height - height - margin,
            width,
            height));

        Render();
        AppWindow.Show();
        Activate();
        clock.Start();
    }

    private void HideFlyout()
    {
        AppWindow.Hide();
        lastHidden = DateTimeOffset.Now;
        clock.Stop();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            HideFlyout();
        }
    }

    private void Render()
    {
        var snapshot = stats.Snapshot;
        var hasKey = stats.HasApiKey;
        var firstLoad = hasKey && snapshot is null && stats.ErrorMessage is null;

        NoKeyPanel.Visibility = ShowIf(!hasKey);
        StatsPanel.Visibility = ShowIf(hasKey && snapshot is not null);
        LoadingRing.IsActive = firstLoad;
        LoadingRing.Visibility = ShowIf(firstLoad);
        ErrorText.Text = stats.ErrorMessage ?? "";
        ErrorText.Visibility = ShowIf(stats.ErrorMessage is not null);
        RefreshButton.IsEnabled = !stats.IsLoading;

        if (snapshot is not null)
        {
            RenderStats(snapshot);
        }

        RenderUpdated();
    }

    private void RenderStats(Snapshot snapshot)
    {
        var today = snapshot.TodaySecondsAsOf(DateTimeOffset.Now, TimeZoneInfo.Local);
        TodayText.Text = DurationText.Format(today);
        WeekText.Text = DurationText.Format(snapshot.WeekSeconds);
        AverageText.Text = Strings.Format("FlyoutAverageFormat", DurationText.Format(snapshot.DailyAverage));

        if (snapshot.DailyGoalSeconds is { } goal)
        {
            var reached = today >= goal;
            GoalPanel.Visibility = Visibility.Visible;
            GoalBar.Value = Math.Min(today / goal, 1);
            GoalText.Text = reached
                ? Strings.Get("FlyoutGoalReached")
                : Strings.Format("FlyoutGoalLeftFormat", DurationText.Format(goal - today));
            if (reached)
            {
                var success = Resource<Brush>("SystemFillColorSuccessBrush");
                GoalBar.Foreground = success;
                GoalText.Foreground = success;
            }
            else
            {
                GoalBar.ClearValue(Control.ForegroundProperty);
                GoalText.ClearValue(TextBlock.ForegroundProperty);
            }
        }
        else
        {
            GoalPanel.Visibility = Visibility.Collapsed;
        }

        StreakText.Text = snapshot.StreakDays > 0 ? Strings.Plural("FlyoutStreak", snapshot.StreakDays) : "";
        TopProjectText.Text = snapshot.TopProjectToday is { } project ? $"📁 {project.Name}" : "";
        HighlightsRow.Visibility = ShowIf(snapshot.StreakDays > 0 || snapshot.TopProjectToday is not null);

        RenderChart(snapshot.Days);
        RenderUsage(snapshot);
    }

    /// <summary>Last 7 days as bars; today is highlighted.</summary>
    private void RenderChart(IReadOnlyList<DayStat> days)
    {
        ChartGrid.Children.Clear();
        ChartGrid.ColumnDefinitions.Clear();
        if (days.Count == 0)
        {
            return;
        }

        var max = Math.Max(days.Max(day => day.Seconds), 3600);
        var accent = Resource<Brush>("AccentFillColorDefaultBrush");
        var secondary = Resource<Brush>("TextFillColorSecondaryBrush");
        var caption = Resource<Style>("CaptionTextBlockStyle");

        for (var i = 0; i < days.Count; i++)
        {
            var day = days[i];
            ChartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var column = new Grid { RowSpacing = 4 };
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var bar = new Border
            {
                Height = Math.Max(2, day.Seconds / max * 92),
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(3),
                Background = accent,
                Opacity = i == days.Count - 1 ? 1 : 0.45,
            };
            var dayName = day.Date.ToString("dddd", CultureInfo.CurrentCulture);
            ToolTipService.SetToolTip(bar, $"{dayName}: {DurationText.Format(day.Seconds)}");

            var label = new TextBlock
            {
                Text = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(day.Date.DayOfWeek),
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = caption,
                Foreground = secondary,
            };
            Grid.SetRow(label, 1);

            column.Children.Add(bar);
            column.Children.Add(label);
            Grid.SetColumn(column, i);
            ChartGrid.Children.Add(column);
        }
    }

    /// <summary>Top 5 languages or projects of the week, each with a bar proportional to its share.</summary>
    private void RenderUsage(Snapshot snapshot)
    {
        var items = BreakdownBar.SelectedItem == ProjectsItem ? snapshot.Projects : snapshot.Languages;
        var secondary = Resource<Brush>("TextFillColorSecondaryBrush");
        var caption = Resource<Style>("CaptionTextBlockStyle");
        UsageList.Children.Clear();

        if (items.Count == 0)
        {
            UsageList.Children.Add(new TextBlock { Text = Strings.Get("FlyoutNoActivity"), Style = caption, Foreground = secondary });
            return;
        }

        var total = Math.Max(items.Sum(item => item.Seconds), 1);
        foreach (var item in items.Take(5))
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = item.Name, TextTrimming = TextTrimming.CharacterEllipsis, Style = caption });

            var duration = new TextBlock { Text = DurationText.Format(item.Seconds), Style = caption, Foreground = secondary };
            Grid.SetColumn(duration, 1);
            row.Children.Add(duration);

            var entry = new StackPanel { Spacing = 2 };
            entry.Children.Add(row);
            entry.Children.Add(new ProgressBar { Maximum = 1, Value = item.Seconds / total });
            UsageList.Children.Add(entry);
        }
    }

    private void RenderUpdated()
    {
        if (stats.Snapshot is not { } snapshot)
        {
            UpdatedText.Text = "";
            return;
        }

        var age = DateTimeOffset.Now - snapshot.UpdatedAt;
        UpdatedText.Text = age < TimeSpan.FromMinutes(1) ? Strings.Get("FlyoutUpdatedNow")
            : age < TimeSpan.FromHours(1) ? Strings.Format("FlyoutUpdatedMinutesFormat", (int)age.TotalMinutes)
            : Strings.Format("FlyoutUpdatedAtFormat", snapshot.UpdatedAt.ToLocalTime().ToString("t", CultureInfo.CurrentCulture));
    }

    private void OnBreakdownChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (stats.Snapshot is { } snapshot)
        {
            RenderUsage(snapshot);
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => _ = stats.RefreshAsync();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        HideFlyout();
        openSettings();
    }

    private void OnOpenDashboard(object sender, RoutedEventArgs e)
    {
        HideFlyout();
        openDashboard();
    }

    private void OnQuit(object sender, RoutedEventArgs e) => quit();

    private static Visibility ShowIf(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>A theme resource (brush, style) for the current light/dark theme.</summary>
    private static T Resource<T>(string key) => (T)Application.Current.Resources[key];
}
