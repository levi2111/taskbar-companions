using System.Windows;
using System.Windows.Media;

namespace TaskbarCompanions;

// The little terminal explorer: Codex's own drawn companion, used when the Codex extension's
// pet artwork is not installed. Everything stays inside the shared sprite canvas.
internal static class TerminalExplorer
{
    private static readonly Brush Ink = Color("#222D30");
    private static readonly Brush Shell = Color("#F4F2E9");
    private static readonly Brush Shade = Color("#C8D2CB");
    private static readonly Brush Mint = Color("#8EF0CF");
    private static readonly Brush Screen = Color("#173E39");
    private static readonly Pen Line = Freeze(new Pen(Ink, 2.5) { LineJoin = PenLineJoin.Round });
    private static readonly Pen Signal = Freeze(new Pen(Mint, 2) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square });
    private static readonly Geometry Silhouette = Freeze(Geometry.Parse(
        "M -22,-43 L 18,-43 18,-39 26,-39 26,-31 30,-31 30,-5 26,-5 26,7 20,7 20,23 12,23 12,30 3,30 3,24 -8,24 -8,30 -18,30 -18,23 -24,23 -24,7 -29,7 -29,-5 -33,-5 -33,-29 -29,-29 -29,-37 -22,-37 Z"));

    public static void Draw(DrawingContext d, CharacterFrame f)
    {
        double t = f.Time;
        bool sleepy = f.Energy is <= 20, asleep = f.Energy is <= 0 && f.SincePoke > 2;
        bool celebrate = f.SincePoke < 1.5 && !f.Dragged;
        bool wave = f.Hovered && !sleepy && !celebrate && !f.Dragged;
        double routine = t % 26;
        bool coding = routine is >= 17 and < 23 && !sleepy && !f.Hovered && !celebrate && !f.Dragged;
        bool done = routine is >= 23 and < 25 && !sleepy && !f.Hovered && !celebrate && !f.Dragged;
        double hop = celebrate ? Math.Sin(Math.Min(1, f.SincePoke / .65) * Math.PI) * 10 : 0;
        double landing = f.SinceDrop < .6 ? Math.Sin(f.SinceDrop * 19) * Math.Exp(-f.SinceDrop * 6) * .13 : 0;
        double tilt = f.Dragged ? Math.Sin(t * 6) * 9 : asleep ? -7 : coding ? 4 : Math.Sin(t * 1.3) * 1.5;

        d.PushTransform(new TranslateTransform(0, -hop + (f.Dragged ? -3 : 0)));
        d.PushTransform(new RotateTransform(tilt, 0, 29));
        d.PushTransform(new ScaleTransform(1 + landing, 1 - landing, 0, 30));

        // Small boots, shaded lower edge, and a stepped cream silhouette.
        Box(d, Ink, -21, 28, 16, 8); Box(d, Ink, 1, 28, 16, 8);
        Box(d, Shade, -18, 29, 10, 3); Box(d, Shade, 4, 29, 10, 3);
        d.DrawGeometry(Shell, Line, Silhouette);
        Box(d, Shade, -27, -3, 4, 9); Box(d, Shade, -22, 18, 8, 5);
        Box(d, Shade, 16, -34, 8, 4); Box(d, Brushes.White, -23, -34, 5, 7);

        // A mint strap and terminal satchel make this little explorer a mechanic.
        Box(d, Ink, 15, -3, 5, 23); Box(d, Mint, 16, -3, 3, 22);
        d.DrawRoundedRectangle(Screen, Line, new Rect(7, 7, 19, 15), 3, 3);
        Chevron(d, 11, 11, 3);
        Box(d, Mint, 18, 17, 4, 2);

        double lookX = f.Gaze.X * f.Attention * 3;
        double lookY = f.Gaze.Y * f.Attention * 2;
        if (coding) { lookX = 2; lookY = 2; }
        foreach (double x in new[] { -15.0, 6.0 })
        {
            if (asleep || f.Blink)
                Box(d, Ink, x, -20 + lookY, 8, 2);
            else if (celebrate || wave || done)
            {
                Box(d, Ink, x, -22, 2, 5); Box(d, Ink, x + 2, -24, 4, 2); Box(d, Ink, x + 6, -22, 2, 5);
            }
            else
            {
                Box(d, Ink, x + lookX, -26 + lookY + (sleepy ? 4 : 0), 7, f.Dragged ? 12 : sleepy ? 5 : 10);
                if (!sleepy) Box(d, Brushes.White, x + lookX + 1, -25 + lookY, 2, 2);
            }
        }
        Box(d, Ink, -5, -9, 7, 2);
        if (celebrate || f.Dragged) Box(d, Ink, -3, -7, 3, 2);
        if (wave || celebrate) { Box(d, Mint, -22, -13, 6, 3); Box(d, Mint, 14, -13, 6, 3); }

        // Articulated block hands: hello, dangling, or tapping the keyboard.
        double leftY = f.Dragged ? -12 : celebrate ? -25 : 8 + Math.Sin(t * 2) * 2;
        double rightY = f.Dragged ? -12 : wave ? -25 + Math.Sin(t * 12) * 4 : celebrate ? -25 : coding ? 8 + Math.Sin(t * 13) * 2 : 8;
        Hand(d, -34, leftY); Hand(d, 31, rightY);
        if (asleep)
        {
            double drift = t % 2.8;
            d.PushOpacity(1 - drift / 2.8);
            Zed(d, 34, -28 - drift * 6);
            d.Pop();
        }
        d.Pop(); d.Pop(); d.Pop();

        if (coding)
        {
            // A pretend local terminal animation, never a claim about actual work.
            d.DrawRoundedRectangle(Screen, Line, new Rect(-28, 8, 52, 27), 4, 4);
            Chevron(d, -22, 14, 4);
            int chars = (int)((routine - 17) * 3) % 6;
            for (int i = 0; i < chars; i++) Box(d, Mint, -9 + i * 5, 17, 3, 2);
            if (t % 1 < .6) Box(d, Shell, -9 + chars * 5, 21, 4, 2);
            Box(d, Shade, -31, 35, 58, 3);
        }
        if (celebrate || done)
        {
            double beat = celebrate ? f.SincePoke : routine - 23;
            Spark(d, -43, -31 - Math.Sin(beat * 4) * 4);
            Spark(d, 42, -42 + Math.Sin(beat * 4) * 4);
            if (done)
            {
                d.DrawRoundedRectangle(Screen, null, new Rect(28, -16, 20, 17), 4, 4);
                d.DrawLine(Signal, new(32, -8), new(36, -4));
                d.DrawLine(Signal, new(36, -4), new(43, -12));
            }
        }
    }

    private static void Hand(DrawingContext d, double x, double y) => d.DrawRoundedRectangle(Shell, Line, new Rect(x - 4, y - 5, 9, 11), 2, 2);
    private static void Chevron(DrawingContext d, double x, double y, double size)
    {
        d.DrawLine(Signal, new(x, y), new(x + size, y + size));
        d.DrawLine(Signal, new(x + size, y + size), new(x, y + size * 2));
    }
    private static void Spark(DrawingContext d, double x, double y)
    {
        Box(d, Ink, x - 2, y - 6, 4, 12); Box(d, Ink, x - 6, y - 2, 12, 4);
        Box(d, Mint, x - 1, y - 5, 2, 10); Box(d, Mint, x - 5, y - 1, 10, 2);
    }
    private static void Zed(DrawingContext d, double x, double y)
    {
        // Dark outline keeps the mint sleep glyph readable on a light desktop.
        foreach (var pen in new[] { new Pen(Ink, 4), Signal })
        {
            d.DrawLine(pen, new(x, y), new(x + 8, y));
            d.DrawLine(pen, new(x + 8, y), new(x, y + 8));
            d.DrawLine(pen, new(x, y + 8), new(x + 8, y + 8));
        }
    }
    private static void Box(DrawingContext d, Brush brush, double x, double y, double w, double h) => d.DrawRectangle(brush, null, new Rect(x, y, w, h));
    private static Brush Color(string hex) => Freeze((SolidColorBrush)new BrushConverter().ConvertFromString(hex)!);
    private static T Freeze<T>(T value) where T : Freezable { value.Freeze(); return value; }
}
