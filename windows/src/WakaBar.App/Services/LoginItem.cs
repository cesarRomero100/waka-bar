using Windows.ApplicationModel;

namespace WakaBar.App.Services;

/// <summary>Launch at startup, through the startup task declared in Package.appxmanifest.</summary>
public static class LoginItem
{
    private const string TaskId = "WakaBarStartup";

    public static async Task<StartupTaskState> GetStateAsync() =>
        (await StartupTask.GetAsync(TaskId)).State;

    /// <summary>Turns launch at startup on or off and returns the resulting state.</summary>
    public static async Task<StartupTaskState> SetEnabledAsync(bool enabled)
    {
        var task = await StartupTask.GetAsync(TaskId);
        if (enabled)
        {
            // Can't override the user (Task Manager > Startup apps) or a policy: the state then stays disabled.
            return await task.RequestEnableAsync();
        }

        task.Disable();
        return task.State;
    }

    public static bool IsEnabled(StartupTaskState state) =>
        state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
}
