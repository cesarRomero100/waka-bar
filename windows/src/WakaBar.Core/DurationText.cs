namespace WakaBar.Core;

public static class DurationText
{
    /// <summary>3h 12m, 45m, 2h. Same output as <c>Format.duration</c> on macOS.</summary>
    public static string Format(double seconds)
    {
        var minutes = (int)(seconds / 60);
        var hours = minutes / 60;
        var rest = minutes % 60;
        if (hours == 0)
        {
            return $"{rest}m";
        }

        return rest == 0 ? $"{hours}h" : $"{hours}h {rest}m";
    }
}
