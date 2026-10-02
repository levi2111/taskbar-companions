# Contributing

Thanks for helping! Bug reports, ideas and pull requests are all welcome.

## Before you start

- **Bugs:** [open an issue](https://github.com/levi2111/taskbar-companions/issues/new/choose) with the version (from **About**), your Windows version, and what you saw. Never paste tokens, or the contents of `.credentials.json` or `.claude.json`.
- **Bigger changes:** open an issue first, so we can agree on the approach before you spend time on it.
- **Security problems:** report them privately instead, as [SECURITY.md](SECURITY.md) explains.

## Building and checking

You need Windows 10 or 11 and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet build TaskbarCompanions/TaskbarCompanions.csproj --configfile NuGet.Config
```

Then run the built exe with `--self-test`, and with `--smoke-test` for changes to windows or startup; the [README](README.md#build-and-checks) explains both. If you change the status line bridge, also run `node --check bridge/claude-statusline.js`.

To build the installer and portable zip the way a release does, run `powershell -ExecutionPolicy Bypass -File packaging\build.ps1`. The installer needs Inno Setup 6.

## Ground rules

These keep the app safe to publish and to trust:

- **Privacy.** No telemetry, analytics, update checks or new network connections. A change that would send anything anywhere needs an issue first, and an update to [PRIVACY.md](PRIVACY.md).
- **Credentials.** Never log, save, refresh or send anyone's tokens, and don't add ways to sign in to Claude or Codex or to reuse their logins. Live account usage is the only exception: it stays off by default, behind its warning.
- **No third-party packages.** `NuGet.Config` clears all package sources on purpose.
- **Artwork and trademarks.** Don't add Anthropic's or OpenAI's logos or artwork files; the Codex pet is loaded from the user's own install and never included. New characters should be original and drawn in code.
- **No administrator rights.** The app must keep working without them.
- **Style.** Match the code around your change, and keep comments about why, not what.
- **Checks and docs.** Add a `--self-test` check for new logic. When behavior changes, update the [README](README.md), the [install guide](docs/INSTALL.md) and the [changelog](CHANGELOG.md).

## Licensing of contributions

By contributing, you agree that your contribution is licensed under this project's [MIT License](LICENSE), and that you have the right to license it that way: it's your own work, or it comes from a source whose license allows that.

## Code of conduct

Everyone taking part is expected to follow the [code of conduct](CODE_OF_CONDUCT.md).
