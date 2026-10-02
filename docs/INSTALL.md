# Installing Taskbar Companions

This guide takes you from download to two companions on your taskbar, one step at a time. You don't need any technical knowledge. Developers can skip to [Option C](#option-c-build-from-source-for-developers).

- [Before you start](#before-you-start)
- [Option A: the installer (recommended)](#option-a-the-installer-recommended)
- [Option B: the portable zip](#option-b-the-portable-zip)
- [Option C: build from source (for developers)](#option-c-build-from-source-for-developers)
- [After installing](#after-installing)
- [Optional: the status line bridge](#optional-the-status-line-bridge)
- [Start with Windows, pin to the taskbar](#start-with-windows-pin-to-the-taskbar)
- [Check that your download is genuine](#check-that-your-download-is-genuine)
- [Updating](#updating)
- [Uninstalling](#uninstalling)
- [Troubleshooting](#troubleshooting)

## Before you start

You need:

- **Windows 10 (version 1809 or later) or Windows 11, 64-bit.** Nearly every PC from the last several years qualifies. To check, open **Settings → System → About** and look at **System type**: it should say "64-bit operating system".
- For Clawd's bars: **Claude Code**, signed in with a Pro or Max plan.
- For the Codex companion's bars: the **Codex extension** for VS Code, VS Code Insiders or Cursor, signed in.

The companions still appear without Claude Code or Codex, but their bars stay empty. You don't need administrator rights, .NET or anything else: the download includes everything.

Good to know:

- The app is **free and open source**, with no ads, accounts or tracking. See the [privacy policy](../PRIVACY.md).
- It's an **unofficial fan project**, not made by Anthropic or OpenAI.
- The app **isn't code-signed yet**. Code signing is how Windows recognizes an app's publisher, so for now Windows calls it unrecognized and asks you to confirm before it runs. The steps below show exactly what you'll see. To be extra careful, you can [check your download](#check-that-your-download-is-genuine) first.

## Option A: the installer (recommended)

### 1. Download

1. Open the [latest release](https://github.com/levi2111/taskbar-companions/releases/latest).
2. Scroll down to **Assets**. If it's collapsed, click **Assets** to open it.
3. Click **TaskbarCompanions-Setup-*version*-x64.exe**. It's about 67 MB.

Your browser may warn you, because few people have downloaded this file so far:

- **Microsoft Edge** says the file "isn't commonly downloaded". Point at the download, click **…** (See more), choose **Keep**, then **Show more** and **Keep anyway**.
- **Google Chrome** may ask whether to keep the file. Choose **Keep**.

### 2. Install

1. Open the downloaded file: click it in your browser's download list, or double-click it in your **Downloads** folder.
2. If Windows shows a blue box saying **"Windows protected your PC"**, click **More info**, check that the app is **TaskbarCompanions-Setup-*version*-x64.exe**, and click **Run anyway**.
   If Windows says **Smart App Control** blocked the app instead, see [Troubleshooting](#windows-blocks-the-app-with-smart-app-control).
3. The installer opens:
   1. **License Agreement:** the MIT License. Choose **I accept the agreement** and click **Next**.
   2. **Information:** a short summary of what the app does and doesn't do. Click **Next**.
   3. **Select Destination Location:** keep the suggested folder and click **Next**.
   4. **Select Additional Tasks:** tick **Create a desktop shortcut** and **Start Taskbar Companions when I sign in to Windows** if you'd like them, then click **Next**.
   5. **Ready to Install:** click **Install**.
   6. **Completing the setup:** keep **Launch Taskbar Companions** ticked and click **Finish**.

The companions appear just above the right end of your taskbar. Continue with [After installing](#after-installing).

The installer puts the app in `%LOCALAPPDATA%\Programs\Taskbar Companions`, adds it to the Start menu, and adds an uninstaller to Windows Settings. It doesn't change anything else on your PC.

## Option B: the portable zip

Use this if you'd rather not install anything, for example to try the app first.

1. On the [latest release](https://github.com/levi2111/taskbar-companions/releases/latest), under **Assets**, download **TaskbarCompanions-*version*-win-x64-portable.zip** (about 67 MB). If your browser warns you, see [Download](#1-download) above.
2. Open your **Downloads** folder, right-click the zip, and choose **Extract All…**. Pick a folder you'll keep, such as `Documents\Taskbar Companions`, and click **Extract**.
   Don't run the app from inside the zip without extracting it: Windows deletes those temporary copies later.
3. In the extracted folder, double-click **TaskbarCompanions.exe**.
4. If Windows says **"Windows protected your PC"**, click **More info**, then **Run anyway**.

Keep the folder where it is, because shortcuts and pins point to it. Your settings are saved separately, in `%LOCALAPPDATA%\TaskbarCompanions`, so replacing the folder with a newer version keeps them.

## Option C: build from source (for developers)

You need Git and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/levi2111/taskbar-companions.git
cd taskbar-companions
powershell -ExecutionPolicy Bypass -File .\launch.ps1
```

`launch.ps1` builds the app into `app\` the first time, then starts it; run it with `-Rebuild` after pulling changes.

To build the same installer and zip as a release, run `powershell -ExecutionPolicy Bypass -File packaging\build.ps1`. The files land in `dist\`. The installer needs Inno Setup 6: `winget install JRSoftware.InnoSetup`.

## After installing

- The companions sit just above the right end of your taskbar. Hover a bar to see what it shows and where its numbers come from; hover a character to hear from it.
- **Right-click a character** for its menu: Settings, About, minimize, return home and more.
- The app also has an icon in the **notification area** near the clock. You may need to click the **^** arrow to see it. Right-click it for the same options, and for **Quit**.
- On first start the app shows the companions whose tools it finds. To change which appear, right-click a character and choose **Settings…**.

**Are the bars empty?** That's normal until Claude Code or Codex has been used on this PC, because the app reads the usage they save. Use them once, and the bars fill within a minute or two. **Settings…** shows what the app found for each companion.

At the bottom of Settings there's a red area, **Live account usage**. It's off by default, and we recommend leaving it off: it reuses Claude Code's login in a way Anthropic's terms don't allow, which can put your Claude account at risk. The app explains the details before it can be turned on.

## Optional: the status line bridge

This is for people who use Claude Code in a terminal. It makes Clawd's bars update after every Claude Code reply, instead of only when Claude Code refreshes its own saved reading. Skip it if you only use Claude Code in VS Code, because the VS Code extension doesn't run status lines.

It needs **Node.js**. If you don't have it, install the LTS version from [nodejs.org](https://nodejs.org/).

1. Right-click a companion, choose **Settings…**, and click **Copy status line setting**. This copies a line like this one, with the path to your copy of the app:

   ```json
   "statusLine": { "type": "command", "command": "node \"C:/Users/you/AppData/Local/Programs/Taskbar Companions/bridge/claude-statusline.js\"" }
   ```

2. Press **Windows + R**, type `notepad %USERPROFILE%\.claude\settings.json`, and press **Enter**. If Notepad offers to create a new file, choose **Yes**.
3. Add the copied line:
   - **If the file is empty,** type `{`, press **Enter**, paste the line, press **Enter**, and type `}`.
   - **If the file already has settings,** click just after the first `{`, press **Enter**, paste the line, and type a comma at its end, so it's separated from the setting below it.
   - **If there's already a `"statusLine"` line,** replace that line with yours, keeping any comma at its end. Claude Code shows one status line at a time, and the bridge prints its own, for example `Opus · 5h 24% · 7d 41%`.
4. Save with **Ctrl + S** and restart Claude Code.

After your next Claude Code reply, Clawd's bars update. If you move the app to another folder, copy the setting again.

## Start with Windows, pin to the taskbar

- **Start with Windows.** If you used the installer, run it again and tick **Start Taskbar Companions when I sign in to Windows**; your settings are kept. For the portable version, press **Windows + R**, type `shell:startup` and press **Enter**. In the folder that opens, right-click an empty space, choose **New → Shortcut**, click **Browse…**, pick **TaskbarCompanions.exe**, and click **Next**, then **Finish**.
  To stop it starting with Windows, turn it off in **Task Manager → Startup apps**, or delete that shortcut.
- **Pin to the taskbar.** While the app runs, right-click its taskbar button and choose **Pin to taskbar**. You can also find **Taskbar Companions** in the Start menu, right-click it, and choose **Pin to taskbar**.

## Check that your download is genuine

This is optional, for extra peace of mind. Each release lists a checksum, a kind of fingerprint, for every file in **SHA256SUMS.txt**.

1. Download **SHA256SUMS.txt** from the same release, and open it in Notepad.
2. Open **PowerShell** from the Start menu and run this, with the name of the file you downloaded:

   ```powershell
   Get-FileHash "$env:USERPROFILE\Downloads\TaskbarCompanions-Setup-1.0.0-x64.exe"
   ```

3. Compare the **Hash** it shows with that file's line in SHA256SUMS.txt; upper or lower case doesn't matter. If they match, your file is exactly the one that was published.

With the [GitHub CLI](https://cli.github.com/), you can also confirm that the file was built from this repository by its release workflow:

```powershell
gh attestation verify TaskbarCompanions-Setup-1.0.0-x64.exe --repo levi2111/taskbar-companions
```

## Updating

The app doesn't update itself or check for updates. To hear about new versions, sign in to GitHub, open the [repository](https://github.com/levi2111/taskbar-companions), click **Watch → Custom**, tick **Releases**, and click **Apply**.

- **Installer:** download the new installer and run it. It replaces the old version and keeps your settings. If the app is running, the installer asks you to quit it first: right-click the tray icon and choose **Quit**.
- **Portable:** quit the app, extract the new zip into the same folder, replacing the old files, and start it again. Your settings are kept.
- **From source:** run `git pull`, then `.\launch.ps1 -Rebuild`.

The [changelog](../CHANGELOG.md) lists what changed in each version.

## Uninstalling

1. Quit the app: right-click the tray icon or a companion, and choose **Quit**.
2. Remove it:
   - **Installer:** open **Settings → Apps → Installed apps** (on Windows 10, **Settings → Apps → Apps & features**), find **Taskbar Companions**, click **…** or the app's name, and choose **Uninstall**. This removes the app, its shortcuts, its startup entry and its settings folder.
   - **Portable:** delete the folder you extracted, and any shortcut you made in `shell:startup`. Then remove the settings folder: press **Windows + R**, type `%LOCALAPPDATA%`, press **Enter**, and delete the **TaskbarCompanions** folder.
   - **From source:** delete the folder you cloned, and the `%LOCALAPPDATA%\TaskbarCompanions` folder.
3. If you set up the status line bridge, remove the `"statusLine"` line from `%USERPROFILE%\.claude\settings.json`, along with the comma that separated it.
4. If you pinned the app, right-click its taskbar button and choose **Unpin from taskbar**.

The app never changes anything in Claude Code or Codex, so there's nothing else to undo.

## Troubleshooting

### Windows blocks the app with Smart App Control

Smart App Control, which some newer Windows 11 PCs have turned on, blocks apps that aren't code-signed. Unlike SmartScreen, it has no **Run anyway** button. Turning Smart App Control off lowers protection for every app, and on some versions of Windows it can't be turned back on without resetting Windows, so we don't recommend doing that just for this app. Code signing is planned; until then, the app can't run on PCs with Smart App Control on.

### My antivirus flagged or removed the app

New, unsigned apps are sometimes flagged by mistake. [Check your download](#check-that-your-download-is-genuine) against SHA256SUMS.txt. If it matches, it's the published file: you can report the false positive to your antivirus maker (for Microsoft Defender, [submit the file to Microsoft](https://www.microsoft.com/wdsi/filesubmission)) and let us know in an [issue](https://github.com/levi2111/taskbar-companions/issues).

### I can't see the companions

- They hide while a fullscreen app or game is in front, and come back when it's gone.
- If they're minimized, click the app's taskbar button, or double-click the tray icon.
- If they're off screen, for example after unplugging a monitor, right-click the tray icon and choose **Bring companions home**.
- If one is hidden, turn it back on in **Settings**.

### The bars are empty, or say "as of … ago"

The app shows what Claude Code and Codex last saved; it doesn't track your usage itself.

- **Empty bars:** open **Settings** to see what the app found. Use Claude Code or Codex once so they save a reading. If they keep their data somewhere unusual, choose the folder in Settings.
- **"as of 2h 10m ago":** the reading is that old, because Claude Code or Codex hasn't checked since. Using them updates it. For Claude Code in a terminal, the [status line bridge](#optional-the-status-line-bridge) keeps it fresh.
- Usage on claude.ai or in the Claude apps counts toward the same limits, but shows up only after Claude Code's next check.

### The Codex companion is a little explorer, not the Codex pet

The Codex pet's artwork belongs to OpenAI and isn't included; the app reads it from your Codex extension. Without the extension, or if its artwork changes, the terminal explorer comes along instead.

### "Taskbar Companions is currently running" when installing or uninstalling

Quit the app first: right-click the tray icon and choose **Quit**. Then click **OK**, or start the installer again.

### Still stuck?

[Open an issue](https://github.com/levi2111/taskbar-companions/issues/new/choose) with the version (from **About**), your Windows version, how you installed the app, and what you see. Please don't paste the contents of `.claude.json` or `.credentials.json`, or any tokens.
