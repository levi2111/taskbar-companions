using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskbarCompanions;

// Choose which companions appear, and where to find Claude Code and Codex when they aren't in the usual places.
// Each section says what was found, so it's clear why a companion's bars might stay empty.
public sealed class SettingsWindow : Window
{
    private readonly AppSettings original;
    private readonly Action<AppSettings> apply;
    private readonly CheckBox showClaude, showCodex;
    private readonly TextBox claudeFolder, codexFolder, codexExe;
    private readonly TextBlock claudeStatus, codexStatus, warning;
    private readonly Button save;

    public SettingsWindow(AppSettings settings, Action<AppSettings> apply)
    {
        original = settings;
        this.apply = apply;
        Title = "Taskbar Companions settings";
        Width = 540; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 13;

        var panel = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };

        panel.Children.Add(Heading("Claude"));
        panel.Children.Add(showClaude = Check("Show Clawd, the Claude companion", settings.Shows("claude")));
        panel.Children.Add(Field("Claude Code folder (empty for the default)", claudeFolder = Box(settings.ClaudeFolder), file: false));
        panel.Children.Add(claudeStatus = Note());

        panel.Children.Add(Heading("Codex"));
        panel.Children.Add(showCodex = Check("Show the Codex companion", settings.Shows("codex")));
        panel.Children.Add(Field("Codex folder, CODEX_HOME (empty for the default)", codexFolder = Box(settings.CodexFolder), file: false));
        panel.Children.Add(Field("codex.exe (empty to find it automatically)", codexExe = Box(settings.CodexExe), file: true));
        panel.Children.Add(codexStatus = Note());

        panel.Children.Add(warning = Note());
        warning.Text = "Keep at least one companion.";
        warning.Foreground = Brushes.Firebrick;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        save = new Button { Content = "Save", IsDefault = true, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3) };
        save.Click += (_, _) => { apply(Pending); Close(); };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 0, 0, 0) };
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = panel;

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
        CodexExe = AppSettings.Blank(codexExe.Text)
    };

    private void Refresh()
    {
        var s = Pending;
        claudeStatus.Text = s.ClaudeFound
            ? $"Found Claude Code data in {s.ClaudeDirectory}."
            : $"No Claude Code data in {s.ClaudeDirectory}. The bars stay empty until Claude Code has been used there, or the status line bridge publishes usage.";

        var codex = new List<string>();
        codex.Add(s.CodexExecutable is string exe
            ? $"Found {exe}; live usage comes from its App Server."
            : "codex.exe not found, so there's no live usage from the App Server.");
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
