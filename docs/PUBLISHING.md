# Publishing Taskbar Companions

For the maintainer: what's in place for publishing the app, the one-time GitHub setup, how to ship a release, and the legal points to keep in mind.

## What's in the repository

| Area | Where |
|---|---|
| License (MIT) and trademark notice | [LICENSE](../LICENSE), README's "License and trademarks" |
| Privacy policy | [PRIVACY.md](../PRIVACY.md), linked from the README, the About window, the installer and the release notes |
| Security policy | [SECURITY.md](../SECURITY.md) |
| Third-party notices | [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md); the bundled runtime's own license files ship in `licenses\` |
| Install guide for any level | [docs/INSTALL.md](INSTALL.md) |
| Contributing guide, code of conduct | [CONTRIBUTING.md](../CONTRIBUTING.md), [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) |
| Changelog | [CHANGELOG.md](../CHANGELOG.md) |
| Issue and pull request templates | `.github/ISSUE_TEMPLATE/`, `.github/PULL_REQUEST_TEMPLATE.md` |
| CI and release automation | `.github/workflows/ci.yml`, `.github/workflows/release.yml` |
| Updates for the workflows' actions | `.github/dependabot.yml` |
| Installer, portable zip, checksums | `packaging/` |
| Notices in the app | About window; the warning before live account usage turns on |

## One-time setup on GitHub

In the repository's **Settings**, unless noted otherwise:

1. **Advanced Security** (called Code security on some accounts):
   - Turn on **Private vulnerability reporting**. SECURITY.md sends reporters there.
   - Turn on **Dependabot alerts** and **Dependabot security updates**.
   - Turn on **Secret Protection** (secret scanning) with **push protection**; both are free for public repositories.
2. **Rules → Rulesets:**
   - For `main`: block force pushes and deletion, and require the **CI** check to pass before merging pull requests.
   - For tags matching `v*`: restrict creating, updating and deleting them to yourself, so nobody else can start a release.
3. **Actions → General → Workflow permissions:** choose **Read repository contents and packages permissions**. The release workflow asks for the extra permissions it needs.
4. On the repository's front page, click the gear next to **About**: add a description, the releases page as the website, and topics such as `windows`, `desktop-pet`, `claude-code`, `codex` and `wpf`.
5. On your GitHub account: turn on **two-factor authentication**. In **Settings → Emails**, tick **Keep my email addresses private** and **Block command line pushes that expose my email**.
6. On this PC, Git is set to commit as `i538513@fontysict.nl` (in `~/.gitconfig`), which would publish your student email in every commit to this public repository. The existing commits use your private GitHub address. To keep it that way here:

   ```powershell
   git config user.email 170767542+levi2111@users.noreply.github.com
   ```

## Releasing a version

