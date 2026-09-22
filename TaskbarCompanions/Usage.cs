using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskbarCompanions;

public sealed record Quota(double? RemainingPercent = null, DateTimeOffset? ResetsAt = null);
public sealed record UsageSnapshot(Quota? Weekly = null, Quota? Session = null,
    DateTimeOffset? UpdatedAt = null, string Source = "Not connected");

public interface IUsageProvider
{
    UsageSnapshot Read(string characterId);
    void Refresh() { }   // ask for new data soon, e.g. right after a reset
    void Dispose() { }
}

// A stable local bridge: any tool can publish usage for an agent by writing its JSON file.
public sealed class JsonUsageProvider : IUsageProvider
{
    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "data");
    public UsageSnapshot Read(string characterId)
    {
        var path = Path.Combine(DataDirectory, characterId + ".usage.json");
        if (!File.Exists(path)) return new();
        try
        {
            var snapshot = JsonSerializer.Deserialize<UsageSnapshot>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (snapshot is null) return new(Source: "Empty usage file");
            if (!Valid(snapshot.Weekly) || !Valid(snapshot.Session)) return new(Source: "Invalid usage values");
            return snapshot with { Source = "Local bridge" };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        { return new(Source: "Usage file unavailable"); }
    }

    private static bool Valid(Quota? q) => q?.RemainingPercent is not double p || (double.IsFinite(p) && p >= 0 && p <= 100);
}

// Live Codex limits from the official App Server (`codex app-server`, JSON-RPC over stdio):
// `account/rateLimits/read` returns the ChatGPT plan's windows without sending a message or
// spending allowance. Uses the codex.exe bundled with the Codex VS Code extension, unless another is chosen.
public sealed class CodexAppServerProvider : IUsageProvider
{
    private static readonly TimeSpan PollEvery = TimeSpan.FromSeconds(60);
    private readonly IUsageProvider fallback = new CodexSessionProvider();
    private readonly object gate = new();
    private System.Diagnostics.Process? server;
    private bool ready;
    private int nextId = 2;
    private DateTime nextPoll, nextStart;
    private volatile UsageSnapshot? latest;

    public UsageSnapshot Read(string characterId)
    {
        var now = DateTime.UtcNow;
        lock (gate)
        {
            if ((server is null || server.HasExited) && now >= nextStart) Start(now);
            if (ready && now >= nextPoll) { nextPoll = now + PollEvery; Send($"{{\"id\":{nextId++},\"method\":\"account/rateLimits/read\"}}"); }
        }
        // Between polls the session logs may be newer (they update after every Codex reply).
        var logged = fallback.Read(characterId);
        return latest is UsageSnapshot live && !(logged.UpdatedAt > live.UpdatedAt) ? live : logged;
    }

    public void Refresh() { lock (gate) nextPoll = DateTime.UtcNow.AddSeconds(3); }

    public void Dispose()
    {
        lock (gate)
        {
            try { if (server is { HasExited: false }) server.Kill(); } catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
            server = null; ready = false;
        }
    }

    private void Start(DateTime now)
    {
        nextStart = now.AddMinutes(1);   // back off if it fails or exits
        ready = false;
        server = null;
        if (AppSettings.Current.CodexExecutable is not string exe) return;
        var process = new System.Diagnostics.Process
        {
            StartInfo = new(exe, "app-server")
            {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetTempPath(),
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            },
            EnableRaisingEvents = true
        };
        process.OutputDataReceived += (_, e) => { if (e.Data is string line) Receive(line); };
        process.ErrorDataReceived += (_, _) => { };
        try { process.Start(); } catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { return; }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        server = process;
        Send("{\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"taskbar-companions\",\"version\":\"1.0\"}}}");
    }

    private void Send(string message)
    {
        try { server?.StandardInput.WriteLine(message); server?.StandardInput.Flush(); }
        catch (Exception e) when (e is IOException or InvalidOperationException) { }
    }

