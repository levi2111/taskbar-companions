<#
Builds the release files for Taskbar Companions into dist\:

  TaskbarCompanions-Setup-<version>-x64.exe          installer: per Windows account, no administrator rights (needs Inno Setup 6)
  TaskbarCompanions-<version>-win-x64-portable.zip   the same files, to extract anywhere
  SHA256SUMS.txt                                     checksums of both
  release-notes.md                                   text for the GitHub release, with this version's CHANGELOG entry

The exe is self-contained: it carries the .NET runtime, so people install nothing else.

    powershell -ExecutionPolicy Bypass -File packaging\build.ps1
#>
param([switch]$RequireInstaller)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'TaskbarCompanions\TaskbarCompanions.csproj'
$version = ([xml](Get-Content $project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Set <Version> in the project file to x.y.z (found '$version')." }
$url = 'https://github.com/levi2111/taskbar-companions'
$dist = Join-Path $root 'dist'
$publish = Join-Path $dist 'publish'
$stage = Join-Path $dist 'stage'
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }

# The runtime comes from Microsoft's runtime packs on nuget.org; packaging\NuGet.Config allows nothing else.
# DisableTransitiveFrameworkReferenceDownloads skips the ASP.NET Core runtime pack, which the app doesn't use.
dotnet publish $project --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DisableTransitiveFrameworkReferenceDownloads=true -p:DebugType=embedded -p:ContinuousIntegrationBuild=true -o $publish --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

# The logic checks run against the exact exe that ships.
$exe = Join-Path $publish 'TaskbarCompanions.exe'
$checks = [System.Diagnostics.Process]::Start($exe, '--self-test')
$checks.WaitForExit()
$result = Join-Path $publish 'checks.txt'
Get-Content $result
if ($checks.ExitCode -ne 0) { throw 'Self-test failed.' }
Remove-Item $result

# What people get: the app, the status line bridge, and the notices that go with them.
Copy-Item $publish $stage -Recurse
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $stage 'LICENSE.txt')
Copy-Item (Join-Path $root 'PRIVACY.md') (Join-Path $stage 'PRIVACY.txt')
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') (Join-Path $stage 'THIRD-PARTY-NOTICES.txt')
@"
Taskbar Companions $version

Start TaskbarCompanions.exe. The companions appear above the right end of your taskbar.
Right-click one of them, or the tray icon, for Settings, About and Quit.

Settings and saved positions are kept in %LOCALAPPDATA%\TaskbarCompanions.
The optional Claude Code status line bridge is in the bridge folder; Settings can copy the line that turns it on.

Install guide, help and source code: $url
Privacy policy: PRIVACY.txt. License: LICENSE.txt. Third-party notices: THIRD-PARTY-NOTICES.txt and the licenses folder.

Unofficial fan project, not affiliated with, endorsed by or sponsored by Anthropic or OpenAI.
"@ | Set-Content (Join-Path $stage 'README.txt') -Encoding UTF8

# The bundled .NET runtime is MIT-licensed and carries notices for the components inside it; they ship too.
$packages = $env:NUGET_PACKAGES
if (-not $packages) { $packages = Join-Path $env:USERPROFILE '.nuget\packages' }
$assets = Get-Content (Join-Path $root 'TaskbarCompanions\obj\project.assets.json') -Raw | ConvertFrom-Json
$packs = @($assets.project.frameworks.PSObject.Properties | ForEach-Object { $_.Value.downloadDependencies } | Where-Object { $_.name -like '*.Runtime.win-x64' })
if ($packs.Count -eq 0) { throw 'No runtime packs listed in project.assets.json.' }
$licenses = New-Item -ItemType Directory (Join-Path $stage 'licenses')
foreach ($pack in $packs) {
    $packVersion = $pack.version.Trim('[', ']').Split(',')[0].Trim()
    $folder = Join-Path $packages ('{0}\{1}' -f $pack.name.ToLowerInvariant(), $packVersion)
    # Every pack has a license; the core runtime also lists the third-party code inside it.
    $notices = @(Get-ChildItem $folder -File | Where-Object { $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES)(\.txt)?$' })
    if (-not ($notices | Where-Object { $_.Name -like 'LICENSE*' })) { throw "No license file in $folder." }
    foreach ($notice in $notices) {
        Copy-Item $notice.FullName (Join-Path $licenses ('{0}.{1}.txt' -f $pack.name, $notice.BaseName.ToLowerInvariant()))
    }
}

$zip = Join-Path $dist "TaskbarCompanions-$version-win-x64-portable.zip"
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $stage -Recurse -File) {
        # Forward slashes, as the zip format expects.
        $name = $file.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $name, [System.IO.Compression.CompressionLevel]::Optimal)
    }
}
finally { $archive.Dispose() }

# Inno Setup installs per user (winget) or for everyone (Chocolatey, as on GitHub's runners); take the newest.
$iscc = @((Join-Path $env:LOCALAPPDATA 'Programs'), ${env:ProgramFiles(x86)}, $env:ProgramFiles) | Where-Object { $_ } |
    ForEach-Object { Get-ChildItem (Join-Path $_ 'Inno Setup *\ISCC.exe') -ErrorAction SilentlyContinue } |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if ($iscc) {
    & $iscc /Q "/DAppVersion=$version" "/DStageDir=$stage" "/O$dist" (Join-Path $PSScriptRoot 'TaskbarCompanions.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
}
elseif ($RequireInstaller) { throw 'Inno Setup 6 is not installed.' }
else { Write-Warning 'Inno Setup 6 was not found, so there is no installer this time. Install it with: winget install JRSoftware.InnoSetup' }

Get-ChildItem $dist -File | Where-Object { $_.Extension -in '.exe', '.zip' } | Sort-Object Name | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
} | Set-Content (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII

$changelog = Get-Content (Join-Path $root 'CHANGELOG.md') -Raw
$entry = [regex]::Match($changelog, "(?ms)^## \[$([regex]::Escape($version))\][^\n]*\n(.*?)(?=^## |\z)").Groups[1].Value.Trim()
if (-not $entry) { throw "CHANGELOG.md has no entry for $version." }
$notes = @"
## Download

- **Most people:** ``TaskbarCompanions-Setup-$version-x64.exe``. Run it and follow the steps. It installs for your Windows account only, without administrator rights, and adds a Start menu entry and an uninstaller.
- **Portable:** ``TaskbarCompanions-$version-win-x64-portable.zip``. Extract it anywhere you like and run ``TaskbarCompanions.exe``.

Both include everything the app needs; there's nothing else to install. The files aren't code-signed yet, so Windows may say "Windows protected your PC": choose **More info**, then **Run anyway**. The [install guide]($url/blob/v$version/docs/INSTALL.md) walks through every step, including updating and uninstalling.

## What's new

$entry

## Check your download

``SHA256SUMS.txt`` lists each file's SHA-256 checksum; in PowerShell, ``Get-FileHash <file>`` shows yours. With the GitHub CLI you can also confirm a file was built by this repository's release workflow: ``gh attestation verify <file> --repo levi2111/taskbar-companions``.

Unofficial fan project, not affiliated with, endorsed by or sponsored by Anthropic or OpenAI. [Privacy policy]($url/blob/v$version/PRIVACY.md) · [License]($url/blob/v$version/LICENSE)
"@
[System.IO.File]::WriteAllText((Join-Path $dist 'release-notes.md'), $notes, (New-Object System.Text.UTF8Encoding $false))

Get-ChildItem $dist -File | Format-Table Name, @{ Name = 'MB'; Expression = { [math]::Round($_.Length / 1MB, 1) } } -AutoSize
