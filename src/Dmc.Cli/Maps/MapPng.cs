using SkiaSharp;
using Svg.Skia;
using Svg.Skia.TypefaceProviders;

namespace Dmc.Maps;

/// <summary>
/// Draws the map's SVG to a PNG that DMC can look at with the Read tool. The picture is the same size as the SVG
/// (1 pixel to 1 SVG unit): the script this replaces drew it a third bigger, and image tokens grow with the pixels.
/// </summary>
public static class MapPng
{
    /// <summary>Render the SVG and return the PNG, with its size in pixels.</summary>
    public static (byte[] Png, int Width, int Height) Render(string svgText)
    {
        using var svg = new SKSvg();
        // Scripts inside an SVG are never run; this is the default, written down so that it stays so.
        svg.Settings.EnableJavaScript = false;
        svg.Settings.TypefaceProviders?.Insert(0, new EmbeddedFonts());
        var picture = svg.FromSvg(svgText) ?? throw new DmcException("the map could not be drawn to a PNG.");
        var bounds = picture.CullRect;
        int width = (int)Math.Ceiling(bounds.Width - 0.01), height = (int)Math.Ceiling(bounds.Height - 0.01);
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawPicture(picture);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return (data.ToArray(), width, height);
    }

    public static void Write(string svgText, string path) => File.WriteAllBytes(path, Render(svgText).Png);

    /// <summary>
    /// Liberation Sans, Regular and Bold (SIL OFL 1.1, the same widths as Arial and Helvetica), embedded in the
    /// program. Without it a Linux machine that has no fonts installed draws a map with no text at all, and the
    /// picture would be different on every OS.
    /// </summary>
    sealed class EmbeddedFonts : ITypefaceProvider
    {
        static readonly Lazy<SKTypeface?> Regular = new(() => Load("LiberationSans-Regular.ttf"));
        static readonly Lazy<SKTypeface?> Bold = new(() => Load("LiberationSans-Bold.ttf"));

        static SKTypeface? Load(string name)
        {
            using var stream = typeof(EmbeddedFonts).Assembly.GetManifestResourceStream("Dmc.Maps.Fonts." + name);
            return stream is null ? null : SKTypeface.FromStream(stream);
        }

        public SKTypeface? FromFamilyName(string fontFamily, SKFontStyleWeight fontWeight, SKFontStyleWidth fontWidth,
            SKFontStyleSlant fontStyle) =>
            fontWeight >= SKFontStyleWeight.SemiBold ? Bold.Value : Regular.Value;
    }
}
