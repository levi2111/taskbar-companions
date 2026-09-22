using System.Windows;
using System.Windows.Media;

namespace TaskbarCompanions;

// Claude's companion: Clawd, the pixel critter from Claude Code's terminal banner,
// rebuilt from its block art (Claude Code 2.1.278):   ▐▛███▛█ / ▝▜██████▀ / ▝▝ ▝▝
// Each terminal quadrant becomes a 6×12 block; expressions use a 3-DIP sub-grid.
// It watches the pointer, thinks with Claude Code's spinner, reads, waves, hops when
// poked, flails when dragged, and is honest about its energy: drowsy when the lowest
// allowance runs low, asleep when one is empty.
internal static class Clawd
{
    private enum Mood { Awake, Drowsy, Asleep }
    private enum Eyes { Open, Wide, Closed, Happy, Sleeping, Lidded, Squeezed }
    private enum Mouth { None, Smile, Oh, Yawn }
    private enum Arms { Rest, Up, Droop, Hold }

    // Sub-grid: column 19 is the centerline, row 0 the top of the body; legs end on the ground at +33.
    private const double Cell = 3, Top = -27;
    private const int Mid = 19;

    // Claude Code's spinner glyphs (· ✢ ✳ ✶ ✻) as pixel stars: a plus that grows, then diagonals.
    private static readonly (int X, int Y)[][] Stars = Enumerable.Range(0, 5).Select(size =>
        (from x in Enumerable.Range(-2, 5) from y in Enumerable.Range(-2, 5)
         where x == 0 && y == 0
             || (x == 0 || y == 0) && Math.Abs(x + y) <= Math.Min(size, 2)
             || x != 0 && Math.Abs(x) == Math.Abs(y) && Math.Abs(x) <= size - 2
         select (x, y)).ToArray()).ToArray();
    private static readonly int[] Spin = { 0, 1, 2, 3, 4, 3, 2, 1 };
    private static readonly string[] ZShape = { "####", "..#.", ".#..", "####" };

    private static readonly Brush EyeInk = Frozen(new SolidColorBrush(Colors.Black));
    private static readonly Brush Shadow = Frozen(new SolidColorBrush(Color.FromRgb(40, 26, 22)));
    private static readonly Brush Cream = Frozen(new SolidColorBrush(Color.FromRgb(255, 245, 232)));
    private static readonly Brush PageShade = Frozen(new SolidColorBrush(Color.FromRgb(232, 214, 194)));
    private static readonly Brush TextInk = Frozen(new SolidColorBrush(Color.FromRgb(196, 160, 138)));
    private static readonly Brush Cover = Frozen(new SolidColorBrush(Color.FromRgb(65, 96, 122)));
    private static readonly Brush Gold = Frozen(new SolidColorBrush(Color.FromRgb(255, 214, 107)));
    private static readonly Brush Blush = Frozen(new SolidColorBrush(Color.FromRgb(250, 168, 158)));

    public static void Draw(DrawingContext d, CharacterFrame f)
    {
        // Aliased edges keep the blocks crisp at any offset or DPI.
        var sprite = new DrawingGroup();
        RenderOptions.SetEdgeMode(sprite, EdgeMode.Aliased);
        using (var g = sprite.Open()) Paint(g, f);
        d.DrawDrawing(sprite);
    }

