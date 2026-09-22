using System.Windows;
using Forms = System.Windows.Forms;

namespace TaskbarCompanions;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? tray;
    private readonly List<CompanionWindow> companions = new();
    private Mutex? instance;
    private EventWaitHandle? showRequest;
    private System.Windows.Threading.DispatcherTimer? requestTimer;
    private bool changingWindowState;

    public void MinimizeCompanions() => SetCompanionState(WindowState.Minimized);
    public void RestoreCompanions() => SetCompanionState(WindowState.Normal);

    private void SetCompanionState(WindowState state)
    {
        if (changingWindowState) return;
        changingWindowState = true;
        try
        {
            foreach (var window in companions)
            {
                window.WindowState = state;
                if (state == WindowState.Normal) window.Show();
            }
            if (state == WindowState.Normal) MainWindow?.Activate();
        }
        finally { changingWindowState = false; }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Desktop.SetApplicationIdentity();
        if (e.Args.Contains("--self-test"))
        {
            try { ShellChecks.Run(); Shutdown(0); }
            catch (Exception error) { System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "checks.txt"), error.ToString()); Shutdown(1); }
            return;
        }
        // Desktop scope prevents a background/sandbox desktop from blocking the user's desktop.
        var scope = Desktop.InstanceScope() + (e.Args.Contains("--smoke-test") ? ".SmokeTest" : "");
        showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\TaskbarCompanions.Show." + scope);
        instance = new Mutex(true, "Local\\TaskbarCompanions." + scope, out var created);
        if (!created) { showRequest.Set(); Shutdown(); return; }
        foreach (var character in CharacterCatalog.All)
        {
            var window = new CompanionWindow(character);
            if (companions.Count == 0)
            {
                MainWindow = window;
                window.Title = "Taskbar Companions";
                window.ShowInTaskbar = true;
                window.StateChanged += (_, _) => SetCompanionState(window.WindowState);
            }
            else
            {
                // Owned windows keep independent positions and behavior, but share
                // the primary window's taskbar and Alt+Tab entry.
                window.Owner = MainWindow;
            }
            companions.Add(window);
            window.Show();
        }
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Restore companions", null, (_, _) => RestoreCompanions());
        menu.Items.Add("Minimize companions", null, (_, _) => MinimizeCompanions());
        menu.Items.Add("Bring both home", null, (_, _) => companions.ForEach(w => w.ResetPosition()));
        menu.Items.Add("Demo usage on / off", null, (_, _) => companions.ForEach(w => w.ToggleDemo()));
        menu.Items.Add("Pause / resume both", null, (_, _) => companions.ForEach(w => w.ToggleAnimation()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Shutdown());
        tray = new Forms.NotifyIcon { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Text = "Taskbar companions", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => RestoreCompanions();
        requestTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        requestTimer.Tick += (_, _) =>
        {
            if (showRequest.WaitOne(0)) RestoreCompanions();
        };
        requestTimer.Start();
        if (e.Args.Contains("--smoke-test"))
        {
            // Exercise the same state change Windows sends from the taskbar.
            MainWindow.WindowState = WindowState.Minimized;
            var phase = 0;
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (_, _) =>
            {
                if (phase++ == 0)
                {
                    if (companions.Any(w => w.WindowState != WindowState.Minimized)
                        || companions.Count(w => w.ShowInTaskbar) != 1
                        || companions.Skip(1).Any(w => w.Owner != MainWindow))
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "desktop-checks.txt"), "FAIL: minimize/taskbar state");
                        Shutdown(1);
                        return;
                    }
                    MainWindow.WindowState = WindowState.Normal;
                    companions.ForEach(w => { w.ToggleDemo(); w.PreviewReset(); });
                    return;
                }
                timer.Stop();
                foreach (var window in companions) window.SavePreview();
                var restored = companions.All(w => w.WindowState == WindowState.Normal && w.IsVisible);
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "desktop-checks.txt"), restored ? "PASS: one taskbar entry, owned companions, shared persistent minimize and restore, rendered previews" : "FAIL: restore");
                Shutdown(restored ? 0 : 1);
            };
            timer.Start();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        foreach (var window in companions) window.Cleanup();
        tray?.Dispose();
        requestTimer?.Stop();
        showRequest?.Dispose();
        instance?.Dispose();
        base.OnExit(e);
    }
}
