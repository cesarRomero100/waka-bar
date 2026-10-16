using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using WakaBar.Core;

namespace WakaBar.App.Widgets;

/// <summary>
/// Images for the widget, sent as PNG data URIs: Adaptive Cards on the Widgets Board have no charts
/// or progress bars. Drawn at 2x the widget size so they stay sharp on high-DPI screens.
/// </summary>
internal static class WidgetImages
{
    /// <summary>The last 7 days as bars with day letters below; today is highlighted.</summary>
    public static string Chart(IReadOnlyList<DayStat> days, bool dark)
    {
        // 268 x 80 on the widget: medium and large are 268 px wide inside their margins.
        const int width = 600;
        const int height = 180;
        const int labelHeight = 36;

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(Color.Transparent);
            if (days.Count > 0)
            {
                // Light violet on dark backgrounds, where the app's indigo would be too dim.
                var accent = dark ? Color.FromArgb(139, 126, 255) : Shapes.Brand;
                var label = dark ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(160, 0, 0, 0);
                var max = Math.Max(days.Max(day => day.Seconds), 3600);
                var slot = width / (float)days.Count;
                var barWidth = slot * 0.6f;
                var chartHeight = height - labelHeight - 8;

                using var font = new Font("Segoe UI", 20, FontStyle.Regular, GraphicsUnit.Pixel);
                using var labelBrush = new SolidBrush(label);
                using var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                for (var i = 0; i < days.Count; i++)
                {
                    var isToday = i == days.Count - 1;
                    var barHeight = Math.Max(4, (float)(days[i].Seconds / max * chartHeight));
                    var bar = new RectangleF((i * slot) + ((slot - barWidth) / 2), chartHeight - barHeight, barWidth, barHeight);
                    using (var path = Shapes.RoundedRectangle(bar, 8))
                    using (var brush = new SolidBrush(isToday ? accent : Color.FromArgb(115, accent)))
                    {
                        graphics.FillPath(brush, path);
                    }

                    var dayName = CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(days[i].Date.DayOfWeek);
                    graphics.DrawString(dayName, font, labelBrush, new RectangleF(i * slot, height - labelHeight, slot, labelHeight), centered);
                }
            }
        }

        return DataUri(bitmap);
    }

    /// <summary>The app icon, for the widget's empty states (no API key, loading).</summary>
    public static string AppIcon()
    {
        const int size = 96;
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            graphics.ScaleTransform(size / 64f, size / 64f);
            TrayIconRenderer.DrawPlain(graphics);
        }

        return DataUri(bitmap);
    }

    /// <summary>Thin progress bar towards the daily goal: amber, green once reached.</summary>
    public static string GoalBar(double progress, bool reached)
    {
        const int width = 600;
        const int height = 16;

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using (var track = Shapes.RoundedRectangle(new RectangleF(0, 0, width, height), height / 2f))
            using (var brush = new SolidBrush(Shapes.Track))
            {
                graphics.FillPath(brush, track);
            }

            if (progress > 0)
            {
                var filled = Math.Max(height, (float)(Math.Clamp(progress, 0, 1) * width));
                using var fill = Shapes.RoundedRectangle(new RectangleF(0, 0, filled, height), height / 2f);
                using var brush = new SolidBrush(reached ? Shapes.Reached : Shapes.Progress);
                graphics.FillPath(brush, fill);
            }
        }

        return DataUri(bitmap);
    }

    private static string DataUri(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
    }
}
