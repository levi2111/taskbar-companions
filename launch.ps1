param([switch]$Rebuild)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'TaskbarCompanions/TaskbarCompanions.csproj'
$executable = Join-Path $PSScriptRoot 'app/TaskbarCompanions.exe'
if ($Rebuild -or -not (Test-Path $executable)) {
    Get-Process TaskbarCompanions -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $executable } | Stop-Process
    dotnet publish $project --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -c Release -o (Join-Path $PSScriptRoot 'app') --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}
Start-Process -FilePath $executable -WindowStyle Hidden
