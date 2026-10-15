using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WakaBar.App;

/// <summary>
/// Draws the tray icon: the WakaBar bars, plus a ring that fills up towards the daily goal.
/// Text doesn't fit a 16 px icon, so the time itself lives in the tooltip.
/// </summary>
internal static class TrayIconRenderer
{
    /// <summary>Drawn large; Windows scales it down to the tray size (16-32 px).</summary>
    private const int Size = 64;

    /// <param name="goalProgress">Today's time over the daily goal (1 = reached), or null without a goal.</param>
    public static Icon Render(double? goalProgress)
    {
        using var bitmap = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            if (goalProgress is { } progress)
            {
                DrawWithRing(graphics, Math.Clamp(progress, 0, 1));
            }
            else
            {
                DrawPlain(graphics);
            }
        }

        // Icon.FromHandle doesn't own the handle: keep an owning copy and free the original.
        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    /// <summary>The app icon: rounded square with the bars.</summary>
    private static void DrawPlain(Graphics graphics)
    {
        var body = new RectangleF(2, 2, Size - 4, Size - 4);
        using var path = Shapes.RoundedRectangle(body, 14);
        using var brush = new SolidBrush(Shapes.Brand);
        graphics.FillPath(brush, path);
        DrawBars(graphics, Shapes.Inset(body, body.Width * 0.22f));
    }

    /// <summary>Circle with the bars inside a progress ring that starts at 12 o'clock.</summary>
    private static void DrawWithRing(Graphics graphics, double progress)
    {
        const float ringWidth = 8;
        var ring = Shapes.Inset(new RectangleF(0, 0, Size, Size), ringWidth / 2);
        using (var track = new Pen(Shapes.Track, ringWidth))
        {
            graphics.DrawEllipse(track, ring);
        }

        if (progress > 0)
        {
            using var fill = new Pen(progress >= 1 ? Shapes.Reached : Shapes.Progress, ringWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            graphics.DrawArc(fill, ring, -90, (float)(progress * 360));
        }

        var body = Shapes.Inset(new RectangleF(0, 0, Size, Size), ringWidth + 3);
        using (var brush = new SolidBrush(Shapes.Brand))
        {
            graphics.FillEllipse(brush, body);
        }

        // Bars inside the square that fits in the circle.
        DrawBars(graphics, Shapes.Inset(body, body.Width * 0.24f));
    }

    /// <summary>Three rising bars; the tallest is solid, like today's bar in the charts.</summary>
    private static void DrawBars(Graphics graphics, RectangleF area)
    {
        float[] heights = [0.45f, 0.7f, 1f];
        var gap = area.Width * 0.14f;
        var width = (area.Width - (gap * (heights.Length - 1))) / heights.Length;
        for (var i = 0; i < heights.Length; i++)
        {
            var height = area.Height * heights[i];
            var alpha = i == heights.Length - 1 ? 255 : 185;
            using var brush = new SolidBrush(Color.FromArgb(alpha, Color.White));
            graphics.FillRectangle(brush, area.X + (i * (width + gap)), area.Bottom - height, width, height);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint handle);
}
