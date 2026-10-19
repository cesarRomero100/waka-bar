using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace WakaBar.App;

/// <summary>
/// The app's texts, from Strings/&lt;language&gt;/Resources.resw. XAML uses x:Uid with the same files;
/// this is for text set from code. Core has its own strings (CoreStrings, Strings.resx).
/// </summary>
internal static class Strings
{
    private static readonly ResourceLoader Loader = new();

    /// <summary>
    /// Makes .NET use the language Windows picked for the app's resources, so Core's strings
    /// (widget, durations) never come out in a different language than the windows.
    /// </summary>
    public static void UseAppLanguage()
    {
        var languages = Windows.Globalization.ApplicationLanguages.Languages;
        if (languages.Count == 0)
        {
            return;
        }

        var culture = CultureInfo.GetCultureInfo(languages[0]);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string Get(string key) => Loader.GetString(key) is { Length: > 0 } value ? value : key;

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>The "One" or "Other" variant of a count string.</summary>
    public static string Plural(string key, int count) =>
        Format(count == 1 ? key + "OneFormat" : key + "OtherFormat", count);
}
