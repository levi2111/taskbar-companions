using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskbarCompanions;

// Choose which companions appear, and where to find Claude Code and Codex when they aren't in the usual places.
// Each section says what was found, so it's clear why a companion's bars might stay empty.
public sealed class SettingsWindow : Window
{
    internal static readonly Brush DangerRed = CompanionWindow.Brush("#B71C1C");
    private readonly AppSettings original;
    private readonly Action<AppSettings> apply;
    private readonly CheckBox showClaude, showCodex, liveUsage;
    private readonly TextBox claudeFolder, codexFolder, codexExe;
    private readonly TextBlock claudeStatus, codexStatus, warning;
    private readonly Button save;

    public SettingsWindow(AppSettings settings, Action<AppSettings> apply)
    {
        original = settings;
        this.apply = apply;
        Title = "Taskbar Companions settings";
        Width = 540; SizeToContent = SizeToContent.Height;
        MaxHeight = SystemParameters.WorkArea.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 13;

        var panel = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };

        panel.Children.Add(Heading("Claude"));
        panel.Children.Add(showClaude = Check("Show Clawd, the Claude companion", settings.Shows("claude")));
        panel.Children.Add(Field("Claude Code folder (empty for the default)", claudeFolder = Box(settings.ClaudeFolder), file: false));
        panel.Children.Add(claudeStatus = Note());
        panel.Children.Add(StatusLineHelp());

        panel.Children.Add(Heading("Codex"));
        panel.Children.Add(showCodex = Check("Show the Codex companion", settings.Shows("codex")));
        panel.Children.Add(Field("Codex folder, CODEX_HOME (empty for the default)", codexFolder = Box(settings.CodexFolder), file: false));
        panel.Children.Add(Field("codex.exe (empty to find it automatically)", codexExe = Box(settings.CodexExe), file: true));
        panel.Children.Add(codexStatus = Note());

        liveUsage = new CheckBox
        {
            Content = "Turn on live account usage…", IsChecked = settings.ClaudeLiveUsage,
            Foreground = Brushes.White, FontWeight = FontWeights.SemiBold
        };
        // Only a click asks; an earlier yes loads without asking again.
        liveUsage.Checked += (_, _) => { if (!LiveUsageWarning.Confirm(this)) liveUsage.IsChecked = false; };
        panel.Children.Add(DangerArea(liveUsage));

