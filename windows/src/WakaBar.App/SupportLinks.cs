using Windows.ApplicationModel;

namespace WakaBar.App;

/// <summary>Links of the "Support WakaBar" section in Settings. Same values in macos/App/SupportLinks.swift.</summary>
internal static class SupportLinks
{
    public static readonly Uri GitHub = new("https://github.com/cesarRomero100/waka-bar");

    /// <summary>Buy Me a Coffee page, e.g. "https://buymeacoffee.com/&lt;username&gt;". The button stays hidden while null.</summary>
    public static readonly Uri? BuyMeACoffee = null;

    /// <summary>"0.1.0", from the package version.</summary>
    public static string Version
    {
        get
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
