using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskbarCompanions;

// Which companions appear, and where Claude Code and Codex keep their data. Stored in app\data\settings.json.
// Empty paths mean the usual places, or the CLAUDE_CONFIG_DIR, CODEX_HOME and CODEX_PATH variables when set.
public sealed record AppSettings
{
    public bool? ShowClaude { get; init; }   // null until chosen: shown when Claude Code is found
    public bool? ShowCodex { get; init; }
    public string? ClaudeFolder { get; init; }
    public string? CodexFolder { get; init; }
    public string? CodexExe { get; init; }

    public static AppSettings Current { get; private set; } = Load();
    private static string FilePath => Path.Combine(JsonUsageProvider.DataDirectory, "settings.json");
    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static AppSettings Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new() : new(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public void Save()
    {
        Current = this;
        try { Directory.CreateDirectory(JsonUsageProvider.DataDirectory); File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    public bool Shows(string id)
    {
        if ((id == "claude" ? ShowClaude : ShowCodex) is bool chosen) return chosen;
        // Not chosen yet: bring the companions whose tools are installed, or both when neither is found.
        return (id == "claude" ? ClaudeFound : CodexFound) || !(ClaudeFound || CodexFound);
    }

    private string? ChosenClaudeFolder => Blank(ClaudeFolder) ?? Blank(Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR"));

    // Holds Claude Code's login (.credentials.json) and settings.
    [JsonIgnore] public string ClaudeDirectory => ChosenClaudeFolder ?? Path.Combine(Home, ".claude");

    // Claude Code's state file, where it caches its usage reading. By default it sits beside the folder, not in it.
    [JsonIgnore] public string ClaudeStateFile => Path.Combine(ChosenClaudeFolder ?? Home, ".claude.json");

    [JsonIgnore] public bool ClaudeFound => Directory.Exists(ClaudeDirectory) || File.Exists(ClaudeStateFile);

    [JsonIgnore] public string CodexDirectory => Blank(CodexFolder) ?? Blank(Environment.GetEnvironmentVariable("CODEX_HOME")) ?? Path.Combine(Home, ".codex");

    [JsonIgnore] public string? CodexExecutable => CodexAppServerProvider.FindCodex(Blank(CodexExe));

    [JsonIgnore] public bool CodexFound => Directory.Exists(CodexDirectory) || CodexExecutable is not null;

    internal static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('"');
}