        panel.Children.Add(warning = Note());
        warning.Text = "Keep at least one companion.";
        warning.Foreground = Brushes.Firebrick;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        save = new Button { Content = "Save", IsDefault = true, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3) };
        save.Click += (_, _) => { apply(Pending); Close(); };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0) };
        // IsCancel only closes windows shown as dialogs; this one isn't.
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        foreach (var box in new[] { showClaude, showCodex }) { box.Checked += (_, _) => Refresh(); box.Unchecked += (_, _) => Refresh(); }
        foreach (var box in new[] { claudeFolder, codexFolder, codexExe }) box.TextChanged += (_, _) => Refresh();
        Refresh();
    }

    // Saving writes both choices down, so a companion found later doesn't appear by surprise.
    private AppSettings Pending => original with
    {
        ShowClaude = showClaude.IsChecked == true,
        ShowCodex = showCodex.IsChecked == true,
        ClaudeFolder = AppSettings.Blank(claudeFolder.Text),
        CodexFolder = AppSettings.Blank(codexFolder.Text),
        CodexExe = AppSettings.Blank(codexExe.Text),
        ClaudeLiveUsage = liveUsage.IsChecked == true
    };

    private void Refresh()
    {
        var s = Pending;
        // Claude Code saves a reading only when it checks usage itself, which is why the bars can lag behind.
        var reading = ClaudeUsageProvider.ReadClaudeCache(s.ClaudeStateFile)?.UpdatedAt is DateTimeOffset at
            ? $"Claude Code last saved a usage reading {UsageDisplay.Age(DateTimeOffset.UtcNow - at)} ago."
            : "Claude Code hasn't saved a usage reading yet.";
        claudeStatus.Text = s.ClaudeFound
            ? $"Found Claude Code data in {s.ClaudeDirectory}. {reading} It saves one when it checks your usage, for example on its /usage screen, not after every reply."
            : $"No Claude Code data in {s.ClaudeDirectory}. The bars stay empty until Claude Code has been used there, or the status line bridge publishes usage.";

        var codex = new List<string>();
        codex.Add(s.CodexExecutable is string exe
            ? $"Found {exe}; up-to-date usage comes from its App Server."
            : "codex.exe not found, so usage comes only from Codex's session logs.");
        var sessions = Path.Combine(s.CodexDirectory, "sessions");
        codex.Add(Directory.Exists(sessions) ? $"Reading session logs in {sessions}." : $"No session logs in {sessions} yet.");
        codex.Add(CodexCompanion.Frames is not null
            ? "The Codex pet's artwork was found in the Codex extension."
            : "The Codex extension's artwork wasn't found, so the terminal explorer comes along instead.");
        codexStatus.Text = string.Join("\n", codex);

        var any = showClaude.IsChecked == true || showCodex.IsChecked == true;
        save.IsEnabled = any;
        warning.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
    }

    // The status line bridge ships beside the exe. This copies the line for Claude Code's settings.json, with its real path.
    private static FrameworkElement StatusLineHelp()
    {
        var script = Path.Combine(AppContext.BaseDirectory, "bridge", "claude-statusline.js");
        var note = Note();
        note.Text = "Optional: fresher numbers while you use Claude Code in a terminal, through the status line bridge (needs Node.js).";
        var copy = new Button { Content = "Copy status line setting", Padding = new Thickness(10, 2, 10, 2), IsEnabled = File.Exists(script) };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText($"\"statusLine\": {{ \"type\": \"command\", \"command\": \"node \\\"{script.Replace('\\', '/')}\\\"\" }}");
                note.Text = "Copied. Paste it into settings.json in your Claude Code folder, as the install guide shows, then restart Claude Code.";
            }
            catch (System.Runtime.InteropServices.ExternalException) { note.Text = "Couldn't reach the clipboard. Try again in a moment."; }
        };
        var guide = AboutWindow.Link("How to set it up", AboutWindow.ProjectUrl + "/blob/main/docs/INSTALL.md#optional-the-status-line-bridge");
        guide.Margin = new Thickness(12, 3, 0, 0);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        row.Children.Add(copy); row.Children.Add(guide);
        var help = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        help.Children.Add(note); help.Children.Add(row);
        return help;
    }

    // Live account usage sits apart, all in red: it reuses Claude Code's login, which Anthropic's terms don't allow other apps to do.
    private static Border DangerArea(CheckBox toggle)
    {
        var area = new StackPanel();
        area.Children.Add(new TextBlock { Text = "Live account usage (risky, off by default)", FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
        area.Children.Add(new TextBlock
        {
            Text = "Asks Anthropic for your Claude usage every two minutes, even while Claude Code is closed, by reusing the login Claude Code saved on this PC. "
                + "Anthropic's terms don't allow other apps to use that login, so turning this on can put your Claude account at risk.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White, Margin = new Thickness(0, 4, 0, 10)
        });
        area.Children.Add(toggle);
        return new Border
        {
            Child = area, Background = DangerRed, BorderBrush = CompanionWindow.Brush("#7F0000"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(14, 10, 14, 12), Margin = new Thickness(0, 22, 0, 0)
        };
    }

    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) };
    private static CheckBox Check(string text, bool value) => new() { Content = text, IsChecked = value, Margin = new Thickness(0, 0, 0, 8) };
    private static TextBox Box(string? value) => new() { Text = value ?? "", Padding = new Thickness(3, 2, 3, 2), VerticalContentAlignment = VerticalAlignment.Center };
    private static TextBlock Note() => new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(0, 2, 0, 0) };

    private static FrameworkElement Field(string label, TextBox box, bool file)
    {
        var browse = new Button { Content = "Browse…", Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(6, 0, 0, 0) };
        browse.Click += (_, _) =>
        {
            var current = AppSettings.Blank(box.Text);
            if (file)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Choose codex.exe", Filter = "codex.exe|codex.exe|Programs (*.exe)|*.exe" };
                if (current is not null && Directory.Exists(Path.GetDirectoryName(current))) dialog.InitialDirectory = Path.GetDirectoryName(current);
                if (dialog.ShowDialog() == true) box.Text = dialog.FileName;
            }
            else
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = label };
                if (current is not null && Directory.Exists(current)) dialog.InitialDirectory = current;
                if (dialog.ShowDialog() == true) box.Text = dialog.FolderName;
            }
        };
        var row = new DockPanel { Margin = new Thickness(0, 3, 0, 6) };
        DockPanel.SetDock(browse, Dock.Right);
        row.Children.Add(browse);
        row.Children.Add(box);
        var field = new StackPanel();
        field.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 2, 0, 0) });
        field.Children.Add(row);
        return field;
    }
}