1. Choose the version, `x.y.z`, following [Semantic Versioning](https://semver.org/): fixes raise `z`, new features raise `y`, and breaking changes raise `x`.
2. Set `<Version>` in `TaskbarCompanions/TaskbarCompanions.csproj`.
3. In CHANGELOG.md, add a `## [x.y.z] - YYYY-MM-DD` entry at the top, with the release date. The release notes are built from it. For 1.0.0, update the date on the existing entry.
4. Build and check locally: `powershell -ExecutionPolicy Bypass -File packaging\build.ps1`. Then try the results in `dist\`, ideally on a Windows account without your developer tools: install, run, open Settings and About, and uninstall. Try the portable zip too.
5. Commit and push to `main`, and wait for **CI** to pass.
6. Optionally, do a dry run on GitHub: **Actions → Release → Run workflow**. It builds the same files and attaches them to the run, without creating a release.
7. Tag the version and push the tag:

   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

   The **Release** workflow checks that the tag matches `<Version>`, builds the files, runs the logic checks on the exact exe that ships, records build provenance, and creates a **draft** release with the files and notes.
8. Open **Releases**, review the draft (notes, files, SHA256SUMS.txt), and click **Publish release**.
9. Optionally, [submit it to winget](#winget).

## Code signing

Unsigned apps trigger "Windows protected your PC", and Smart App Control blocks them outright. Signing gives the app a verified publisher and builds SmartScreen reputation faster. Options:

- **[SignPath Foundation](https://signpath.org/):** free code signing for open-source projects, after an application. The certificate is SignPath Foundation's, and signing runs from GitHub Actions, which fits the release workflow.
- **[Azure Artifact Signing](https://learn.microsoft.com/azure/artifact-signing/)** (formerly Trusted Signing): Microsoft's pay-monthly signing service. Check whether it's available to individuals in your country.
- **An OV code signing certificate** from a certificate authority, typically a few hundred euros a year. Its private key has to live on a hardware token or in a cloud HSM.

Since 2024, EV certificates no longer skip SmartScreen's reputation building, so they aren't worth the extra cost here. Whichever option you choose, sign `TaskbarCompanions.exe` before packaging, and the installer after: Inno Setup's `SignTool` directive signs the installer and uninstaller.

If Microsoft Defender flags a release by mistake, [submit the file to Microsoft](https://www.microsoft.com/wdsi/filesubmission) as a false positive.

## Where to distribute

### GitHub Releases

The main channel. The README and the install guide point there.

### winget

Lets people install with `winget install levi2111.TaskbarCompanions`. After publishing a release:

1. Install the manifest tool: `winget install Microsoft.WingetCreate`.
2. Create the manifest from the installer's download URL:

   ```powershell
   wingetcreate new https://github.com/levi2111/taskbar-companions/releases/download/v1.0.0/TaskbarCompanions-Setup-1.0.0-x64.exe
   ```

   Use `levi2111.TaskbarCompanions` as the package identifier, **inno** as the installer type and **user** as the scope, and fill in the MIT license, the privacy policy URL and a short description. Let it submit the pull request to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs); it asks for a GitHub token.
3. For later versions: `wingetcreate update levi2111.TaskbarCompanions --version x.y.z --urls <installer URL> --submit`.

winget's automated review scans the installer. Unsigned installers are accepted.

### Microsoft Store: not for now

The Store doesn't accept apps that use others' intellectual property without permission. Clawd, the Claude and Codex names inside the app, and loading OpenAI's Codex pet artwork would likely fail certification without written permission from Anthropic and OpenAI.

## Legal checklist

- **License.** MIT, with "levi2111" as the copyright holder; a pseudonym is fine. Contributions come in under the same license, as CONTRIBUTING.md says.
- **Anthropic's trademarks and Clawd.** Anthropic's [trademark guidelines](https://www.anthropic.com/legal/trademark-guidelines) allow its marks only as it specifically permits, and forbid alterations. A redrawn Clawd is an altered version of Anthropic's character, so it's this project's main legal exposure; mentioning Claude and Claude Code to say what the app works with, with the non-affiliation notice, is much lower risk. The options:
  1. Ask for permission: email marketing@anthropic.com, describing the free fan app, the redrawn Clawd and the disclaimers, and keep the reply.
  2. Replace Clawd with an original character by default, the way the Codex companion falls back to the terminal explorer.
  3. Keep it as is, knowing Anthropic may ask for changes, and act on such a request promptly.

  Whichever you choose, keep Anthropic's and OpenAI's logos out of the app icon, screenshots and listings; keep "Claude" and "Codex" out of the product's name; and keep the non-affiliation notice everywhere it is now.
- **OpenAI's artwork and names.** The Codex pet artwork is never distributed; the app reads it from the user's own extension. Keep it out of screenshots and listings too. OpenAI's [brand guidelines](https://openai.com/brand/) cover its names.
- **Live account usage.** It conflicts with Anthropic's [policy on credential use](https://code.claude.com/docs/en/legal-and-compliance) for third-party apps. It stays off by default, in its red area of Settings, behind a warning users must accept, and the docs disclose it. If Anthropic objects, or the endpoint changes, remove it: it lives in `ClaudeUsageProvider.Poll`, `AppSettings.ClaudeLiveUsage` and the red area in `SettingsWindow`.
- **Codex App Server.** Using OpenAI's official App Server protocol with the user's own Codex login is what it's for.
- **Privacy.** You receive no personal data, so PRIVACY.md is all that's needed. If a future feature would send anything to you, such as crash reports, update checks or analytics, update PRIVACY.md and ask for consent in the app before shipping it.
- **Third-party licenses.** The downloads bundle the .NET runtime (MIT). `packaging\build.ps1` copies its license files into `licenses\` automatically; update THIRD-PARTY-NOTICES.md if you add anything else.
- **No warranty.** The MIT License's disclaimer covers the software, and the About window repeats it.
- **Contact for rights holders.** The README invites Anthropic and OpenAI to open an issue. A dedicated contact email in the README and CODE_OF_CONDUCT.md would give them, and people reporting conduct problems, a private channel.
- **Export control.** The app has no encryption of its own (it only uses Windows' HTTPS when live usage is on) and is published openly, so export rules for encryption software generally don't apply. Mention that if a store asks.
