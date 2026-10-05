# Changelog

All notable changes to Taskbar Companions. Versions follow [Semantic Versioning](https://semver.org/), and this file follows [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added

- Clawd shows the weekly Fable allowance, for Claude plans with a separate weekly limit for the Fable model: a thin violet strip along the bottom of the HP bar, with its percentage and reset time in the bar's hover text. It comes from Claude Code's saved usage reading or live account usage, and is hidden when neither reports one. `claude.usage.json` accepts an optional `fable` entry.
- Settings says how old Claude Code's saved usage reading is.

### Fixed

- Clawd no longer shows full bars from an out-of-date reading. Claude Code saves a usage reading only when it checks usage itself, so the reading could predate a reset by hours or days while Claude Code was in use, and the bars showed as freshly reset. Such a window is now left empty, and the hover text says to open `/usage` in Claude Code to refresh it.

## [1.0.0] - 2026-09-28

The first packaged release: a normal Windows installer and a portable zip, with nothing else to install.

### Added

- An installer for your Windows account only, without administrator rights. It adds a Start menu entry, an optional desktop shortcut, an optional start at sign-in, and an uninstaller in Windows Settings.
- A portable zip with the same files.
- Both are built by GitHub Actions from this repository, with SHA-256 checksums and build provenance you can verify.
- An **About** window, in the tray menu and each companion's right-click menu. It shows the version, license, privacy summary and trademark notice, with links to the install guide, the privacy policy and your settings folder.
- **Copy status line setting** in Settings, which copies the line that turns on the status line bridge in Claude Code, with the right path filled in.
- The version, copyright and description in the exe's file properties.
- A privacy policy, security policy, install guide, contributing guide, code of conduct and third-party notices.

### Changed

- Settings and saved positions now live in `%LOCALAPPDATA%\TaskbarCompanions` instead of `app\data`, so the app works wherever it's installed. On first start they're copied over from `app\data`.
- The status line bridge writes to that folder too. Tools that publish `codex.usage.json` or `claude.usage.json` must write there now.
- Live account usage moved from Clawd's right-click menu to its own red area in Settings. Turning it on first explains what it does and the risk to your Claude account, which you must accept. If you had it on, turn it on again there.
- The app only starts a `codex.exe` found by its full path. Relative paths in `PATH`, `CODEX_PATH` or Settings are ignored.
- Live account usage never follows redirects, so the login token can only go to `api.anthropic.com`.
- The exe declares that it runs without administrator rights, on Windows 10 and 11.

### Fixed

- **Cancel** in Settings now closes the window.

## Before 1.0.0

Source-only releases on 2026-09-22 and 2026-09-23: the companions, the usage sources and the Settings window. There were no downloads; the app was built with `launch.ps1`.