    private static void Paint(DrawingContext d, CharacterFrame f)
    {
        var t = f.Time;
        var mood = f.Energy switch { <= .5 => Mood.Asleep, <= 20 => Mood.Drowsy, _ => Mood.Awake };
        var poke = f.SincePoke;
        var woken = mood == Mood.Asleep && poke < 2.6;
        if (woken) mood = Mood.Drowsy;
        // An allowance reset: wide awake, two big hops with arms up, then a wave, under a sparkle fountain.
        var party = f.SinceReset < 3.2 && !f.Dragged;
        var cheer = f.SinceReset;
        if (party) { mood = Mood.Awake; woken = false; }

        // A 24-second routine: after 17 quiet seconds, alternately think something through or read.
        var calm = mood == Mood.Awake && !f.Dragged && !f.Hovered && poke > 1.6 && !party;
        var into = (t + 13) % 24 - 17;
        var thinkRound = (int)((t + 13) / 24) % 2 == 0;
        var thinking = calm && thinkRound && into is >= 0 and < 4.2;
        var aha = calm && thinkRound && into is >= 4.2 and < 5.1;
        var reading = calm && !thinkRound && into is >= 0 and < 5.6;
        var yawn = mood == Mood.Drowsy && !woken ? Pulse((t % 13 - 9) / 2.6) : 0;

        var (lift, crouch) = party ? (cheer < 2.1 ? Hop(cheer % .7, cheer < 1.4 ? 15 : 6) : (0, false))
            : poke < 1 ? Hop(poke, mood == Mood.Awake ? 12 : 6) : aha ? Hop(into - 4.2, 6) : (0, false);
        var sink = mood == Mood.Asleep ? 3 : crouch || f.SinceDrop < .2 ? 2 : 0;

        var wander = new Vector(Math.Sin(t * .37) * .45 + Math.Sin(t * .11) * .3, Math.Sin(t * .23 + 1) * .25);
        var gaze = wander + (f.Gaze - wander) * f.Attention;
        if (mood == Mood.Drowsy) gaze = gaze * .5 + new Vector(0, .4);
        var (dx, dy) = ((int)Math.Clamp(Math.Round(gaze.X * 2.4), -2, 2), (int)Math.Clamp(Math.Round(gaze.Y * 3), -4, 3));
        if (thinking) (dx, dy) = (2, -4);   // Claude Code's own look-right pose, toward the spinner
        else if (reading) (dx, dy) = ((int)Math.Round(Math.Sin(t * 2.4)), 2);
        else if (mood == Mood.Asleep) (dx, dy) = (0, 0);

        var blink = t % 5.3 > 5.14 || ((int)(t / 5.3) % 3 == 2 && t % 5.3 is > 4.84 and < 4.98);
        var (eyes, mouth) = f.Dragged ? (Eyes.Wide, Mouth.Oh)
            : party ? (Eyes.Happy, Mouth.Smile)
            : poke < 1.2 ? (woken ? (Eyes.Wide, Mouth.Oh) : (Eyes.Happy, Mouth.Smile))
            : mood == Mood.Asleep ? (Eyes.Sleeping, Mouth.None)
            : mood == Mood.Drowsy ? (yawn > .35 ? (Eyes.Squeezed, Mouth.Yawn) : (Eyes.Lidded, f.Hovered ? Mouth.Smile : Mouth.None))
            : aha ? (Eyes.Happy, Mouth.Smile)
            : f.Hovered ? (Eyes.Open, Mouth.Smile)
            : (Eyes.Open, Mouth.None);
        if (eyes == Eyes.Open && blink || eyes == Eyes.Lidded && t % 6.5 > 5.7) eyes = Eyes.Closed;

        var flail = (int)(t * 6) % 2 == 0 ? Arms.Up : Arms.Rest;
        var (left, right) = f.Dragged ? (flail, flail == Arms.Up ? Arms.Rest : Arms.Up)
            : party ? (Arms.Up, cheer < 2.1 || (int)(t * 5) % 2 == 0 ? Arms.Up : Arms.Rest)
            : poke < .8 || aha || yawn > .35 ? (Arms.Up, Arms.Up)
            : reading ? (Arms.Hold, Arms.Hold)
            : mood != Mood.Awake ? (Arms.Droop, Arms.Droop)
            : f.Hovered ? (Arms.Rest, (int)(t * 5) % 2 == 0 ? Arms.Up : Arms.Rest)   // a wave hello
            : (Arms.Rest, Arms.Rest);

        var body = f.Accent;
        d.PushTransform(new TranslateTransform(0, -Math.Round(lift / Cell) * Cell));
        int[] legs = { 10, 14, 22, 26 };
        for (var i = 0; i < legs.Length; i++)
        {
            var swing = f.Dragged ? ((int)(t * 8) + i) % 2 * 2 - 1 : 0;   // dangle and kick
            Px(d, body, legs[i] + swing, 16 + sink, 2, f.Dragged ? 5 : 4 - sink);
        }
        Px(d, body, 6, sink, 26, 16);
        if (left != Arms.Hold) Arm(d, body, -1, left, sink);
        if (right != Arms.Hold) Arm(d, body, 1, right, sink);

        if (f.Hovered && mood == Mood.Awake || poke < 1.2 && !woken || party)
        {
            Px(d, Blush, 7, 9 + sink, 2, 1);
            Px(d, Blush, 29, 9 + sink, 2, 1);
        }
        Eye(d, -1, 10 + dx, 4 + dy + sink, eyes);
        Eye(d, 1, 26 + dx, 4 + dy + sink, eyes);
        switch (mouth)
        {
            case Mouth.Smile: Px(d, EyeInk, 17, 9 + sink); Px(d, EyeInk, 18, 10 + sink, 2, 1); Px(d, EyeInk, 20, 9 + sink); break;
            case Mouth.Oh: Px(d, EyeInk, 18, 9 + sink, 2, 2); break;
            case Mouth.Yawn: Px(d, EyeInk, 17, 9 + sink, 4, 1 + Math.Round(3 * yawn)); break;
        }

        if (reading)
        {
            Book(d, into, sink);
            Arm(d, body, -1, Arms.Hold, sink);
            Arm(d, body, 1, Arms.Hold, sink);
        }
        if (thinking) Star(d, body, 33, -5, Spin[(int)(t * 8) % Spin.Length]);
        if (aha)
        {
            var k = (into - 4.2) / .9;
            Star(d, Gold, 33, -5, k < .35 ? 4 : Math.Max(0, (int)(4 - (k - .35) / .65 * 5)));
        }
        if (!woken && poke < .8)
            foreach (var (sx, sy) in new[] { (-.6, -1.0), (.6, -1.0), (-1.0, -.4), (1.0, -.4) })
            {
                var reach = poke * 8;
                Star(d, Gold, Math.Round(Mid + sx * (12 + reach)), Math.Round(sy * (4 + reach)), poke < .45 ? 1 : 0);
            }
        if (woken && poke < 1)
        {
            Glyph(d, Gold, 18, -10, 2, 3);
            Glyph(d, Gold, 18, -6, 2, 1);
        }
        if (party && cheer < 1.4) Star(d, Gold, Mid, -6, cheer < .9 ? 4 : (int)(4 - (cheer - .9) / .5 * 4));
        if (party) PartyHat(d, cheer < .12 ? -3 : cheer < .2 ? -1 : 0, sink);
        d.Pop();

        // Sparkles burst up and out in two waves, then fall away and shrink.
        if (party)
            for (var i = 0; i < 10; i++)
            {
                var k = cheer - (i % 2) * .5;
                if (k is < 0 or > 2.2) continue;
                var angle = Math.PI * (.12 + .76 * (i / 2) / 4.0) + (i % 2) * .15;
                var col = Mid + Math.Cos(angle) * k * 9;   // stays inside the 144-DIP window
                var row = 2 - Math.Sin(angle) * k * 14 + k * k * 5;
                Star(d, i % 3 == 0 ? Cream : Gold, Math.Round(col), Math.Round(row), k < .8 ? 2 : k < 1.6 ? 1 : 0);
            }

        if (mood == Mood.Asleep)
            for (var i = 0; i < 3; i++)
            {
                var k = (t * .4 + i / 3.0) % 1;
                if (k is < .08 or > .92) continue;   // pop in and out, pixel style
                var size = k < .5 ? 2.0 : 3.0;
                var at = new Point(Math.Round(24 + k * 14 + Math.Sin(k * 6 + i) * 2), Math.Round(-30 - k * 28));
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < 4; x++)
                        if (ZShape[y][x] == '#')
                        {
                            d.DrawRectangle(Shadow, null, new(at.X + x * size + 1, at.Y + y * size + 1, size, size));
                            d.DrawRectangle(Cream, null, new(at.X + x * size, at.Y + y * size, size, size));
                        }
            }
    }

    // Arm shapes are defined for the left side and mirrored across the centerline.
    private static void Arm(DrawingContext d, Brush b, int side, Arms pose, int sink)
    {
        void Block(double col, double row, double w, double h) => Px(d, b, side < 0 ? col : 2 * Mid - col - w, row + sink, w, h);
        switch (pose)
        {
            case Arms.Rest: Block(2, 8, 4, 4); break;
            case Arms.Up: Block(2, 4, 4, 4); Block(4, 8, 2, 4); break;   // Claude Code's arms-up pose
            case Arms.Droop: Block(2, 10, 4, 4); break;
            case Arms.Hold: Block(2, 11, 7, 4); break;
        }
    }

    // Eye shapes live in a 2×4 box and are mirrored for the right eye.
    private static void Eye(DrawingContext d, int side, double col, double row, Eyes eyes)
    {
        void Dot(Brush b, double x, double y, double w = 1, double h = 1) => Px(d, b, col + (side < 0 ? x : 2 - x - w), row + y, w, h);
        switch (eyes)
        {
            case Eyes.Open: Dot(EyeInk, 0, 0, 2, 4); break;
            case Eyes.Wide: Dot(EyeInk, 0, -1, 2, 5); Dot(Cream, side < 0 ? 0 : 1, -1); break;
            case Eyes.Closed: Dot(EyeInk, 0, 3, 2, 1); break;
            case Eyes.Lidded: Dot(EyeInk, 0, 2, 2, 2); break;
            case Eyes.Happy: Dot(EyeInk, -1, 2); Dot(EyeInk, 0, 1, 2, 1); Dot(EyeInk, 2, 2); break;
            case Eyes.Sleeping: Dot(EyeInk, -1, 2); Dot(EyeInk, 0, 3, 2, 1); Dot(EyeInk, 2, 2); break;
            case Eyes.Squeezed: Dot(EyeInk, 0, 1); Dot(EyeInk, 1, 2); Dot(EyeInk, 0, 3); break;
        }
    }

    // An open book: it pops up, one page turns, and it drops away.
    private static void Book(DrawingContext d, double s, int sink)
    {
        var r = 10 + sink + (s is < .15 or > 5.45 ? 1 : 0);
        Px(d, Cover, 7, r, 24, 7);
        Px(d, Cream, 8, r, 10, 6);
        Px(d, Cream, 20, r, 10, 6);
        foreach (var (col, width) in new[] { (9, 7), (21, 7) })
        {
            Px(d, TextInk, col, r + 1, width, 1);
            Px(d, TextInk, col, r + 3, width - 2, 1);
        }
        var turn = (s - 2.5) / .6;
        var w = Math.Round(10 * Math.Abs(1 - 2 * turn));
        if (turn is > 0 and < 1 && w > 0) Px(d, PageShade, turn < .5 ? 20 : 18 - w, r, w, 6);
    }

    // A striped cone worn at a jaunty angle for resets. It drops on from above and hops along.
    private static void PartyHat(DrawingContext d, int drop, int sink)
    {
        var r = sink + drop;
        foreach (var (row, col, width) in new[] { (-1, 22, 7), (-2, 23, 5), (-3, 24, 4), (-4, 25, 3), (-5, 26, 2), (-6, 27, 1) })
            Px(d, row is -1 or -4 ? Gold : Cover, col, r + row, width, 1);
        Px(d, Gold, 27, r - 8, 2, 2);
    }

    private static void Star(DrawingContext d, Brush b, double col, double row, int size)
    {
        foreach (var (x, y) in Stars[size]) Px(d, b, col + x, row + y);
    }

    // Outlined so it reads on light and dark wallpapers alike.
    private static void Glyph(DrawingContext d, Brush b, double col, double row, double w, double h)
    {
        var at = new Rect((col - Mid) * Cell, Top + row * Cell, w * Cell, h * Cell);
        d.DrawRectangle(Shadow, null, Rect.Offset(at, 1, 1));
        d.DrawRectangle(b, null, at);
    }

    private static void Px(DrawingContext d, Brush b, double col, double row, double w = 1, double h = 1) =>
        d.DrawRectangle(b, null, new((col - Mid) * Cell, Top + row * Cell, w * Cell, h * Cell));

    // Anticipation crouch, a hop, and a landing crouch.
    private static (double Lift, bool Crouch) Hop(double p, double height) => p switch
    {
        < 0 => (0, false),
        < .1 => (0, true),
        < .5 => (height * Math.Sin(Math.PI * (p - .1) / .4), false),
        < .62 => (0, true),
        _ => (0, false)
    };

    private static double Pulse(double x) => x is > 0 and < 1 ? Math.Sin(Math.PI * x) : 0;
    private static T Frozen<T>(T value) where T : Freezable { value.Freeze(); return value; }
}
