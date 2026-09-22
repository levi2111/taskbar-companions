using System.IO;

namespace TaskbarCompanions;

// Dependency-free checks for quota boundaries and desktop geometry.
internal static class ShellChecks
{
    public static void Run()
    {
        var count = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); count++; }
        var now = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        Check(UsageDisplay.Countdown(null, now) == "reset --", "unknown reset");
        Check(UsageDisplay.Countdown(now, now) == "awaiting refresh", "reset boundary");
        Check(UsageDisplay.Countdown(now.AddSeconds(-1), now) == "awaiting refresh", "past reset");
        Check(UsageDisplay.Countdown(now.AddSeconds(3661), now) == "01:01:01", "short countdown");
        Check(UsageDisplay.Countdown(now.AddHours(49), now) == "2d 01h", "weekly countdown");
        Check(UsageDisplay.IsStale(new(), now), "missing timestamp");
        Check(!UsageDisplay.IsStale(new(UpdatedAt: now.AddMinutes(-4)), now), "fresh timestamp");
        Check(UsageDisplay.IsStale(new(UpdatedAt: now.AddMinutes(-6)), now), "stale timestamp");
        Check(UsageDisplay.IsStale(new(UpdatedAt: now.AddHours(1)), now), "future timestamp");
        var monitor = new Desktop.Rect { Left = -1920, Top = 0, Right = 0, Bottom = 1080 };
        Check(Desktop.Covers(monitor, monitor), "fullscreen on negative-coordinate monitor");
        Check(!Desktop.Covers(new() { Left = -1920, Top = 0, Right = 0, Bottom = 1032 }, monitor), "maximized is not fullscreen");
        Directory.CreateDirectory(JsonUsageProvider.DataDirectory);
        var id = "test-" + Guid.NewGuid().ToString("N");
        var path = Path.Combine(JsonUsageProvider.DataDirectory, id + ".usage.json");
        var provider = new JsonUsageProvider();
        try
        {
            Check(provider.Read(id).Source == "Not connected", "missing bridge file");
            File.WriteAllText(path, "{");
            Check(provider.Read(id).Source == "Usage file unavailable", "partial JSON");
            File.WriteAllText(path, "{\"weekly\":{\"remainingPercent\":101}}");
            Check(provider.Read(id).Source == "Invalid usage values", "out-of-range quota");
            File.WriteAllText(path, "{\"weekly\":{\"remainingPercent\":0},\"session\":{\"remainingPercent\":100}}");
            var result = provider.Read(id);
            Check(result.Weekly?.RemainingPercent == 0 && result.Session?.RemainingPercent == 100, "quota endpoints");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        var log = Path.Combine(Path.GetTempPath(), id + ".jsonl");
        try
        {
            // Weekly listed first to prove classification uses window_minutes, not field order.
            File.WriteAllText(log, "{\"timestamp\":\"2026-09-22T01:10:57Z\",\"payload\":{\"type\":\"token_count\",\"rate_limits\":{\"limit_id\":\"codex\",\"primary\":{\"used_percent\":71.0,\"window_minutes\":10080,\"resets_at\":1790438477},\"secondary\":{\"used_percent\":98.0,\"window_minutes\":300,\"resets_at\":1790044937}}}}\n"
                + "{\"timestamp\":\"2026-09-22T01:11:00Z\",\"payload\":{\"type\":\"token_count\",\"rate_limits\":{\"limit_id\":\"premium\",\"primary\":{\"used_percent\":5.0,\"window_minutes\":300}}}}\n");
            var codex = CodexSessionProvider.Parse(log);
            Check(codex?.Weekly?.RemainingPercent == 29 && codex.Session?.RemainingPercent == 2, "codex windows by duration");
            Check(codex?.Session?.ResetsAt == DateTimeOffset.FromUnixTimeSeconds(1790044937) && codex.UpdatedAt == new DateTimeOffset(2026, 9, 22, 1, 10, 57, TimeSpan.Zero), "codex timestamps, other buckets ignored");
            File.WriteAllText(log, "{\"payload\":{\"rate_limits\":null}}\n");
            Check(CodexSessionProvider.Parse(log) is null, "codex log without limits");
        }
        finally { if (File.Exists(log)) File.Delete(log); }
        Check(UsageDisplay.BarLabel(now.AddDays(3).AddHours(7).AddMinutes(40), now, weekly: true) == "3d", "weekly label rounds days down");
        Check(UsageDisplay.BarLabel(now.AddDays(5).AddHours(14), now, weekly: true) == "6d", "weekly label rounds days up");
        Check(UsageDisplay.BarLabel(now.AddDays(2).AddHours(4).AddMinutes(50), now, weekly: true) == "2d 4h", "weekly label under three days");
        Check(UsageDisplay.BarLabel(now.AddDays(1).AddMinutes(5), now, weekly: true) == "1d", "weekly label, whole day");
        Check(UsageDisplay.BarLabel(now.AddHours(9).AddMinutes(30), now, weekly: true) == "9h", "weekly label under a day");
        Check(UsageDisplay.BarLabel(now.AddHours(4).AddMinutes(12), now, weekly: true) == "4h 12m", "weekly label under five hours");
        Check(UsageDisplay.BarLabel(now.AddHours(2).AddMinutes(13), now, weekly: false) == "2h 13m", "session label");
        Check(UsageDisplay.BarLabel(now.AddMinutes(42), now, weekly: false) == "42m", "session label under an hour");
        Check(UsageDisplay.BarLabel(now.AddSeconds(20), now, weekly: false) == "<1m" && UsageDisplay.BarLabel(now, now, false) == "", "label edges");
        Check(UsageDisplay.Remaining(new(12, now.AddSeconds(-1)), now) == 100 && UsageDisplay.Remaining(new(12, now.AddSeconds(1)), now) == 12, "reset window refills");
        using (var live = System.Text.Json.JsonDocument.Parse("{\"rateLimits\":{\"limitId\":\"codex\",\"primary\":{\"usedPercent\":100,\"windowDurationMins\":300,\"resetsAt\":1790044937},\"secondary\":{\"usedPercent\":71,\"windowDurationMins\":10080,\"resetsAt\":1790438477}},"
            + "\"rateLimitsByLimitId\":{\"codex\":{\"limitId\":\"codex\",\"primary\":{\"usedPercent\":40,\"windowDurationMins\":300,\"resetsAt\":1790044937}}}}"))
        {
            var app = CodexAppServerProvider.Parse(live.RootElement, now);
            Check(app?.Session?.RemainingPercent == 60 && app.Weekly is null && app.UpdatedAt == now, "app server prefers the codex bucket");
        }
        using (var claude = System.Text.Json.JsonDocument.Parse("{\"five_hour\":{\"utilization\":33.0,\"resets_at\":\"2026-09-22T03:40:00.579016+00:00\"},\"seven_day\":{\"utilization\":5.0,\"resets_at\":null},\"seven_day_opus\":null}"))
        {
            var usage = ClaudeUsageProvider.Parse(claude.RootElement, now);
            Check(usage?.Session?.RemainingPercent == 67 && usage.Session.ResetsAt == new DateTimeOffset(2026, 9, 22, 3, 40, 0, 579, TimeSpan.Zero).AddTicks(160)
                && usage.Weekly?.RemainingPercent == 95 && usage.Weekly.ResetsAt is null, "claude usage windows");
        }
        Check(UsageDisplay.Freshness(new(UpdatedAt: now.AddMinutes(-90)), now) == " · as of 1h 30m ago", "freshness age");
        var sheet = Path.Combine(Path.GetTempPath(), id + ".png");
        try
        {
            // Missing or unexpected Codex artwork must fall back to the terminal explorer, not crash.
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(System.Windows.Media.Imaging.BitmapSource.Create(
                1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, new byte[4], 4)));
            using (var stream = File.Create(sheet)) encoder.Save(stream);
            Check(CodexCompanion.LoadFrames(sheet) is null && CodexCompanion.LoadFrames(sheet + ".missing") is null && CodexCompanion.LoadFrames(null) is null, "codex artwork fallback");
        }
        finally { if (File.Exists(sheet)) File.Delete(sheet); }
        // Windows' WebP decoder reports the Codex sheet as opaque Bgr32 with its alpha in the fourth byte.
        System.Windows.Media.Imaging.BitmapSource Pixels(params byte[] bgrx) => System.Windows.Media.Imaging.BitmapSource.Create(
            bgrx.Length / 4, 1, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null, bgrx, bgrx.Length);
        var cutout = CodexCompanion.WithAlpha(Pixels(0, 0, 0, 0, 10, 20, 30, 255));
        var alpha = new byte[8];
        cutout.CopyPixels(alpha, 8, 0);
        Check(cutout.Format == System.Windows.Media.PixelFormats.Bgra32 && alpha[3] == 0 && alpha[7] == 255, "codex artwork keeps its transparency");
        Check(CodexCompanion.WithAlpha(Pixels(10, 20, 30, 0)).Format == System.Windows.Media.PixelFormats.Bgr32, "opaque artwork stays opaque");

        Check(!new AppSettings { ShowClaude = false, ShowCodex = true }.Shows("claude") && new AppSettings { ShowClaude = false, ShowCodex = true }.Shows("codex"), "chosen companions win over detection");
        var claudeHome = Path.Combine(Path.GetTempPath(), id + "-claude");
        try
        {
            Directory.CreateDirectory(claudeHome);
            var chosen = new AppSettings { ClaudeFolder = "  \"" + claudeHome + "\" " };
            Check(chosen.ClaudeFound && chosen.Shows("claude") && chosen.ClaudeDirectory == claudeHome
                && chosen.ClaudeStateFile == Path.Combine(claudeHome, ".claude.json"), "chosen Claude folder is found and holds its state");
            Check(!new AppSettings { ClaudeFolder = claudeHome + "-missing" }.ClaudeFound, "missing Claude folder");
        }
        finally { if (Directory.Exists(claudeHome)) Directory.Delete(claudeHome); }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "checks.txt"), $"PASS: {count} checks");
    }
}
