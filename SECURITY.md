# Security policy

## Supported versions

Security fixes go into the latest release. Please check that a problem still happens there before reporting it.

## Reporting a vulnerability

Please report security problems privately, not in a public issue:

1. Open the repository's **Security** tab.
2. Choose **Report a vulnerability**, or go straight to https://github.com/levi2111/taskbar-companions/security/advisories/new.

Include what you found, the version (from **About**), how to reproduce it, and what an attacker could do with it. Never include real tokens, the contents of `.credentials.json` or other secrets; a redacted example is enough.

This is a one-person hobby project, so responses are best effort. I aim to acknowledge reports within a week, and to fix confirmed problems before discussing them publicly. If you'd like, you'll be credited in the release notes.

Problems in Claude Code or Codex themselves go to Anthropic, under its [responsible disclosure policy](https://www.anthropic.com/responsible-disclosure-policy), or to OpenAI, under its [coordinated vulnerability disclosure policy](https://openai.com/policies/coordinated-vulnerability-disclosure-policy/).

## How the app limits what it can do

- It runs with your normal Windows rights and never asks for administrator access.
- It has no telemetry and no automatic updates, and it makes no internet connections of its own unless you turn on live account usage. Even then, its only request is an HTTPS request to `api.anthropic.com` that doesn't follow redirects. See [PRIVACY.md](PRIVACY.md).
- It never saves, logs or refreshes credentials.
- The only program it starts is `codex.exe`, and only by a full path: from Settings, `CODEX_PATH`, the Codex extension or `PATH`. Relative paths are ignored, so a `codex.exe` planted in the current folder can't be picked up.
- It keeps its settings in your own `%LOCALAPPDATA%` folder, which other Windows users can't change.
- It uses no third-party packages. Release builds add only Microsoft's .NET runtime packs, from nuget.org.
- Release files are built by GitHub Actions from tagged source, with published SHA-256 checksums and build provenance attestations.

## Verifying a download

Compare a file's SHA-256 checksum with the release's `SHA256SUMS.txt`:

```powershell
Get-FileHash .\TaskbarCompanions-Setup-1.0.0-x64.exe
```

With the [GitHub CLI](https://cli.github.com/), confirm that it was built by this repository's release workflow:

```powershell
gh attestation verify .\TaskbarCompanions-Setup-1.0.0-x64.exe --repo levi2111/taskbar-companions
```
