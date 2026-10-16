using System.Drawing;
using System.Drawing.Drawing2D;

namespace WakaBar.App;

/// <summary>Drawing helpers shared by the tray icon and the widget images.</summary>
internal static class Shapes
{
    /// <summary>Same indigo as the app icon.</summary>
    public static readonly Color Brand = Color.FromArgb(91, 76, 224);

    /// <summary>Amber: stands out from a gray track on light and dark backgrounds.</summary>
    public static readonly Color Progress = Color.FromArgb(245, 158, 11);

    public static readonly Color Reached = Color.FromArgb(34, 197, 94);

    /// <summary>Gray at half opacity: visible on light and dark backgrounds.</summary>
    public static readonly Color Track = Color.FromArgb(120, 128, 128, 128);

    public static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
        var path = new GraphicsPath();
        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static RectangleF Inset(RectangleF rectangle, float amount) =>
        new(rectangle.X + amount, rectangle.Y + amount, rectangle.Width - (2 * amount), rectangle.Height - (2 * amount));
}