    private void Receive(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line);
            var message = json.RootElement;
            if (message.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == 1)
            {
                lock (gate) { Send("{\"method\":\"initialized\"}"); ready = true; nextPoll = DateTime.MinValue; }
                return;
            }
            // Responses carry the limits in `result`; `account/rateLimits/updated` pushes them in `params`.
            if (!message.TryGetProperty("result", out var body) && !message.TryGetProperty("params", out body)) return;
            if (Parse(body, DateTimeOffset.UtcNow) is UsageSnapshot snapshot) latest = snapshot;
        }
        catch (JsonException) { }
    }

    internal static UsageSnapshot? Parse(JsonElement body, DateTimeOffset at)
    {
        if (body.ValueKind != JsonValueKind.Object) return null;
        var limits = body.TryGetProperty("rateLimitsByLimitId", out var byId) && byId.ValueKind == JsonValueKind.Object && byId.TryGetProperty("codex", out var codex) ? codex
            : body.TryGetProperty("rateLimits", out var general) ? general : default;
        if (limits.ValueKind != JsonValueKind.Object) return null;
        if (limits.TryGetProperty("limitId", out var limitId) && limitId.ValueKind == JsonValueKind.String && limitId.GetString() != "codex") return null;
        Quota? weekly = null, session = null;
        foreach (var name in new[] { "primary", "secondary" })
        {
            if (!limits.TryGetProperty(name, out var window) || window.ValueKind != JsonValueKind.Object) continue;
            if (!window.TryGetProperty("usedPercent", out var used) || used.ValueKind != JsonValueKind.Number) continue;
            DateTimeOffset? resets = window.TryGetProperty("resetsAt", out var r) && r.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(r.GetInt64()) : null;
            var quota = new Quota(Math.Clamp(100 - used.GetDouble(), 0, 100), resets);
            var minutes = window.TryGetProperty("windowDurationMins", out var m) && m.ValueKind == JsonValueKind.Number ? m.GetDouble() : 0;
            if (minutes >= 24 * 60) weekly = quota; else if (minutes > 0) session = quota;
        }
        return weekly is null && session is null ? null : new(weekly, session, at, "Codex account (live)");
    }

    // A codex.exe chosen in Settings, then CODEX_PATH, the Codex extension, and PATH.
    internal static string? FindCodex(string? chosen)
    {
        if (chosen is not null && File.Exists(chosen)) return chosen;
        if (Environment.GetEnvironmentVariable("CODEX_PATH") is string custom && File.Exists(custom)) return custom;
        var exe = ExtensionDirectories()
            .Select(d => Path.Combine(d.FullName, "bin", "windows-x86_64", "codex.exe"))
            .FirstOrDefault(File.Exists);
        if (exe is not null) return exe;
        return (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Select(dir => { try { return Path.Combine(dir.Trim(), "codex.exe"); } catch (ArgumentException) { return ""; } })
            .FirstOrDefault(File.Exists);
    }

    // Installed copies of the Codex extension for VS Code, VS Code Insiders and Cursor, newest first per editor.
    internal static IEnumerable<DirectoryInfo> ExtensionDirectories()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var editor in new[] { ".vscode", ".vscode-insiders", ".cursor" })
        {
            var extensions = new DirectoryInfo(Path.Combine(home, editor, "extensions"));
            if (!extensions.Exists) continue;
            foreach (var d in extensions.EnumerateDirectories("openai.chatgpt-*").OrderByDescending(d => d.LastWriteTimeUtc))
                yield return d;
        }
    }
}

// Claude limits. By default only from local sources: the usage reading Claude Code caches in
// .claude.json, and the status line bridge. Opting in to live usage also polls the endpoint behind
// Claude Code's /usage screen. That endpoint is undocumented, so it may change; it reads the login
// Claude Code stores and never refreshes or sends it anywhere else. Checking usage sends no message
// and spends no allowance.
public sealed class ClaudeUsageProvider : IUsageProvider
{
    private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly TimeSpan PollEvery = TimeSpan.FromMinutes(2);
    private readonly IUsageProvider fallback = new JsonUsageProvider();
    private readonly object gate = new();
    private DateTime nextPoll;
    private bool polling;
    private volatile bool enabled;
    private volatile UsageSnapshot? latest;
    private volatile string? problem;

    // Off by default; turned on from Claude's right-click menu.
    public bool Live
    {
        get => enabled;
        set
        {
            enabled = value;
            // Drop any live reading; the next poll takes the cache again.
            if (!value) { latest = null; problem = null; }
            Refresh();
        }
    }

    private static string CredentialsPath => Path.Combine(AppSettings.Current.ClaudeDirectory, ".credentials.json");

    public UsageSnapshot Read(string characterId)
    {
        lock (gate)
            if (!polling && DateTime.UtcNow >= nextPoll)
            {
                polling = true;
                nextPoll = DateTime.UtcNow + PollEvery;
                _ = Task.Run(Poll);
            }
        var bridged = fallback.Read(characterId);
        var live = latest;
        var snapshot = live is not null && !(bridged.UpdatedAt > live.UpdatedAt) ? live : bridged;
        return problem is string p ? snapshot with { Source = $"{snapshot.Source} · {p}" } : snapshot;
    }

    public void Refresh() { lock (gate) nextPoll = DateTime.UtcNow.AddSeconds(3); }

