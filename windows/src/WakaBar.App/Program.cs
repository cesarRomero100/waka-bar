using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace WakaBar.App;

/// <summary>
/// Custom entry point (DISABLE_XAML_GENERATED_MAIN) so only one WakaBar runs: launching it again,
/// from the Start menu or the startup task, shows the stats of the running instance instead of
/// adding a second tray icon.
/// </summary>
public static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        if (RedirectToRunningInstance())
        {
            return 0;
        }

        Application.Start(callbackParams =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    /// <summary>True if another instance is running and received this launch.</summary>
    private static bool RedirectToRunningInstance()
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("main");
        if (mainInstance.IsCurrent)
        {
            mainInstance.Activated += OnActivated;
            return false;
        }

        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        // Redirect off the STA thread: waiting on it here could deadlock.
        Task.Run(() => mainInstance.RedirectActivationToAsync(activation).AsTask()).Wait();
        return true;
    }

    private static void OnActivated(object? sender, AppActivationArguments args) =>
        (Application.Current as App)?.OnRedirectedActivation();
}
