using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TaskbarCompanions;

public sealed class CompanionWindow : Window
{
    private readonly CharacterDefinition character;
    private readonly int home;
    private readonly CharacterView sprite;
    private readonly IUsageProvider provider;
    private readonly DispatcherTimer timer;
    private readonly QuotaRow weekly;
    private readonly QuotaRow session;
    private UsageSnapshot usage = new();
    private bool demo, paused, dragging, dismissed, docked = true;
    private DateTimeOffset nextRead, lastCheck = DateTimeOffset.UtcNow;
    private Point? press;
    private MenuItem dockMenu = null!;
    private MenuItem? liveMenu;
    private string SettingsPath => Path.Combine(JsonUsageProvider.DataDirectory, character.Id + ".position.json");

    public string CharacterId => character.Id;

    // `home` is this companion's place among the visible ones, counted from the right end of the taskbar.
    public CompanionWindow(CharacterDefinition character, int home)
    {
        this.character = character;
        this.home = home;
        // Codex polls its account through the official App Server and falls back to its session logs.
        // Claude reads local data (Claude Code's cache, the status line bridge) unless live usage is turned on.
        provider = character.Id == "codex" ? new CodexAppServerProvider() : new ClaudeUsageProvider();
        Title = character.Name + " companion";
        Width = 144; Height = 144;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanMinimize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ShowActivated = false; Topmost = true;
        UseLayoutRounding = true;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });
        var stack = new StackPanel { Width = 128, VerticalAlignment = VerticalAlignment.Center };
        root.Children.Add(stack);
        weekly = new QuotaRow("HP", "#38E866", weekly: true);
        session = new QuotaRow("MP", "#2894FF", weekly: false);
        stack.Children.Add(weekly); stack.Children.Add(session);

        sprite = new CharacterView { Character = character, Width = 120, Height = 108, Cursor = System.Windows.Input.Cursors.Hand };
        Grid.SetRow(sprite, 1); root.Children.Add(sprite);
        Content = root;
        ContextMenu = MakeMenu();
        root.MouseLeftButtonDown += (_, e) => { press = e.GetPosition(this); };
        root.MouseMove += (_, e) =>
        {
            if (press is not Point start || e.LeftButton != MouseButtonState.Pressed) return;
            var now = e.GetPosition(this);
            if ((now - start).Length < 5) return;
            press = null; dragging = sprite.Dragged = true;
            try { DragMove(); } catch (InvalidOperationException) { }
            finally { dragging = sprite.Dragged = false; }
            if (docked) Desktop.Dock(this, home, false);
            SavePosition();
        };
        root.MouseLeftButtonUp += (_, _) =>
        {
            // A press that never became a drag is a poke.
            if (press is not null && sprite.IsMouseOver) sprite.Poke();
            press = null;
        };
        Loaded += (_, _) => RestorePosition();
        Closing += (_, e) => { if (dismissed) return; e.Cancel = true; Minimize(); };
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        timer.Tick += (_, _) => Tick(); timer.Start();
    }

    private ContextMenu MakeMenu()
    {
        var menu = new ContextMenu();
        void Item(string label, Action action) { var item = new MenuItem { Header = label }; item.Click += (_, _) => action(); menu.Items.Add(item); }
        Item("Minimize companions", Minimize);
        var dock = dockMenu = new MenuItem { Header = "Sit on taskbar", IsCheckable = true, IsChecked = true };
        dock.Click += (_, _) => { docked = dock.IsChecked; if (docked) Desktop.Dock(this, home, false); SavePosition(); };
        menu.Items.Add(dock);
        Item("Pause / resume animation", ToggleAnimation);
        Item("Demo usage on / off", ToggleDemo);
        Item("Return home", ResetPosition);
        Item("Preview reset celebration", PreviewReset);
        if (provider is ClaudeUsageProvider claude)
        {
            var live = liveMenu = new MenuItem
            {
                Header = "Live account usage", IsCheckable = true, IsChecked = claude.Live,
                ToolTip = "Uses Claude Code's saved login to ask Anthropic's undocumented usage endpoint every two minutes. Off by default."
            };
            live.Click += (_, _) => { claude.Live = live.IsChecked; nextRead = default; SavePosition(); };
            menu.Items.Add(live);
        }
        menu.Items.Add(new Separator());
        var app = (App)System.Windows.Application.Current;
        Item("Settings…", app.OpenSettings);
        var hide = new MenuItem { Header = "Hide this companion", ToolTip = "Bring it back from Settings." };
        hide.Click += (_, _) => app.HideCompanion(character.Id);
        menu.Items.Add(hide);
        // The last visible companion stays; the app would have nothing left on screen.
        menu.Opened += (_, _) => hide.IsEnabled = app.CompanionCount > 1;
        Item("Quit application", () => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    private void Tick()
    {
        var now = DateTimeOffset.UtcNow;
        // Poll desktop and usage at low frequency; only animation runs at 20 fps.
        if (now >= nextRead)
        {
            nextRead = now.AddSeconds(1);
            var fullscreen = Desktop.ForegroundIsFullscreen();
            // Keep the taskbar button available, and never undo a user's minimize.
            Opacity = fullscreen ? 0 : 1;
            IsHitTestVisible = !fullscreen;
            if (!fullscreen && WindowState != WindowState.Minimized && docked && !dragging) Desktop.Dock(this, home, false);
            if (!demo) usage = provider.Read(character.Id);
            // A window resetting while we watch is worth a celebration and a fresh reading.
            bool Crossed(Quota? q) => q?.ResetsAt is DateTimeOffset r && r > lastCheck && r <= now;
            var reset = (Weekly: Crossed(usage.Weekly), Session: Crossed(usage.Session));
            lastCheck = now;
            sprite.Energy = new[] { UsageDisplay.Remaining(usage.Weekly, now), UsageDisplay.Remaining(usage.Session, now) }.Min();
            var freshness = demo ? "" : UsageDisplay.Freshness(usage, now);
            weekly.Update(usage.Weekly, now, freshness, usage.Source);
            session.Update(usage.Session, now, freshness, usage.Source);
            if (reset.Weekly || reset.Session)
            {
                provider.Refresh();
                Celebrate(reset.Weekly ? weekly : null, reset.Session ? session : null);
            }
        }
        var visible = Opacity > 0 && WindowState != WindowState.Minimized;
        if (visible) { weekly.Step(); session.Step(); }
        if (visible && !paused) sprite.InvalidateVisual();
    }

    public void PreviewReset() => Celebrate(weekly, session);

    private void Celebrate(params QuotaRow?[] rows)
    {
        foreach (var row in rows) row?.Refill();
        sprite.Celebrate();
    }

    public void ToggleDemo()
    {
        demo = !demo;
        var now = DateTimeOffset.UtcNow;
        usage = demo ? new(new(character.Id == "codex" ? 76 : 62, now.AddDays(3).AddHours(7)), new(character.Id == "codex" ? 48 : 83, now.AddHours(2).AddMinutes(14)), now, "Demo") : new();
        nextRead = default;
    }

    public void ToggleAnimation() { paused = !paused; sprite.Paused = paused; sprite.InvalidateVisual(); }
    public void Minimize() => ((App)System.Windows.Application.Current).MinimizeCompanions();
    public void Restore() { ((App)System.Windows.Application.Current).RestoreCompanions(); nextRead = default; }
    public void ResetPosition() { Restore(); docked = true; dockMenu.IsChecked = true; Desktop.Dock(this, home, true); SavePosition(); }
    public void Cleanup() { timer.Stop(); provider.Dispose(); SavePosition(); }

    // Closes for good, when the set of companions changes; closing from the window itself only minimizes.
    public void Dismiss() { Cleanup(); dismissed = true; Close(); }

    internal void SavePreview()
    {
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render((Visual)Content);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(AppContext.BaseDirectory, character.Id + ".preview.png"));
        encoder.Save(stream);
    }

    private void RestorePosition()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var p = JsonSerializer.Deserialize<SavedPosition>(File.ReadAllText(SettingsPath));
                if (p is not null && double.IsFinite(p.Left) && double.IsFinite(p.Top))
                {
                    Left = p.Left; Top = p.Top; docked = p.Docked;
                    if (provider is ClaudeUsageProvider claude && p.LiveUsage == true)
                    {
                        claude.Live = true;
                        if (liveMenu is not null) liveMenu.IsChecked = true;
                    }
                    dockMenu.IsChecked = docked;
                    // Recover disconnected monitors or old off-screen positions.
                    var visible = System.Windows.Forms.Screen.AllScreens.Any(s =>
                    {
                        var dpi = VisualTreeHelper.GetDpi(this);
                        return s.WorkingArea.IntersectsWith(new System.Drawing.Rectangle((int)(Left * dpi.DpiScaleX), (int)(Top * dpi.DpiScaleY), (int)(Width * dpi.DpiScaleX), (int)(Height * dpi.DpiScaleY)));
                    });
                    if (!visible) ResetPosition();
                    else if (docked) Desktop.Dock(this, home, false);
                    return;
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        ResetPosition();
    }

    private void SavePosition()
    {
        try { Directory.CreateDirectory(JsonUsageProvider.DataDirectory); File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new SavedPosition(Left, Top, docked, provider is ClaudeUsageProvider claude ? claude.Live : null))); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private sealed record SavedPosition(double Left, double Top, bool Docked, bool? LiveUsage = null);
    internal static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
    internal static TextBlock Text(string value, double size, string color) => new() { Text = value, FontSize = size, Foreground = Brush(color), FontFamily = new FontFamily("Segoe UI") };
}

