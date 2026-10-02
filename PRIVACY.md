# Privacy policy

*Effective 28 September 2026, for Taskbar Companions 1.0.0 and later.*

Taskbar Companions is a free Windows app made by an individual developer, levi2111 ("I" below). This policy explains what the app does with information on your PC.

**The short version: I don't collect anything.** The app has no accounts, ads, analytics, tracking or telemetry, and it never sends anything to me.

## What the app reads on your PC

To fill the bars, the app reads what Claude Code and Codex already keep on your PC. The locations below are the defaults; Settings can point the app somewhere else.

| What | Where | Used for |
|---|---|---|
| Claude Code's saved usage reading: the `cachedUsageUtilization` entry | `%USERPROFILE%\.claude.json` | Clawd's bars |
| Usage files written by the status line bridge or another tool | `%LOCALAPPDATA%\TaskbarCompanions\*.usage.json` | Both companions' bars |
| The `rate_limits` entries in Codex's session logs | `%USERPROFILE%\.codex\sessions` | The Codex companion's bars |
| The Codex pet's artwork | Your installed Codex extension for VS Code, VS Code Insiders or Cursor | Drawing the Codex pet |
| Claude Code's login token, **only if you turn on live account usage** | `%USERPROFILE%\.claude\.credentials.json` | Asking Anthropic for your current usage (see below) |

The app opens these files but uses only the entries listed. `.claude.json` and Codex's session logs also hold other things, such as your project list and your Codex conversations. The app reads only the end of the newest session logs to find the `rate_limits` entries, and it keeps and sends nothing else from these files. It also checks whether Claude Code's and Codex's folders exist, to decide which companions to show, and Settings shows what it found.

While it runs, the app also looks at:

- the pointer position, so the characters' eyes can follow it;
- the size of the window in front, to hide the companions while a fullscreen app or game is showing.

These stay in memory. They're never saved or sent anywhere.

## What the app saves

In `%LOCALAPPDATA%\TaskbarCompanions`:

- your settings: which companions show, any folders you chose, and whether live account usage is on;
- where each companion sits on screen.

If you set up the status line bridge, it also writes your latest Claude usage there. Nothing else is saved, and credentials never are.

## Internet connections

The app doesn't connect to the internet itself unless you turn on live account usage. Two things can connect:

1. **Codex's App Server.** When the Codex companion is showing and `codex.exe` is found, the app starts OpenAI's own Codex App Server in the background. About once a minute it asks the App Server for your plan's rate limits. The App Server contacts OpenAI with your Codex login; the app never sees your Codex credentials. OpenAI's [privacy policy](https://openai.com/policies/privacy-policy/) covers that connection. Hiding the Codex companion stops it.
2. **Live account usage, off by default.** Only if you turn it on in Settings and accept its warning, the app reads Claude Code's login token every two minutes and sends it to Anthropic at `api.anthropic.com` to ask for your current usage. The token goes nowhere else, and the app never saves, logs or refreshes it. Anthropic's [privacy policy](https://www.anthropic.com/legal/privacy) covers that connection. Anthropic's terms don't allow other apps to use Claude Code's login, which can put your Claude account at risk; the warning in Settings explains this before you can turn it on.

Links in the About and Settings windows open in your web browser only when you click them. The app doesn't check for updates.

## The status line bridge

The optional status line bridge, `bridge\claude-statusline.js`, runs inside Claude Code once you set it up. Claude Code passes it status information after each reply. The bridge writes your plan's usage percentages and reset times to `%LOCALAPPDATA%\TaskbarCompanions\claude.usage.json` and prints a short status line. It makes no network connections.

## Downloads

When you download the app from GitHub, GitHub's [privacy statement](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement) covers that visit. GitHub shows me totals such as download counts, not who downloaded.

## Your choices

- Hide either companion in Settings. The app then stops reading that companion's usage, and it doesn't start Codex's App Server while the Codex companion is hidden.
- Leave live account usage off, or turn it off again in Settings.
- Delete `%LOCALAPPDATA%\TaskbarCompanions` at any time to remove everything the app saved. Uninstalling with the installer removes it for you.

## Children

The app is a utility for people who use Claude Code or Codex, whose own terms set minimum ages. It collects nothing from anyone, including children.

## Your rights

I don't receive or process any of your personal data, so there's nothing for me to show, correct or delete. Anthropic's and OpenAI's privacy policies cover the data they hold about you, and requests about it go to them.

## Changes to this policy

A new version of this policy will be published in this repository with a new effective date, and listed in the [changelog](CHANGELOG.md). Any change that would send information to me would be announced before it ships, and the app would ask for your consent first.

## Contact

For questions about this policy, open an issue at https://github.com/levi2111/taskbar-companions/issues. Please don't post personal information there. To report a security problem privately, see [SECURITY.md](SECURITY.md).