    // Claude Code caches its own usage reading in .claude.json; reusing it avoids the rate-limited endpoint.
    private static UsageSnapshot? ReadClaudeCache()
    {
        var path = AppSettings.Current.ClaudeStateFile;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var config = JsonDocument.Parse(stream);
            if (!config.RootElement.TryGetProperty("cachedUsageUtilization", out var cache) || cache.ValueKind != JsonValueKind.Object
                || !cache.TryGetProperty("utilization", out var body)
                || !cache.TryGetProperty("fetchedAtMs", out var fetched) || fetched.ValueKind != JsonValueKind.Number) return null;
            return Parse(body, DateTimeOffset.FromUnixTimeMilliseconds(fetched.GetInt64())) is UsageSnapshot s ? s with { Source = "Claude Code cache" } : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }

    private async Task Poll()
    {
        var cached = ReadClaudeCache();
        if (cached is not null && !(latest?.UpdatedAt >= cached.UpdatedAt)) latest = cached;
        if (cached?.UpdatedAt > DateTimeOffset.UtcNow - PollEvery) { problem = null; lock (gate) polling = false; return; }
        if (!enabled) { problem = latest is null ? "live usage is off, right-click Claude to turn it on" : null; lock (gate) polling = false; return; }
        try
        {
            using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(CredentialsPath));
            if (!credentials.RootElement.TryGetProperty("claudeAiOauth", out var login)
                || !login.TryGetProperty("accessToken", out var token) || token.ValueKind != JsonValueKind.String)
            { problem = "not signed in to Claude Code"; return; }
            // Only Claude Code refreshes its login; until it does, keep the last reading.
            if (login.TryGetProperty("expiresAt", out var expires) && expires.ValueKind == JsonValueKind.Number
                && DateTimeOffset.FromUnixTimeMilliseconds(expires.GetInt64()) <= DateTimeOffset.UtcNow)
            { problem = "login expired, open Claude Code to renew"; return; }

            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.anthropic.com/api/oauth/usage");
            request.Headers.Authorization = new("Bearer", token.GetString());
            request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
            using var response = await Http.SendAsync(request);
            if ((int)response.StatusCode == 429) { lock (gate) nextPoll = DateTime.UtcNow.AddMinutes(10); problem = "rate limited, retrying later"; return; }
            if (!response.IsSuccessStatusCode) { problem = $"usage check failed ({(int)response.StatusCode})"; return; }
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (Parse(body.RootElement, DateTimeOffset.UtcNow) is UsageSnapshot snapshot) { latest = snapshot; problem = null; }
            else problem = "no plan limits reported";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException
            or System.Net.Http.HttpRequestException or TaskCanceledException)
        { problem = e is JsonException ? "unexpected response" : "offline"; }
        finally { lock (gate) polling = false; }
    }

    internal static UsageSnapshot? Parse(JsonElement body, DateTimeOffset at)
    {
        static Quota? Window(JsonElement body, string name)
        {
            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty(name, out var w) || w.ValueKind != JsonValueKind.Object) return null;
            if (!w.TryGetProperty("utilization", out var used) || used.ValueKind != JsonValueKind.Number) return null;
            DateTimeOffset? resets = w.TryGetProperty("resets_at", out var r) && r.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(r.GetString(), out var parsed) ? parsed : null;
            return new(Math.Clamp(100 - used.GetDouble(), 0, 100), resets);
        }
        var weekly = Window(body, "seven_day");
        var session = Window(body, "five_hour");
        return weekly is null && session is null ? null : new(weekly, session, at, "Claude account (live)");
    }
}

// Codex records the ChatGPT plan's rate limits in its local session logs after every response.
// Reading them needs no credentials or network; values are as fresh as Codex's last reply.
public sealed class CodexSessionProvider : IUsageProvider
{
    private readonly string root;
    private readonly IUsageProvider fallback = new JsonUsageProvider();
    private DateTime scannedAt;
    private (string Path, DateTime Written, long Length)? newest;
    private UsageSnapshot? latest;

    public CodexSessionProvider(string? root = null) => this.root = root ?? Path.Combine(AppSettings.Current.CodexDirectory, "sessions");