internal sealed class QuotaRow : FrameworkElement
{
    private static readonly Typeface Face = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly string stat;
    private readonly bool weekly;
    private readonly Color color;
    private readonly Brush fill;
    private double percent, shown, refilledAt = double.NegativeInfinity;
    private string label = "";

    public QuotaRow(string stat, string color, bool weekly)
    {
        this.stat = stat;
        this.weekly = weekly;
        this.color = ((SolidColorBrush)CompanionWindow.Brush(color)).Color;
        fill = new LinearGradientBrush(Colors.White, this.color, 90);
        Height = 14;
        Margin = new Thickness(0, 2, 0, 2);
        SnapsToDevicePixels = true;
        System.Windows.Automation.AutomationProperties.SetName(this, stat);
    }

    private double SinceRefill => Clock.Elapsed.TotalSeconds - refilledAt;

    protected override void OnRender(DrawingContext drawing)
    {
        var inner = new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4));
        drawing.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));
        if (shown > 0) drawing.DrawRectangle(fill, null, new Rect(inner.X, inner.Y, inner.Width * shown / 100, inner.Height));

        // Refill: a shine sweeps along the bar while the track glows in the bar's color.
        var s = SinceRefill;
        if (s < 1.6)
        {
            var glow = (byte)(200 * (1 - s / 1.6) * (.6 + .4 * Math.Cos(s * 12)));
            drawing.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(glow, color.R, color.G, color.B)), 2), new Rect(1, 1, ActualWidth - 2, ActualHeight - 2));
            var x = inner.X + (inner.Width + 24) * Math.Min(1, s / .9) - 12;
            var shine = new LinearGradientBrush(new GradientStopCollection
            {
                new(Color.FromArgb(0, 255, 255, 255), 0), new(Color.FromArgb(220, 255, 255, 255), .5), new(Color.FromArgb(0, 255, 255, 255), 1)
            }, 0);
            drawing.PushClip(new RectangleGeometry(inner));
            drawing.DrawRectangle(shine, null, new Rect(x - 12, inner.Y, 24, inner.Height));
            drawing.Pop();
        }

        if (label.Length == 0) return;
        // White with a black outline stays legible over the white-to-color fill and the empty track.
        var text = new FormattedText(label, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Face, 9,
            Brushes.White, null, TextFormattingMode.Display, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var origin = new Point(Math.Round(ActualWidth - 4 - text.Width), Math.Round((ActualHeight - text.Height) / 2));
        text.SetForegroundBrush(Brushes.Black);
        foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, 1), (-1, 1), (1, -1) })
            drawing.DrawText(text, origin + new Vector(dx, dy));
        text.SetForegroundBrush(Brushes.White);
        drawing.DrawText(text, origin);
    }

    public void Update(Quota? quota, DateTimeOffset now, string freshness, string source)
    {
        var remaining = UsageDisplay.Remaining(quota, now);
        var expired = quota?.ResetsAt <= now;
        percent = remaining ?? 0;
        label = UsageDisplay.BarLabel(quota?.ResetsAt, now, weekly);
        ToolTip = $"{stat}: {(remaining is null ? "unknown" : $"{percent:0}% remaining")} · {source} · "
            + (expired == true ? "reset, full until the next request" : UsageDisplay.Countdown(quota?.ResetsAt, now))
            + freshness;
        InvalidateVisual();
    }

    // Drain and pour back in, for a reset.
    public void Refill() { refilledAt = Clock.Elapsed.TotalSeconds; shown = 0; }

    // Called every animation frame: ease the fill toward its value; snap for ordinary updates.
    public void Step()
    {
        var animating = SinceRefill < 1.6;
        if (!animating && shown == percent) return;
        shown = animating ? shown + (percent - shown) * .12 : percent;
        InvalidateVisual();
    }
}
