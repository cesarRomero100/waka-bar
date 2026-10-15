// Generates the MSIX package images (tiles, logos, splash screen) with the same design as the macOS icon.
// Needs a Mac (AppKit). Usage, from windows/: swift Scripts/generate-assets.swift src/WakaBar.App/Assets

import AppKit

let outputDir = URL(fileURLWithPath: CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : ".")
try FileManager.default.createDirectory(at: outputDir, withIntermediateDirectories: true)

/// Draws the icon (rounded square, indigo gradient, rising bars) centered on a transparent canvas.
/// `iconFraction` is the icon's side relative to the canvas height.
func render(width: Int, height: Int, iconFraction: CGFloat) -> Data {
    let rep = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
        colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
    )!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    let context = NSGraphicsContext.current!.cgContext

    // Work on a 1024-point icon canvas, scaled and centered.
    let side = CGFloat(height) * iconFraction
    context.translateBy(x: (CGFloat(width) - side) / 2, y: (CGFloat(height) - side) / 2)
    context.scaleBy(x: side / 1024, y: side / 1024)

    let body = NSBezierPath(roundedRect: NSRect(x: 0, y: 0, width: 1024, height: 1024), xRadius: 220, yRadius: 220)
    let gradient = NSGradient(
        starting: NSColor(srgbRed: 0.43, green: 0.36, blue: 0.99, alpha: 1),
        ending: NSColor(srgbRed: 0.23, green: 0.16, blue: 0.68, alpha: 1)
    )!
    gradient.draw(in: body, angle: -90)

    let heights: [CGFloat] = [250, 400, 320, 560]
    let barWidth: CGFloat = 112
    let gap: CGFloat = 62
    let totalWidth = barWidth * CGFloat(heights.count) + gap * CGFloat(heights.count - 1)
    var x = (1024 - totalWidth) / 2
    for (index, barHeight) in heights.enumerated() {
        let bar = NSBezierPath(
            roundedRect: NSRect(x: x, y: 210, width: barWidth, height: barHeight),
            xRadius: 30, yRadius: 30
        )
        NSColor.white.withAlphaComponent(index == heights.count - 1 ? 1 : 0.7).setFill()
        bar.fill()
        x += barWidth + gap
    }

    NSGraphicsContext.restoreGraphicsState()
    return rep.representation(using: .png, properties: [:])!
}

// File names referenced by Package.appxmanifest; Windows picks the scale-200 variants.
let assets: [(name: String, width: Int, height: Int, iconFraction: CGFloat)] = [
    ("Square44x44Logo.scale-200.png", 88, 88, 0.92),
    ("Square150x150Logo.scale-200.png", 300, 300, 0.6),
    ("Wide310x150Logo.scale-200.png", 620, 300, 0.6),
    ("SplashScreen.scale-200.png", 1240, 600, 0.5),
    ("LockScreenLogo.scale-200.png", 48, 48, 0.92),
    ("StoreLogo.png", 50, 50, 0.92),
]

for asset in assets {
    let data = render(width: asset.width, height: asset.height, iconFraction: asset.iconFraction)
    try data.write(to: outputDir.appendingPathComponent(asset.name))
}
print("Wrote \(assets.count) images to \(outputDir.path)")

// MARK: - Widgets Board: widget icon and widget picker screenshots

let publicDir = outputDir.deletingLastPathComponent().appendingPathComponent("Public")
try FileManager.default.createDirectory(at: publicDir, withIntermediateDirectories: true)
try render(width: 64, height: 64, iconFraction: 0.94).write(to: publicDir.appendingPathComponent("WidgetIcon.png"))

