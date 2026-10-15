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
