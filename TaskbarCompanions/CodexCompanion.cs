using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskbarCompanions;

// The default Codex pet. Its artwork belongs to OpenAI and is not redistributed here: it is read
// from the Codex extension installed on this PC. Without it, the terminal explorer takes his place.
internal static class CodexCompanion
{
    public static readonly BitmapSource[][]? Frames = LoadFrames(FindSheet());

    private static string? FindSheet()
    {
        try
        {
            return CodexAppServerProvider.ExtensionDirectories()
                .Select(d => new DirectoryInfo(Path.Combine(d.FullName, "webview", "assets")))
                .Where(assets => assets.Exists)
                .SelectMany(assets => assets.EnumerateFiles("codex-spritesheet*.webp"))
                .Select(f => f.FullName)
                .FirstOrDefault();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }
    }

    internal static BitmapSource[][]? LoadFrames(string? path)
    {
        int[] counts = { 7, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8 };
        if (path is null || !File.Exists(path)) return null;
        var sheet = new BitmapImage();
        try
        {
            sheet.BeginInit();
            sheet.CacheOption = BitmapCacheOption.OnLoad;
            sheet.UriSource = new Uri(path);
            sheet.EndInit();
            sheet.Freeze();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException
            or FileFormatException or System.Runtime.InteropServices.ExternalException) { return null; }
        // Only the known layout of 192 × 208 cells; a future sheet with another layout falls back.
        if (sheet.PixelWidth != 8 * 192 || sheet.PixelHeight != counts.Length * 208) return null;
        var source = WithAlpha(sheet);
        var rows = new BitmapSource[counts.Length][];
        int width = sheet.PixelWidth / 8, height = sheet.PixelHeight / counts.Length;
        for (int row = 0; row < counts.Length; row++)
        {
            rows[row] = new BitmapSource[counts[row]];
            for (int column = 0; column < counts[row]; column++)
            {
                var frame = new CroppedBitmap(source, new Int32Rect(column * width, row * height, width, height));
                frame.Freeze();
                rows[row][column] = frame;
            }
        }
        return rows;
    }

    // Windows' WebP decoder reports the sheet as opaque Bgr32, but the fourth byte still holds its
    // transparency; drawn as is, the background comes out black. Reread those bytes as alpha.
    // A genuinely opaque Bgr32 image leaves that byte at zero and is kept as it is.
    internal static BitmapSource WithAlpha(BitmapSource image)
    {
        if (image.Format != PixelFormats.Bgr32) return image;
        var stride = image.PixelWidth * 4;
        var pixels = new byte[stride * image.PixelHeight];
        image.CopyPixels(pixels, stride, 0);
        var hasAlpha = false;
        for (var i = 3; i < pixels.Length && !hasAlpha; i += 4) hasAlpha = pixels[i] != 0;
        if (!hasAlpha) return image;
        var result = BitmapSource.Create(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    public static void Draw(DrawingContext d, CharacterFrame f)
    {
        int row = 0, column = 0;
        bool asleep = f.Energy is <= 0 && f.SincePoke > 2;
        bool blink = f.Time % 8 >= 7.82;
        double tilt = 0, lift = 0, squash = 0;
        // Slow breathing is anchored at his feet; gestures are short, with long quiet gaps.
        double breath = (1 - Math.Cos(f.Time * Math.PI / 2.6)) * .004;
        var cheer = f.SinceReset;
        if (f.Dragged)
        {
            row = 5; column = 1;
            tilt = Math.Sin(f.Time * 3.5) * 4;
            lift = -2;
        }
        else if (f.SinceDrop < .65)
        {
            row = 8; column = 1;
            squash = Math.Sin(f.SinceDrop / .65 * Math.PI) * .045;
        }
        // An allowance reset is the one thing worth a celebration: a jump, a happy bounce, then a wave.
        else if (cheer < .9) { row = 4; column = Math.Min(4, (int)(cheer / .18)); }
        else if (cheer < 2.1) { row = 8; column = (int)(cheer * 4) % 2 == 0 ? 2 : 1; }
        else if (cheer < 3.2) { row = 3; column = 1 + (int)(cheer * 5) % 2; }
        else if (f.SincePoke < 1.8)
        {
            var poke = f.SincePoke;
            if (poke < .3)
            {
                row = 8; column = 1;
                squash = Math.Sin(poke / .3 * Math.PI) * .035;
            }
            else if (poke < 1.15)
            {
                row = 3; column = 1 + (int)((poke - .3) / .22) % 2;
                tilt = -2 * Math.Sin((poke - .3) / .85 * Math.PI);
            }
            else { row = 8; column = 1; }
        }
        else if (asleep || f.Energy is <= 20) column = 1;
        else if (f.Hovered)
        {
            row = 8; column = blink ? 0 : 1;
            tilt = f.Gaze.X * f.Attention * 2;
        }
        else if (f.Time % 90 is >= 60 and < 84)
        {
            row = 7;
            column = blink ? 2 : f.Time % 12 < 2 ? 1 : 0;
        }
        else if (f.Time % 30 is >= 24 and < 27)
        {
            row = 6; column = f.Time % 30 < 25.5 ? 0 : 2;
        }
        else if (blink) column = 1;

        if (asleep) breath *= .5;
        d.PushTransform(new TranslateTransform(0, lift));
        d.PushTransform(new RotateTransform(tilt, 0, 35));
        d.PushTransform(new ScaleTransform(1 + squash * .5, 1 + breath - squash, 0, 35));
        d.DrawImage(Frames![row][column], new Rect(-48, -65, 96, 104));
        d.Pop(); d.Pop(); d.Pop();

        // Cyan sparkles, the color of his prompt, drift up around him.
        if (cheer < 2.4 && !f.Dragged)
            for (var i = 0; i < 6; i++)
            {
                var k = cheer - i * .12;
                if (k is < 0 or > 1.4) continue;
                var x = Math.Round((i % 2 == 0 ? -1 : 1) * (26 + i * 4 + k * 6));
                var y = Math.Round(-20 - i * 6 - k * 26);
                var size = k < .9 ? 2 : 1;
                d.DrawRectangle(Spark, null, new Rect(x - size, y - size * 3, size * 2, size * 6));
                d.DrawRectangle(Spark, null, new Rect(x - size * 3, y - size, size * 6, size * 2));
            }
    }

    private static readonly Brush Spark = Freeze(new SolidColorBrush(Color.FromRgb(143, 240, 255)));
    private static Brush Freeze(Brush b) { b.Freeze(); return b; }
}