/// A mock of the medium widget, 300x304 with transparent rounded corners, as the picker requires.
func screenshot(dark: Bool) -> Data {
    let width = 300, height = 304
    let rep = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
        colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
    )!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    // Top-left origin, like the widget layout.
    let context = NSGraphicsContext.current!.cgContext
    context.translateBy(x: 0, y: CGFloat(height))
    context.scaleBy(x: 1, y: -1)
    let flipped = NSGraphicsContext(cgContext: context, flipped: true)
    NSGraphicsContext.current = flipped

    let background = dark ? NSColor(white: 0.17, alpha: 1) : NSColor(white: 0.98, alpha: 1)
    let text = dark ? NSColor.white : NSColor(white: 0.1, alpha: 1)
    let subtle = dark ? NSColor(white: 1, alpha: 0.65) : NSColor(white: 0, alpha: 0.55)
    let accent = dark ? NSColor(srgbRed: 139 / 255, green: 126 / 255, blue: 1, alpha: 1)
        : NSColor(srgbRed: 91 / 255, green: 76 / 255, blue: 224 / 255, alpha: 1)
    let amber = NSColor(srgbRed: 245 / 255, green: 158 / 255, blue: 11 / 255, alpha: 1)
    let track = NSColor(white: 0.5, alpha: 0.47)

    background.setFill()
    NSBezierPath(roundedRect: NSRect(x: 0, y: 0, width: width, height: height), xRadius: 12, yRadius: 12).fill()

    func draw(_ string: String, _ x: CGFloat, _ y: CGFloat, size: CGFloat, color: NSColor, bold: Bool = false, right: Bool = false) {
        let font = bold ? NSFont.boldSystemFont(ofSize: size) : NSFont.systemFont(ofSize: size)
        let attributed = NSAttributedString(string: string, attributes: [.font: font, .foregroundColor: color])
        let origin = right ? x - attributed.size().width : x
        attributed.draw(at: NSPoint(x: origin, y: y))
    }

    draw("Today", 16, 14, size: 12, color: subtle)
    draw("3h 12m", 16, 30, size: 28, color: text, bold: true)
    track.setFill()
    NSBezierPath(roundedRect: NSRect(x: 16, y: 70, width: 140, height: 6), xRadius: 3, yRadius: 3).fill()
    amber.setFill()
    NSBezierPath(roundedRect: NSRect(x: 16, y: 70, width: 90, height: 6), xRadius: 3, yRadius: 3).fill()
    draw("1h 48m to go", 16, 82, size: 12, color: subtle)

    draw("Last 7 days", 284, 14, size: 12, color: subtle, right: true)
    draw("15h 12m", 284, 30, size: 15, color: text, bold: true, right: true)
    draw("avg 2h 10m/day", 284, 50, size: 12, color: subtle, right: true)

    draw("🔥 7-day streak", 16, 108, size: 12, color: text)
    draw("📁 waka-bar · 2h", 16, 126, size: 12, color: text)

    let hours: [CGFloat] = [2.5, 4.1, 3.2, 1.0, 5.0, 2.2, 3.2]
    let letters = ["M", "T", "W", "T", "F", "S", "S"]
    let chartTop: CGFloat = 156, chartHeight: CGFloat = 108, slot: CGFloat = 268 / 7
    for (index, value) in hours.enumerated() {
        let barHeight = value / 5 * chartHeight
        let x = 16 + CGFloat(index) * slot + slot * 0.2
        (index == hours.count - 1 ? accent : accent.withAlphaComponent(0.45)).setFill()
        NSBezierPath(
            roundedRect: NSRect(x: x, y: chartTop + chartHeight - barHeight, width: slot * 0.6, height: barHeight),
            xRadius: 4, yRadius: 4
        ).fill()
        draw(letters[index], x + slot * 0.3 - 4, chartTop + chartHeight + 6, size: 12, color: subtle)
    }

    NSGraphicsContext.restoreGraphicsState()
    return rep.representation(using: .png, properties: [:])!
}

try screenshot(dark: false).write(to: publicDir.appendingPathComponent("WidgetScreenshotLight.png"))
try screenshot(dark: true).write(to: publicDir.appendingPathComponent("WidgetScreenshotDark.png"))
print("Wrote the widget icon and screenshots to \(publicDir.path)")
