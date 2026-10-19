using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using WakaBar.Core;

namespace WakaBar.App.Services;

/// <summary>
/// Keeps the stats fresh: refreshes every 5 minutes and when the PC wakes, caches the latest snapshot
/// and notifies the UI. Runs on the UI thread. Mirrors <c>macos/App/StatsModel.swift</c>.
/// </summary>
public sealed class StatsService : IDisposable
{
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private static string InvalidUrlMessage => Strings.Get("ErrorInvalidUrl");

    private readonly SettingsStore settings;
    private readonly DispatcherQueue dispatcher;
    private readonly DispatcherQueueTimer timer;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private CancellationTokenSource? refreshCancellation;

    public StatsService(SettingsStore settings, DispatcherQueue dispatcher)
    {
        this.settings = settings;
        this.dispatcher = dispatcher;
        Snapshot = SnapshotCache.Load();

        timer = dispatcher.CreateTimer();
        timer.Interval = RefreshInterval;
        timer.Tick += (_, _) => _ = RefreshAsync();
        timer.Start();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    /// <summary>Raised on the UI thread whenever anything below changes.</summary>
    public event EventHandler? Changed;

    public Snapshot? Snapshot { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsLoading { get; private set; }

    public bool HasApiKey => settings.ApiKey is not null;

    public SettingsStore Settings => settings;

    public Uri? DashboardUrl => WakaTimeClient.DashboardUrl(settings.BaseUrl);

    /// <summary>Fetches fresh stats. A new call cancels the one in flight, so an older response never wins.</summary>
    public async Task RefreshAsync()
    {
        refreshCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        refreshCancellation = cancellation;
        IsLoading = true;
        Changed?.Invoke(this, EventArgs.Empty);

        try
        {
            await LoadAsync(cancellation.Token);
        }
        finally
        {
            if (refreshCancellation == cancellation)
            {
                refreshCancellation = null;
                IsLoading = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            cancellation.Dispose();
        }
    }

    /// <summary>Saves the API key and server. Returns an error message, or null if they were saved.</summary>
    public async Task<string?> SaveAsync(string apiKey, string baseUrl)
    {
        apiKey = apiKey.Trim();
        var customUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim();
        if (customUrl is not null && WakaTimeClient.ParseBaseUrl(customUrl) is null)
        {
            return InvalidUrlMessage;
        }

        var changed = apiKey != (settings.ApiKey ?? "") || customUrl != settings.CustomBaseUrl;
        try
        {
            settings.SetApiKey(apiKey);
        }
        catch (Exception error)
        {
            return Strings.Format("ErrorSaveKeyFormat", error.Message);
        }

        settings.CustomBaseUrl = customUrl;
        if (changed)
        {
            // The previous account's numbers must not stay on screen, even if the new ones fail to load.
            Snapshot = null;
            ErrorMessage = null;
            SnapshotCache.Clear();
        }

        await RefreshAsync();
        return ErrorMessage;
    }

    /// <summary>Applies right away, without waiting for the next refresh.</summary>
    public void SetDailyGoal(double seconds)
    {
        settings.DailyGoalSeconds = seconds;
        if (Snapshot is { } snapshot)
        {
            Publish(snapshot with { DailyGoalSeconds = seconds > 0 ? seconds : null });
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        timer.Stop();
        refreshCancellation?.Cancel();
        http.Dispose();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var apiKey = settings.ApiKey;
        if (apiKey is null)
        {
            ErrorMessage = null;
            return;
        }

        var baseUrl = WakaTimeClient.ParseBaseUrl(settings.BaseUrl);
        if (baseUrl is null)
        {
            ErrorMessage = InvalidUrlMessage;
            return;
        }

        try
        {
            var snapshot = await new WakaTimeClient(http, baseUrl, apiKey).FetchSnapshotAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var goal = settings.DailyGoalSeconds;
            ErrorMessage = null;
            Publish(snapshot with { DailyGoalSeconds = goal > 0 ? goal : null });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Replaced by a newer refresh, which reports its own result.
        }
        catch (Exception error) when (error is WakaTimeApiException or HttpRequestException or JsonException or TaskCanceledException)
        {
            ErrorMessage = Describe(error);
        }
    }

    private void Publish(Snapshot snapshot)
    {
        Snapshot = snapshot;
        SnapshotCache.Save(snapshot);
    }

    private static string Describe(Exception error) => error switch
    {
        WakaTimeApiException { IsUnauthorized: true } => Strings.Get("ErrorInvalidKey"),
        WakaTimeApiException api => Strings.Format("ErrorHttpFormat", api.StatusCode),
        TaskCanceledException => Strings.Get("ErrorTimeout"),
        JsonException => Strings.Get("ErrorUnexpected"),
        _ => Strings.Get("ErrorOffline"),
    };

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        // Raised on a system thread. After sleep the numbers can be hours old; give Wi-Fi a moment first.
        dispatcher.TryEnqueue(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await RefreshAsync();
        });
    }
}