    public UsageSnapshot Read(string characterId)
    {
        try
        {
            // The widget polls every second; only rescan the log tree every few seconds.
            if (DateTime.UtcNow - scannedAt > TimeSpan.FromSeconds(5))
            {
                scannedAt = DateTime.UtcNow;
                var files = Directory.Exists(root)
                    ? new DirectoryInfo(root).EnumerateFiles("*.jsonl", SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTimeUtc).Take(5).ToList()
                    : new List<FileInfo>();
                var head = files.Count == 0 ? ((string, DateTime, long)?)null : (files[0].FullName, files[0].LastWriteTimeUtc, files[0].Length);
                if (head != newest)
                {
                    newest = head;
                    // A just-started session has no limits yet, so fall back to older logs.
                    foreach (var file in files)
                        if (Parse(file.FullName) is UsageSnapshot found) { latest = found; break; }
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return latest ?? fallback.Read(characterId);
    }

    internal static UsageSnapshot? Parse(string path)
    {
        const int tail = 512 * 1024;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        stream.Seek(Math.Max(0, stream.Length - tail), SeekOrigin.Begin);
        var lines = new StreamReader(stream).ReadToEnd().Split('\n');
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            if (!lines[i].Contains("\"rate_limits\":{")) continue;
            try
            {
                using var json = JsonDocument.Parse(lines[i]);
                var entry = json.RootElement;
                if (!entry.TryGetProperty("payload", out var payload) || !payload.TryGetProperty("rate_limits", out var limits) || limits.ValueKind != JsonValueKind.Object) continue;
                // Other buckets (e.g. "premium") cover specific models, not the plan's general allowance.
                if (limits.TryGetProperty("limit_id", out var id) && id.ValueKind == JsonValueKind.String && id.GetString() != "codex") continue;
                Quota? weekly = null, session = null;
                foreach (var name in new[] { "primary", "secondary" })
                {
                    if (!limits.TryGetProperty(name, out var window) || window.ValueKind != JsonValueKind.Object) continue;
                    if (!window.TryGetProperty("used_percent", out var used) || used.ValueKind != JsonValueKind.Number) continue;
                    DateTimeOffset? resets = window.TryGetProperty("resets_at", out var r) && r.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(r.GetInt64()) : null;
                    var quota = new Quota(Math.Clamp(100 - used.GetDouble(), 0, 100), resets);
                    // Classify by returned duration, not by field name.
                    var minutes = window.TryGetProperty("window_minutes", out var m) && m.ValueKind == JsonValueKind.Number ? m.GetDouble() : 0;
                    if (minutes >= 24 * 60) weekly = quota; else if (minutes > 0) session = quota;
                }
                if (weekly is null && session is null) continue;
                DateTimeOffset? at = entry.TryGetProperty("timestamp", out var t) && t.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(t.GetString(), out var parsed) ? parsed : null;
                return new(weekly, session, at, "Codex session log");
            }
            catch (JsonException) { } // the first line of the tail may be cut off
        }
        return null;
    }
}

public static class UsageDisplay
{
    public static bool IsStale(UsageSnapshot s, DateTimeOffset now) =>
        s.UpdatedAt is null || now - s.UpdatedAt > TimeSpan.FromMinutes(5) || s.UpdatedAt > now.AddMinutes(1);

    // Live sources only report while their tool is in use, so say how old the data is.
    public static string Freshness(UsageSnapshot s, DateTimeOffset now)
    {
        if (!IsStale(s, now)) return "";
        if (s.UpdatedAt is not DateTimeOffset at || at > now.AddMinutes(1)) return " · stale or missing timestamp";
        var age = now - at;
        return " · as of " + (age.TotalHours >= 1 ? $"{(int)age.TotalHours}h {age.Minutes:00}m" : $"{(int)age.TotalMinutes}m") + " ago";
    }

    // A window whose reset has passed has no usage in it until the next request starts a new one.
    public static double? Remaining(Quota? q, DateTimeOffset now) => q?.ResetsAt <= now ? 100 : q?.RemainingPercent;

    // The label inside a bar. Weekly: "5d" (nearest day) from 3 days out, then "2d 4h", "9h", and "4h 12m"
    // under five hours; session: "2h 13m".
    public static string BarLabel(DateTimeOffset? reset, DateTimeOffset now, bool weekly)
    {
        if (reset is null || reset <= now) return "";
        var left = reset.Value - now;
        if (weekly && left.TotalDays >= 3) return $"{Math.Round(left.TotalDays, MidpointRounding.AwayFromZero)}d";
        var parts = new List<string>();
        if (weekly && left.TotalDays >= 1)
        {
            parts.Add($"{(int)left.TotalDays}d");
            if (left.Hours > 0) parts.Add($"{left.Hours}h");
        }
        else
        {
            if ((int)left.TotalHours > 0) parts.Add($"{(int)left.TotalHours}h");
            if ((!weekly || left.TotalHours < 5) && left.Minutes > 0) parts.Add($"{left.Minutes}m");
        }
        return parts.Count == 0 ? "<1m" : string.Join(" ", parts);
    }

    public static string Countdown(DateTimeOffset? reset, DateTimeOffset now)
    {
        if (reset is null) return "reset --";
        var remaining = reset.Value - now;
        if (remaining <= TimeSpan.Zero) return "awaiting refresh";
        if (remaining.TotalDays >= 1) return $"{(int)remaining.TotalDays}d {remaining.Hours:00}h";
        return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }
}
