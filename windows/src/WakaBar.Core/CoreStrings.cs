using System.Globalization;
using System.Resources;

namespace WakaBar.Core;

/// <summary>
/// Strings that Core puts on screen (widget data), from Resources/Strings.resx and its
/// translations (Strings.es.resx). Translations come from the macOS catalog where the text is shared.
/// </summary>
public static class CoreStrings
{
    private static readonly ResourceManager Resources = new("WakaBar.Core.Resources.Strings", typeof(CoreStrings).Assembly);

    /// <summary>The text for <paramref name="key"/> in <paramref name="culture"/> (falls back to English).</summary>
    public static string Get(string key, CultureInfo culture) =>
        Resources.GetString(key, culture) ?? throw new KeyNotFoundException($"No string named {key} in Strings.resx.");

    public static string Format(string key, CultureInfo culture, params object[] args) =>
        string.Format(culture, Get(key, culture), args);

    /// <summary>Picks the "One" or "Other" variant of a count string, like the macOS plural rules.</summary>
    public static string Plural(string key, int count, CultureInfo culture) =>
        Format(count == 1 ? key + "OneFormat" : key + "OtherFormat", culture, count);
}
