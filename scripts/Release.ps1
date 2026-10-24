param([switch]$UseExistingBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
& (Join-Path $PSScriptRoot 'Validate-Project.ps1') -Root $root
function Invoke-HiddenChecked([string]$Executable, [string[]]$Arguments, [int]$TimeoutSeconds) {
    $process = Start-Process -FilePath $Executable -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (!$process.WaitForExit(1000)) { if ($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds) { $process.Kill(); throw "Timed out: $Executable" } }
    if ($process.ExitCode -ne 0) { throw "Process failed ($($process.ExitCode)): $Executable" }
}
if (!$UseExistingBuild) {
    $version = [regex]::Match((Get-Content ProjectSettings/ProjectVersion.txt -Raw), 'm_EditorVersion: ([^\s]+)').Groups[1].Value
    $editor = $env:UNITY_EDITOR
    if (!$editor) { $editor = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe" }
    Invoke-HiddenChecked $editor @('-batchmode','-nographics','-projectPath',('"'+$root+'"'),'-executeMethod','SunsetState.Editor.ProjectSetup.BuildWindows','-quit','-logFile',('"'+(Join-Path $root 'release-build.log')+'"')) 600
    if (!(Select-String -LiteralPath release-build.log -SimpleMatch 'SUNSET_BUILD_OK' -Quiet)) { throw 'Build success marker missing' }
}
$build = Join-Path $root 'Builds/Windows'
foreach ($required in @('SunsetState.exe','UnityPlayer.dll','SunsetState_Data/globalgamemanagers','build-info.json')) { if (!(Test-Path -LiteralPath (Join-Path $build $required))) { throw "Incomplete build: $required" } }
$info = Get-Content -LiteralPath (Join-Path $build 'build-info.json') -Raw | ConvertFrom-Json
if ($info.version -notmatch '^\d+\.\d+\.\d+$') { throw 'Build version missing' }
$testLog = Join-Path $root 'release-smoke.log'
Invoke-HiddenChecked (Join-Path $build 'SunsetState.exe') @('-batchmode','-nographics','-sunset-smoke','-logFile',('"'+$testLog+'"')) 240
$testText = Get-Content -LiteralPath $testLog -Raw
if (!$testText.Contains('SUNSET_SMOKE_OK') -or $testText.Contains('SUNSET_SMOKE_FAILED') -or $testText -match '(?m)^\w*Exception:') { throw 'Release smoke validation failed' }
$releaseId = 'SunsetState-' + $info.version + '-' + $info.channel + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
$stage = Join-Path $root ('Builds/Releases/' + $releaseId)
New-Item -ItemType Directory -Path $stage -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $build -Recurse -File) {
    $relative = $file.FullName.Substring($build.Length + 1)
    if ($relative -match 'DoNotShip|\.pdb$|\.mdb$|\.log$|^release-manifest\.json$') { continue }
    $destination = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
Copy-Item -LiteralPath README.md,THIRD_PARTY_NOTICES.md,VALIDATION.md -Destination $stage -Force
$launcher = '@echo off' + "`r`n" + 'start "Sunset State" /d "%~dp0" "%~dp0SunsetState.exe"' + "`r`n"
[IO.File]::WriteAllText((Join-Path $stage 'Play.bat'), $launcher, [Text.Encoding]::ASCII)
$files = @(foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName) { [ordered]@{path=$file.FullName.Substring($stage.Length + 1).Replace('\','/');bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash} })
$revision = (git rev-parse HEAD 2>$null); if ($LASTEXITCODE -ne 0) { $revision = 'unknown' }
$manifest = [ordered]@{schema=1;version=$info.version;channel=$info.channel;builtUtc=$info.builtUtc;packagedUtc=[DateTime]::UtcNow.ToString('O');sourceRevision=$revision;sourceHasChanges=[bool](git status --porcelain);smokeVerified=$true;files=$files}
$manifestPath = Join-Path $stage 'release-manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
& (Join-Path $PSScriptRoot 'Test-Release.ps1') -ManifestPath $manifestPath
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = $stage + '.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
($hash + '  ' + [IO.Path]::GetFileName($archive)) | Set-Content -LiteralPath ($archive + '.sha256') -Encoding ascii
Write-Output "RELEASE_OK $archive"
