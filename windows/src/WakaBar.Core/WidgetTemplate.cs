namespace WakaBar.Core;

/// <summary>
/// Adaptive Card template for the Widgets Board widget (WidgetTemplate.json, embedded), bound to
/// <see cref="WidgetData"/>. One template for all sizes: elements switch on <c>$host.widgetSize</c>.
/// </summary>
public static class WidgetTemplate
{
    public static string Json { get; } = Load();

    private static string Load()
    {
        using var stream = typeof(WidgetTemplate).Assembly.GetManifestResourceStream("WakaBar.Core.WidgetTemplate.json")
            ?? throw new InvalidOperationException("WidgetTemplate.json is not embedded in WakaBar.Core.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
