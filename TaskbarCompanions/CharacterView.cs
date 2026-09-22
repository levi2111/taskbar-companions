using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace TaskbarCompanions;

public sealed class CharacterView : FrameworkElement
{
    public required CharacterDefinition Character { get; init; }
    public bool Paused { get; set; }
    public double? Energy { get; set; }
    public bool Dragged { get => dragged; set { if (dragged && !value) droppedAt = Now; dragged = value; } }
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private Brush? accent;
    private bool dragged;
    private double pokedAt = double.NegativeInfinity, droppedAt = double.NegativeInfinity, hoveredAt = double.NegativeInfinity, celebratedAt = double.NegativeInfinity, movedAt;
    private System.Drawing.Point pointer;
    private Vector gaze;
    private double attention;
    private int line = Random.Shared.Next(1000);
    private double Now => clock.Elapsed.TotalSeconds;

    public CharacterView() => ToolTip = "";

    // Each hover reveals one of the character's lines; there is no persistent bubble.
    protected override void OnToolTipOpening(System.Windows.Controls.ToolTipEventArgs e)
    {
        if (Character.Dialogue.Length == 0) { e.Handled = true; return; }
        ToolTip = Character.Dialogue[line++ % Character.Dialogue.Length];
    }

    public void Poke() => pokedAt = Now;
    public void Celebrate() => celebratedAt = Now;

    protected override void OnRender(DrawingContext d)
    {
        base.OnRender(d);
        var t = Paused ? 0 : Now;
        accent ??= (Brush)new BrushConverter().ConvertFromString(Character.Accent)!;
        d.DrawEllipse(new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)), null, new(ActualWidth / 2, ActualHeight - 5), 30, 4);
        double bob = Character.Grounded ? 0 : Math.Sin(t * 2) * 2;
        d.PushTransform(new TranslateTransform(ActualWidth / 2, ActualHeight - 43 + bob));
        Character.Draw(d, Paused ? new(t, false, accent) { Energy = Energy } : Observe(t, accent));
        d.Pop();
    }

    private CharacterFrame Observe(double t, Brush accent)
    {
        if (IsMouseOver) hoveredAt = t;
        // Attend to the pointer while it moves, then let attention drift away.
        var screen = System.Windows.Forms.Control.MousePosition;
        if (screen != pointer) { pointer = screen; movedAt = t; }
        var attentive = t - movedAt < 4;
        if (attentive)
        {
            try
            {
                var toward = PointFromScreen(new(screen.X, screen.Y)) - new Point(ActualWidth / 2, ActualHeight - 52);
                var reach = toward.Length;
                gaze += ((reach < 1 ? new Vector() : toward / reach * Math.Min(1, reach / 160)) - gaze) * .35;
            }
            catch (InvalidOperationException) { attentive = false; }
        }
        attention += ((attentive ? 1 : 0) - attention) * .12;
        return new(t, t % 4.3 > 4.12, accent)
        {
            Gaze = gaze, Attention = attention, Hovered = t - hoveredAt < .35, Dragged = dragged,
            SincePoke = t - pokedAt, SinceDrop = t - droppedAt, SinceReset = t - celebratedAt, Energy = Energy
        };
    }
}
