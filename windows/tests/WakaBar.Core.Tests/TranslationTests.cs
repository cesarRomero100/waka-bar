using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WakaBar.Core.Tests;

/// <summary>
/// Every English string has a Spanish translation with the same placeholders, in Core (Strings.resx)
/// and in the app (Resources.resw, which the app's tests can't load: it's a WinUI project).
/// Mirrors LocalizationTests on macOS.
/// </summary>
public partial class TranslationTests
{
    public static TheoryData<string, string> Files => new()
    {
        { "src/WakaBar.Core/Resources/Strings.resx", "src/WakaBar.Core/Resources/Strings.es.resx" },
        { "src/WakaBar.App/Strings/en-US/Resources.resw", "src/WakaBar.App/Strings/es/Resources.resw" },
    };

    [Theory]
    [MemberData(nameof(Files))]
    public void EveryStringIsTranslated(string english, string spanish)
    {
        var source = Load(english);
        var translated = Load(spanish);
        Assert.Empty(source.Keys.Except(translated.Keys));
        Assert.Empty(translated.Keys.Except(source.Keys));
        Assert.Empty(translated.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key));
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void TranslationsKeepTheirPlaceholders(string english, string spanish)
    {
        var translated = Load(spanish);
        var mismatched = Load(english)
            .Where(entry => translated.TryGetValue(entry.Key, out var value)
                && !Placeholders(entry.Value).SequenceEqual(Placeholders(value)))
            .Select(entry => entry.Key)
            .ToList();
        Assert.Empty(mismatched);
    }

    [Fact]
    public void CoreFindsItsSpanishStrings()
    {
        // Fails if Strings.es.resx stops being compiled into the satellite assembly.
        var spanish = System.Globalization.CultureInfo.GetCultureInfo("es-MX");
        Assert.Equal("Hoy", CoreStrings.Get("WidgetToday", spanish));
    }

    /// <summary>Format items such as {0} or {1:N0}, sorted: translations may reorder them.</summary>
    private static IEnumerable<string> Placeholders(string text) =>
        FormatItem().Matches(text).Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal);

    private static Dictionary<string, string> Load(string relativePath) =>
        XDocument.Load(Path.Combine(WindowsFolder(), relativePath))
            .Root!.Elements("data")
            .ToDictionary(data => data.Attribute("name")!.Value, data => data.Element("value")?.Value ?? "");

    /// <summary>windows/, from this file's location (tests/WakaBar.Core.Tests/TranslationTests.cs).</summary>
    private static string WindowsFolder([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));

    [GeneratedRegex(@"\{(\d+)(?:[,:][^}]*)?\}")]
    private static partial Regex FormatItem();
}
