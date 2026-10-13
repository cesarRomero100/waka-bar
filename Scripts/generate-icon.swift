// Generates the macOS app icon set.
// Usage: swift Scripts/generate-icon.swift App/Assets.xcassets/AppIcon.appiconset

import AppKit

let outputDir = URL(fileURLWithPath: CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : ".")
try FileManager.default.createDirectory(at: outputDir, withIntermediateDirectories: true)

/// Draws the icon on a 1024-point canvas scaled to `pixels`.
func render(pixels: Int) -> Data {
    let rep = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: pixels, pixelsHigh: pixels,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
        colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
    )!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    let context = NSGraphicsContext.current!.cgContext
    let scale = CGFloat(pixels) / 1024
    context.scaleBy(x: scale, y: scale)

    // Body: macOS icon grid, 824pt rounded square centered on the canvas.
    let body = NSBezierPath(roundedRect: NSRect(x: 100, y: 100, width: 824, height: 824), xRadius: 185, yRadius: 185)

    NSGraphicsContext.saveGraphicsState()
    let shadow = NSShadow()
    shadow.shadowColor = NSColor.black.withAlphaComponent(0.3)
    shadow.shadowBlurRadius = 28
    shadow.shadowOffset = NSSize(width: 0, height: -12)
    shadow.set()
    NSColor.black.setFill()
    body.fill()
    NSGraphicsContext.restoreGraphicsState()

    let gradient = NSGradient(
        starting: NSColor(srgbRed: 0.43, green: 0.36, blue: 0.99, alpha: 1),
        ending: NSColor(srgbRed: 0.23, green: 0.16, blue: 0.68, alpha: 1)
    )!
    gradient.draw(in: body, angle: -90)

    // Bar chart: four bars rising to the right, the last one highlighted.
    let heights: [CGFloat] = [210, 340, 270, 460]
    let barWidth: CGFloat = 92
    let gap: CGFloat = 52
    let totalWidth = barWidth * CGFloat(heights.count) + gap * CGFloat(heights.count - 1)
    let baseY: CGFloat = 270
    var x = (1024 - totalWidth) / 2
    for (index, height) in heights.enumerated() {
        let bar = NSBezierPath(
            roundedRect: NSRect(x: x, y: baseY, width: barWidth, height: height),
            xRadius: 26, yRadius: 26
        )
        NSColor.white.withAlphaComponent(index == heights.count - 1 ? 1 : 0.7).setFill()
        bar.fill()
        x += barWidth + gap
    }

    NSGraphicsContext.restoreGraphicsState()
    return rep.representation(using: .png, properties: [:])!
}

// (point size, scale) pairs required for a macOS app icon.
let variants: [(points: Int, scale: Int)] = [
    (16, 1), (16, 2), (32, 1), (32, 2), (128, 1), (128, 2), (256, 1), (256, 2), (512, 1), (512, 2),
]

var images: [[String: String]] = []
for variant in variants {
    let suffix = variant.scale == 1 ? "" : "@\(variant.scale)x"
    let fileName = "icon_\(variant.points)x\(variant.points)\(suffix).png"
    try render(pixels: variant.points * variant.scale).write(to: outputDir.appendingPathComponent(fileName))
    images.append([
        "filename": fileName,
        "idiom": "mac",
        "scale": "\(variant.scale)x",
        "size": "\(variant.points)x\(variant.points)",
    ])
}

let contents: [String: Any] = ["images": images, "info": ["author": "xcode", "version": 1]]
let json = try JSONSerialization.data(withJSONObject: contents, options: [.prettyPrinted, .sortedKeys])
try json.write(to: outputDir.appendingPathComponent("Contents.json"))
print("Wrote \(variants.count) icons to \(outputDir.path)")
