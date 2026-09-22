using System.Windows;
using System.Windows.Media;

namespace TaskbarCompanions;

// Each agent owns one definition. Replace the drawing independently of the shell.
// Grounded characters keep their feet planted instead of bobbing.
public sealed record CharacterDefinition(string Id, string Name, string Role, string Accent,
    int HomeIndex, Action<DrawingContext, CharacterFrame> Draw, string[] Dialogue, bool Grounded = false);

// What a character can perceive during one animation frame. Drawings may ignore any of it.
public sealed record CharacterFrame(double Time, bool Blink, Brush Accent)
{
    public Vector Gaze { get; init; }        // toward the pointer, length 0–1
    public double Attention { get; init; }   // 1 while the pointer is moving, easing to 0 when it rests
    public bool Hovered { get; init; }
    public bool Dragged { get; init; }
    public double SincePoke { get; init; } = double.PositiveInfinity;
    public double SinceDrop { get; init; } = double.PositiveInfinity;
    public double SinceReset { get; init; } = double.PositiveInfinity;   // an allowance window just reset
    public double? Energy { get; init; }     // lowest known allowance remaining, 0–100; null when unknown
}

public static class CharacterCatalog
{
    public static readonly CharacterDefinition[] All =
    {
        // The Codex pet needs the Codex extension's artwork; without it, the terminal explorer comes along instead.
        CodexCompanion.Frames is not null
            ? new("codex", "CODEX", "Codex pet", "#8EF0CF", 0, CodexCompanion.Draw,
                new[] { "Small steps. Working software.", "Ready when you are.", "One thing at a time. Then we test.", "A little refactor, as a treat.", "Quiet company while you work." },
                Grounded: true)
            : new("codex", "CODEX", "little terminal explorer", "#8EF0CF", 0, TerminalExplorer.Draw,
                new[] { "Small steps. Working software.", "I packed a terminal. And snacks.", "One thing at a time. Then we test.", "A little refactor, as a treat.", "Tiny feet. Big curiosity.", "Tap me for a little celebration." }),
        new("claude", "CLAUDE", "pixel critter · thinks things through", "#D77757", 1, Clawd.Draw,
            new[] { "Let me think about that for a second.", "I'd rather ask than guess.", "Reading the code before touching it.",
                "Small, careful diffs.", "If I'm unsure, I'll say so.", "Show the work, not just the answer.",
                "Tests passing is a lovely feeling.", "Go on, give me a poke." })
    };

}
