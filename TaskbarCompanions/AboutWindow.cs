using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TaskbarCompanions;

// Version, license, privacy and trademark notices, with links to the full documents on GitHub.
public sealed class AboutWindow : Window
{
    public const string ProjectUrl = "https://github.com/levi2111/taskbar-companions";

    // The version the build stamps into the exe, e.g. "1.0.0", without the commit a build may append.
    public static string Version { get; } = (typeof(AboutWindow).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

    public AboutWindow()
    {
        Title = "About Taskbar Companions";
        Width = 480; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI"); FontSize = 13;

        var panel = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };
        panel.Children.Add(new TextBlock { Text = "Taskbar Companions", FontSize = 18, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(Paragraph($"Version {Version}", top: 0, dim: true));
        panel.Children.Add(Paragraph("Desktop companions that show how much of your Claude and Codex plan allowance is left, as HP and MP bars on the taskbar."));
        panel.Children.Add(Paragraph("Copyright © 2026 levi2111. Free and open source under the MIT License, provided as is, without warranty of any kind."));
        panel.Children.Add(Paragraph("Privacy: no accounts, ads, tracking or telemetry. The app reads the usage Claude Code and Codex keep on this PC, "
            + "and asks Codex's own App Server, which checks with OpenAI using your Codex login. It connects to the internet itself "
            + "only if you turn on live account usage in Settings."));
        panel.Children.Add(Paragraph("Unofficial fan project, not affiliated with, endorsed by or sponsored by Anthropic or OpenAI. "
            + "Claude, Claude Code and Clawd are trademarks of Anthropic; Codex and ChatGPT are trademarks of OpenAI. "
            + "Clawd is Anthropic's character, redrawn here as fan art and not covered by this app's license. "
            + "The Codex pet artwork belongs to OpenAI: it's read from your own Codex extension and never included.", dim: true));

        var links = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        foreach (var (label, page) in new[]
        {
            ("Website and source code", ""), ("Install guide", "/blob/main/docs/INSTALL.md"), ("Privacy policy", "/blob/main/PRIVACY.md"),
            ("License", "/blob/main/LICENSE"), ("Third-party notices", "/blob/main/THIRD-PARTY-NOTICES.md"), ("Report a problem", "/issues")
        })
            links.Children.Add(Link(label, ProjectUrl + page));
        var folder = new Hyperlink(new Run("Open settings folder")) { ToolTip = JsonUsageProvider.DataDirectory };
        folder.Click += (_, _) =>
        {
            try { Directory.CreateDirectory(JsonUsageProvider.DataDirectory); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return; }
            Open(JsonUsageProvider.DataDirectory);
        };
        links.Children.Add(new TextBlock(folder) { Margin = new Thickness(0, 0, 16, 4) });
        panel.Children.Add(links);

        var close = new Button { Content = "Close", IsDefault = true, IsCancel = true, MinWidth = 84, Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        close.Click += (_, _) => Close();
        panel.Children.Add(close);
        Content = panel;
    }

    private static TextBlock Paragraph(string text, double top = 10, bool dim = false) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0),
        Foreground = dim ? Brushes.DimGray : SystemColors.ControlTextBrush
    };

    // A link to a fixed https address, opened in the default browser.
    internal static TextBlock Link(string label, string url)
    {
        var link = new Hyperlink(new Run(label)) { ToolTip = url };
        link.Click += (_, _) => Open(url);
        return new TextBlock(link) { Margin = new Thickness(0, 0, 16, 4) };
    }

    internal static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
    }
}
