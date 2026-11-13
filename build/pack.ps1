# Builds the installer (DAXLogin-Setup.exe) and update packages into .\Releases
#   .\build\pack.ps1 -Version 1.0.0
# Requires: .NET SDK and the vpk tool (dotnet tool install -g vpk --version 1.2.161)
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root 'publish'
$releases = Join-Path $root 'Releases'

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force -Confirm:$false }

dotnet test (Join-Path $root 'tests\DAXLogin.Tests') -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

dotnet publish (Join-Path $root 'src\DAXLogin') -c Release -o $publish -p:Version=$Version -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

$vpkArgs = @(
    'pack',
    '--packId', 'DAXLogin',
    '--packVersion', $Version,
    '--packDir', $publish,
    '--mainExe', 'DAXLogin.exe',
    '--packTitle', 'DAXLogin',
    '--packAuthors', 'DAXLogin',
    '--icon', (Join-Path $root 'src\DAXLogin\Assets\app.ico'),
    '--splashImage', (Join-Path $PSScriptRoot 'splash.png'),
    '--framework', 'net48',
    '--outputDir', $releases
)
if ($Notes) {
    $notesFile = Join-Path $env:TEMP 'daxlogin-notes.md'
    Set-Content $notesFile $Notes -Encoding utf8
    $vpkArgs += @('--releaseNotes', $notesFile)
}
vpk @vpkArgs
if ($LASTEXITCODE -ne 0) { throw 'vpk pack failed.' }

# Styled installer: wraps the Velopack Setup.exe that vpk just produced.
$payload = Join-Path $releases 'DAXLogin-win-Setup.exe'
$setupOut = Join-Path $root 'src\DAXLogin.Setup\bin\Release\net48\DAXLogin-Setup.exe'
dotnet build (Join-Path $root 'src\DAXLogin.Setup') -c Release -p:Version=$Version "-p:PayloadPath=$payload" -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
Copy-Item $setupOut (Join-Path $releases 'DAXLogin-Setup.exe') -Force

Write-Host "Done. Installer: $releases\DAXLogin-Setup.exe"
