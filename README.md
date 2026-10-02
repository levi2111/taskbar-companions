# Taskbar Companions

Two little desktop companions for Windows that sit on your taskbar and show how much of your **Codex** and **Claude** plan allowance is left, as RPG-style HP and MP bars. One process, one independent transparent window per companion. Use either one on its own, or both.

- **Codex** is the default blue Codex pet when the Codex extension is installed. Otherwise the little terminal explorer, drawn in this project, comes along in his place.
- **Claude** is Clawd, the pixel critter from Claude Code's terminal banner, redrawn in code.

![Clawd's poses on light and dark backgrounds](docs/clawd-poses.png)

> **Unofficial fan project.** Not affiliated with, endorsed by, or sponsored by Anthropic or OpenAI. Claude, Claude Code and Clawd are trademarks of Anthropic. Codex and ChatGPT are trademarks of OpenAI. See [License and trademarks](#license-and-trademarks).

## Download and install

1. Open the [latest release](https://github.com/levi2111/taskbar-companions/releases/latest).
2. Under **Assets**, download **`TaskbarCompanions-Setup-<version>-x64.exe`** and open it.
3. If Windows says "Windows protected your PC", choose **More info**, then **Run anyway**. The app isn't code-signed yet, so Windows doesn't recognize it.
4. Follow the installer. It installs for your Windows account only and doesn't need administrator rights.

The download includes everything the app needs, .NET included. If you'd rather not install anything, there's a portable zip on the same page.

**[The install guide](docs/INSTALL.md)** walks through every step, plus updating, uninstalling, starting with Windows, checking your download and fixing common problems.

## Requirements

- Windows 10 (version 1809 or later) or Windows 11, 64-bit. Tested on Windows 11.
- For Claude's bars: Claude Code, signed in with a Pro or Max plan.
- For Codex's bars and the Codex pet: the Codex extension for VS Code, VS Code Insiders or Cursor, signed in.

You only need the tool for the companion you want. On first start the app shows the companions whose tools it finds (both, if it finds neither); **Settings** changes that.

## Using the companions

- Right-click the running app's taskbar icon and choose **Pin to taskbar**. The characters share one taskbar button and one Alt+Tab entry.
- Right-click a character and choose **Minimize companions**. The shared taskbar button minimizes and restores them together; launching again restores them without moving them.
- The tray menu also offers **Restore companions**, **Bring companions home**, **Demo usage on / off**, **Pause / resume companions**, **Settings…**, **About Taskbar Companions**, and **Quit**. Closing a character minimizes all of them.
- **Settings…** (in the tray menu and each character's right-click menu) shows or hides each companion, and says what it found for each: Claude Code's folder, `codex.exe`, Codex's session logs, and the Codex pet's artwork. If Claude Code or Codex keeps its data somewhere unusual, pick the folder (or `codex.exe`) there. **Hide this companion** in a character's right-click menu hides it straight away; the last one showing can't be hidden.
- **About…** shows the version, license, privacy summary and trademark notice, with links to this page and to your settings folder.
- Drag a character or its bars to move it. **Sit on taskbar** toggles docking; **Return home** resets placement.
- Each character has two floating tracks: green HP for weekly allowance remaining, blue MP for the short (five-hour) window. Inside each bar is the time until that window resets. HP shows whole days ("5d") until three days remain, then "2d 4h", "9h", and "4h 12m" under five hours. MP shows "2h 13m". Hover a bar for details and where the data came from; hover a character for one of its lines.
- When a window resets, its bar drains and pours back full with a shine, and the character celebrates. **Preview reset celebration** in the right-click menu plays it on demand.
- Unknown usage leaves the bars empty. **Demo usage on / off** shows sample values, and the hover details say so.
- Fullscreen apps temporarily hide the companions without minimizing them.

Settings, saved positions and bridge files live in `%LOCALAPPDATA%\TaskbarCompanions`.

## The characters

### Codex

**The Codex pet.** Its artwork belongs to OpenAI and is **not included in this repository**. At startup the app looks for `codex-spritesheet*.webp` inside the Codex extension you have installed (`~\.vscode\extensions\openai.chatgpt-*\webview\assets\`, and the same under `.vscode-insiders` and `.cursor`) and reads it from there, using Windows' built-in WebP support. If the sheet is missing, can't be decoded, or has an unexpected layout, the terminal explorer comes instead.

He breathes gently with his feet planted, blinks every eight seconds, and occasionally pauses in a thoughtful pose. Hovering gives a smile and a slight lean toward the pointer. Clicking gives a little nod, a short wave, then a smile. While dragged he holds a scrunched expression and sways gently; releasing him gives a soft squash and recovery. Every 90 seconds he spends 24 seconds with his laptop. Low energy selects a resting expression. An allowance reset earns a jump, a happy bounce and a wave, with cyan sparkles.

**The little terminal explorer.** An ivory pixel explorer with a mint terminal satchel and tiny boots, drawn entirely in code (`TerminalExplorer.cs`). He waves when hovered, hops with sparkles when poked, types at a pretend terminal every so often, and naps when energy runs out.

![The terminal explorer's poses](docs/terminal-explorer-poses.png)

These are local animations, not indicators of what Codex is actually doing.

### Claude: Clawd

Rebuilt in `Clawd.cs` from Claude Code's block-character art. Each terminal quadrant becomes a 6×12 pixel block, rendered without anti-aliasing; expressions use a finer 3-pixel grid.

- **Watches the pointer.** Its eyes follow the mouse while it moves, then wander.
- **Has a routine.** Every 24 seconds it pauses to think, looking up at a pixel version of Claude Code's `· ✢ ✳ ✶ ✻` spinner, then hops at a gold "aha" star. On alternate rounds it holds up an open book and reads.
- **Responds to you.** Hover it and it smiles, blushes and waves. Click without dragging to poke it: it crouches, hops and gives off gold sparkles. Drag it and it flails and kicks, then crouches on landing.
- **Is honest about energy.** Mood follows the lower of HP and MP. At 20% or less it gets drowsy, with heavy lids, drooping arms, and the odd yawn. At 0% it sits down to sleep with floating Zs, and a poke only startles it awake for a moment.
- **Celebrates resets.** It springs awake, a striped party hat drops onto its head, and it hops under a gold star, then waves while sparkles fall.

## Where usage comes from

Nothing here sends a message or spends allowance. If a source has gone quiet, the bar tooltip says how old the data is, for example "as of 1h 20m ago".

### Codex (ChatGPT plan)

- **Codex App Server (live).** The app starts the official [App Server](https://learn.chatgpt.com/docs/app-server) (`codex.exe app-server`: the one chosen in Settings, or from `CODEX_PATH`, the Codex extension, or `PATH`, always by its full path) hidden in the background. It asks `account/rateLimits/read` once a minute and three seconds after a reset. This is the same read-only call the Codex app uses. The App Server signs in with Codex's own login and talks to OpenAI itself; this app never handles your Codex credentials.
- **Codex session logs (local).** Between polls, and whenever the App Server is unavailable, the app reads the `rate_limits` Codex writes after each reply under `%USERPROFILE%\.codex\sessions` (or the Codex folder chosen in Settings, or `CODEX_HOME`), whichever is newer.
- Both use the `codex` bucket and classify windows by duration: under a day is MP, a day or more is HP. With neither available, the app falls back to `codex.usage.json` in its data folder (see [Other sources](#other-sources)).

### Claude (Pro/Max plan)

By default, Claude usage comes only from local files:

- **Claude Code's own cache.** Claude Code stores its latest usage reading in `~\.claude.json` (or in the Claude Code folder chosen in Settings, or under `$CLAUDE_CONFIG_DIR`). The app reads only that entry, so it's as fresh as Claude Code's last check.
- **Status line bridge.** `bridge/claude-statusline.js` is a Claude Code [status line](https://code.claude.com/docs/en/statusline) command, and it ships beside the app. Claude Code passes it `rate_limits.five_hour` and `rate_limits.seven_day` after the first response of a session. It prints `Opus · 5h 24% · 7d 41%` in Claude Code and atomically writes `claude.usage.json` in the app's data folder. It needs [Node.js](https://nodejs.org/). To turn it on, open **Settings**, click **Copy status line setting**, and paste the line into `~\.claude\settings.json` ([step by step](docs/INSTALL.md#optional-the-status-line-bridge)). It looks like this, with the path to your copy of the app:

  ```json
  "statusLine": { "type": "command", "command": "node \"C:/Users/you/AppData/Local/Programs/Taskbar Companions/bridge/claude-statusline.js\"" }
  ```

  It only updates while a terminal Claude Code session is responding; the VS Code extension does not run status line commands. Usage on claude.ai counts toward the same limits but only shows up the next time Claude Code responds.

**Live account usage (risky, off by default).** A separate red area in **Settings** can turn on polling, every two minutes, of the endpoint behind Claude Code's `/usage` screen (`GET https://api.anthropic.com/api/oauth/usage`). For this, the app reads the login token Claude Code stores in `~\.claude\.credentials.json`. The token is sent only to `api.anthropic.com` (redirects aren't followed) and is never logged, written, or refreshed. Before it turns on, a warning explains the risks, and you have to accept them:

- **Anthropic's terms don't allow it.** Claude Code's [legal and compliance page](https://code.claude.com/docs/en/legal-and-compliance) says developers "may not collect, store, or intermediate Claude.ai credentials or session tokens", and Anthropic may enforce this without notice. Your Claude account could be restricted or suspended.
- The endpoint is **undocumented** and may change or stop working at any time.
- Security software may flag an app that reads another app's login file.
- If the stored login has expired because Claude Code hasn't run for a while, the tooltip says so; open Claude Code to renew it.

The choice is saved in the app's `settings.json`.

### Other sources

Any tool can publish usage by writing JSON files into the app's data folder, `%LOCALAPPDATA%\TaskbarCompanions`, named `codex.usage.json` and `claude.usage.json`:

```json
{
  "weekly": { "remainingPercent": 76, "resetsAt": "2026-09-25T18:00:00Z" },
  "session": { "remainingPercent": 48, "resetsAt": "2026-09-22T18:00:00Z" },
  "updatedAt": "2026-09-22T15:00:00Z"
}
```

Percentages range from 0 to 100; null means unknown. Use ISO-8601 timestamps with time zones, and write with an atomic file replacement to avoid partial reads. The app polls every second and notes the data's age after five minutes. Once a window's `resetsAt` passes, it shows that window as full, because a reset clears usage until the next request starts a new window. `session` means the provider's short quota window, not a chat session. There's a sample in `examples/`. You can also implement `IUsageProvider` in code.

## Privacy and network access

- **Reads locally:** Codex session logs, the usage entry in Claude Code's `.claude.json`, and the usage files in its data folder. With live account usage on, also Claude Code's login token.
- **Starts:** `codex.exe app-server`, which talks to OpenAI with Codex's own login.
- **Connects to the internet itself:** only when you turn on live account usage, and then only to `api.anthropic.com`.
- **Writes:** only its data folder, `%LOCALAPPDATA%\TaskbarCompanions` (settings and positions), plus `checks.txt` beside the exe and short-lived temp files during `--self-test`.

No telemetry, no update checks, no other network access, and no NuGet packages (`NuGet.Config` clears all package sources). The [privacy policy](PRIVACY.md) has the details.

## Build from source

For developers. You need Git and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/levi2111/taskbar-companions.git
cd taskbar-companions
powershell -ExecutionPolicy Bypass -File .\launch.ps1
```

`launch.ps1` builds the app into `app\` the first time, then starts it. Run it with `-Rebuild` after pulling changes. You can also start `app\TaskbarCompanions.exe` directly; keep the `app` folder in place after pinning. The exe you build yourself is unsigned too, so Smart App Control or SmartScreen may block it.

## Code tour

- `Characters.cs`: character definitions (names, colors, dialogue, drawing callback) and the `CharacterFrame` each drawing receives: time, pointer gaze and attention, hover, poke, drag, reset and energy. A drawing may ignore any of it.
- `Clawd.cs`: Claude's appearance and behavior.
- `CodexCompanion.cs`: the Codex pet, and loading its artwork from the installed extension.
- `TerminalExplorer.cs`: the terminal explorer.
- `CharacterView.cs`: animation clock, pointer and hover tracking, dialogue tooltips.
- `Usage.cs`: usage providers, the JSON bridge and data folder, freshness and countdown formatting.
- `CompanionWindow.cs`: each companion's window, floating bars, menus and saved position.
- `Settings.cs`: which companions show, where Claude Code and Codex keep their data, and detecting them.
- `SettingsWindow.cs`: the Settings window, and the warning before live account usage turns on.
- `AboutWindow.cs`: version, license, privacy and trademark notices.
- `Desktop.cs`: monitor positioning and fullscreen detection.
- `App.xaml.cs`: process lifetime, single instance, tray menu, and opening a window for each companion shown.
- `packaging/`: the release build script, the installer script and the NuGet config for release builds.

## Build and checks

```powershell
dotnet build TaskbarCompanions/TaskbarCompanions.csproj --configfile NuGet.Config
```

- **Logic checks:** run the built exe with `--self-test`. It writes `checks.txt` beside the exe and exits with 0 on success, 1 on failure. It covers countdown boundaries, data freshness, malformed JSON, quota bounds, Codex log and App Server parsing, Claude usage parsing, bar labels, reset refills, fullscreen versus maximized geometry, the Codex artwork fallback and transparency, choosing companions and folders, copying settings from the old data folder, ignoring relative `codex.exe` paths, and live account usage being off by default.
- **Startup smoke check:** run with `--smoke-test`. It opens the companions the settings show alongside any running copy, checks the shared taskbar entry and minimize/restore, renders `codex.preview.png` and/or `claude.preview.png` mid reset celebration, writes `desktop-checks.txt`, and exits after about four seconds.
- **Release files:** `powershell -ExecutionPolicy Bypass -File packaging\build.ps1` builds the self-contained exe, runs the logic checks on it, and writes the installer (needs [Inno Setup 6](https://jrsoftware.org/isinfo.php)), the portable zip, `SHA256SUMS.txt` and release notes into `dist\`. Release builds take Microsoft's .NET runtime packs from nuget.org; `packaging/NuGet.Config` allows nothing else.
- **GitHub Actions** builds and checks every push and pull request, and builds the release files when a version is tagged. [docs/PUBLISHING.md](docs/PUBLISHING.md) describes the release process.

## Scope and known limits

Taskbar docking targets the bottom of the chosen monitor's work area (Windows 11's standard bottom taskbar). Fullscreen detection uses foreground-window geometry, not game hooks. Auto-hidden or side taskbars, mixed-DPI multi-monitor setups, games with unusual window bounds, and virtual desktops are largely untested. The app adds a startup entry only if you tick that option in the installer.

## Help, feedback and security

- Questions and bug reports: [issues](https://github.com/levi2111/taskbar-companions/issues). The [install guide's troubleshooting](docs/INSTALL.md#troubleshooting) covers common problems.
- Security problems: please report them privately, as [SECURITY.md](SECURITY.md) explains.
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md) and the [code of conduct](CODE_OF_CONDUCT.md).
- What changed in each version: [CHANGELOG.md](CHANGELOG.md).

## License and trademarks

The code is under the [MIT License](LICENSE), including the terminal explorer, which is original artwork drawn in code. It comes without warranty.

Clawd is Anthropic's character: `Clawd.cs` draws a fan redrawing of it, and the license doesn't cover Anthropic's rights in the character or in the names Claude, Claude Code and Clawd. The Codex pet artwork is OpenAI's; it isn't part of this repository or its downloads. The downloads include the .NET runtime under its own MIT license; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

This project isn't affiliated with, endorsed by, or sponsored by Anthropic or OpenAI. If you represent either and would like something changed or removed, please [open an issue](https://github.com/levi2111/taskbar-companions/issues) and it will be handled promptly.
