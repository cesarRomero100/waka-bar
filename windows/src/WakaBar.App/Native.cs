using System.Runtime.InteropServices;

namespace WakaBar.App;

internal static class Native
{
    /// <summary>DPI of the monitor the window is on (96 = 100%). AppWindow sizes are in physical pixels.</summary>
    [DllImport("user32.dll")]
    internal static extern uint GetDpiForWindow(nint hwnd);

    internal static double ScaleFor(nint hwnd) => GetDpiForWindow(hwnd) / 96.0;
}