// Turning live account usage on needs an explicit yes, after reading what it does and what it risks.
internal sealed class LiveUsageWarning : Window
{
    public static bool Confirm(Window owner) => new LiveUsageWarning { Owner = owner }.ShowDialog() == true;

    private LiveUsageWarning()
    {
        Title = "Turn on live account usage?";
        Width = 520; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 13;

        var body = new StackPanel { Margin = new Thickness(20, 6, 20, 20) };
        body.Children.Add(Text("Live account usage asks Anthropic for your Claude plan usage every two minutes, so Clawd's bars stay current "
            + "even while Claude Code is closed, for example while you chat on claude.ai."));
        body.Children.Add(Text("To do that, Taskbar Companions reads the login token Claude Code saved on this PC (.credentials.json in your "
            + "Claude Code folder) and sends it to Anthropic at api.anthropic.com. It's never sent anywhere else, and this app never saves, logs or refreshes it."));
        body.Children.Add(Text("Before you turn it on:", bold: true));
        body.Children.Add(Text("•  Anthropic's terms don't allow this. Its Claude Code documentation says developers \"may not collect, store, "
            + "or intermediate Claude.ai credentials or session tokens\", and that Anthropic may enforce this without notice. "
            + "Your Claude account could be restricted or suspended.", top: 4));
        body.Children.Add(Text("•  It uses an undocumented Anthropic address that can change or stop working at any time.", top: 4));
        body.Children.Add(Text("•  Security software may flag or block an app that reads another app's login file.", top: 4));
        body.Children.Add(Text("Without it, the bars still update whenever Claude Code checks your usage."));
        var policy = AboutWindow.Link("Read Anthropic's policy on credential use", "https://code.claude.com/docs/en/legal-and-compliance");
        policy.Margin = new Thickness(0, 10, 0, 0);
        body.Children.Add(policy);

        var accept = new CheckBox { Content = "I have read this and accept the risk to my Claude account.", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 0) };
        body.Children.Add(accept);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var turnOn = new Button { Content = "Turn on", IsEnabled = false, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3) };
        turnOn.Click += (_, _) => DialogResult = true;
        // Cancel is the default, so Enter or Esc leaves it off.
        var cancel = new Button { Content = "Cancel", IsCancel = true, IsDefault = true, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0) };
        accept.Checked += (_, _) => turnOn.IsEnabled = true;
        accept.Unchecked += (_, _) => turnOn.IsEnabled = false;
        buttons.Children.Add(turnOn); buttons.Children.Add(cancel);
        body.Children.Add(buttons);

        var panel = new StackPanel();
        panel.Children.Add(new Border
        {
            Background = SettingsWindow.DangerRed, Padding = new Thickness(20, 12, 20, 12),
            Child = new TextBlock { Text = "This can put your Claude account at risk.", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap }
        });
        panel.Children.Add(body);
        Content = panel;
    }

    private static TextBlock Text(string text, double top = 10, bool bold = false) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0),
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
    };
}
