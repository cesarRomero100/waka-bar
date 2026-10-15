using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.Graphics;
using WakaBar.App.Services;
using WakaBar.Core;
using WinRT.Interop;

namespace WakaBar.App;

/// <summary>API key, server, daily goal and launch at startup. Mirrors macOS SettingsView.swift.</summary>
public sealed partial class SettingsWindow : Window
{
    private const int WidthDip = 520;
    private const int HeightDip = 640;

    /// <summary>Daily goal choices, in hours. Index 0 of the combo box is "Off".</summary>
    private static readonly double[] GoalHours = [0.5, 1, 1.5, 2, 3, 4, 5, 6, 8];

    private readonly StatsService stats;

    /// <summary>True while the code sets controls, so their change events don't save anything.</summary>
    private bool updating = true;

    public SettingsWindow(StatsService stats)
    {
        this.stats = stats;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        Closed += (_, e) =>
        {
            // Hidden, not closed: the app keeps running in the tray.
            e.Handled = true;
            AppWindow.Hide();
        };

        GoalBox.Items.Add("Off");
        foreach (var hours in GoalHours)
        {
            GoalBox.Items.Add(DurationText.Format(hours * 3600));
        }

        updating = false;
    }

    /// <summary>Shows the window in the middle of the primary screen, with the saved values.</summary>
    public void ShowCentered()
    {
        LoadFields();
        var scale = Native.ScaleFor(WindowNative.GetWindowHandle(this));
        var width = (int)(WidthDip * scale);
        var height = (int)(HeightDip * scale);
        var workArea = DisplayArea.Primary.WorkArea;
        AppWindow.MoveAndResize(new RectInt32(
            workArea.X + ((workArea.Width - width) / 2),
            workArea.Y + ((workArea.Height - height) / 2),
            width,
            height));

        AppWindow.Show();
        Activate();
        _ = LoadLaunchAtStartupAsync();
    }

    private void LoadFields()
    {
        updating = true;
        ApiKeyBox.Password = stats.Settings.ApiKey ?? "";
        ServerBox.Text = stats.Settings.CustomBaseUrl ?? "";
        GoalBox.SelectedIndex = Array.IndexOf(GoalHours, stats.Settings.DailyGoalSeconds / 3600) + 1;
        ErrorText.Visibility = Visibility.Collapsed;
        updating = false;
    }

    private async Task LoadLaunchAtStartupAsync()
    {
        try
        {
            ShowStartupState(await LoginItem.GetStateAsync());
        }
        catch (Exception error)
        {
            ShowStartupError(error);
        }
    }

    private async void OnLaunchAtStartupToggled(object sender, RoutedEventArgs e)
    {
        if (updating)
        {
            return;
        }

        try
        {
            ShowStartupState(await LoginItem.SetEnabledAsync(LaunchAtStartupSwitch.IsOn));
        }
        catch (Exception error)
        {
            ShowStartupError(error);
        }
    }

    /// <summary>The switch always shows the real state, even when Windows refused the change.</summary>
    private void ShowStartupState(StartupTaskState state)
    {
        updating = true;
        LaunchAtStartupSwitch.IsOn = LoginItem.IsEnabled(state);
        updating = false;

        var note = state switch
        {
            StartupTaskState.DisabledByUser => "Turned off in Task Manager › Startup apps. Turn it back on there.",
            StartupTaskState.DisabledByPolicy or StartupTaskState.EnabledByPolicy => "Managed by your organization.",
            _ => null,
        };
        LaunchAtStartupNote.Text = note ?? "";
        LaunchAtStartupNote.Visibility = note is null ? Visibility.Collapsed : Visibility.Visible;
        LaunchAtStartupSwitch.IsEnabled = state is not (StartupTaskState.DisabledByPolicy or StartupTaskState.EnabledByPolicy);
    }

    private void ShowStartupError(Exception error)
    {
        LaunchAtStartupNote.Text = $"Launch at startup isn't available: {error.Message}";
        LaunchAtStartupNote.Visibility = Visibility.Visible;
        LaunchAtStartupSwitch.IsEnabled = false;
    }

    private void OnGoalChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updating || GoalBox.SelectedIndex < 0)
        {
            return;
        }

        stats.SetDailyGoal(GoalBox.SelectedIndex == 0 ? 0 : GoalHours[GoalBox.SelectedIndex - 1] * 3600);
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        SavingRing.IsActive = true;
        ErrorText.Visibility = Visibility.Collapsed;

        var error = await stats.SaveAsync(ApiKeyBox.Password, ServerBox.Text);

        SavingRing.IsActive = false;
        SaveButton.IsEnabled = true;
        if (error is null)
        {
            AppWindow.Hide();
            return;
        }

        ErrorText.Text = error;
        ErrorText.Visibility = Visibility.Visible;
    }
}
