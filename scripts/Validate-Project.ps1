param([string]$Root = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path
foreach ($required in @('Assets/Scenes/SanOro.unity','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','README.md','THIRD_PARTY_NOTICES.md','Build.bat','Play.bat')) {
    if (!(Test-Path -LiteralPath (Join-Path $Root $required))) { throw "Missing project file: $required" }
}
$null = Get-Content -LiteralPath (Join-Path $Root 'Packages/manifest.json') -Raw | ConvertFrom-Json
$null = Get-Content -LiteralPath (Join-Path $Root 'Packages/packages-lock.json') -Raw | ConvertFrom-Json
$guids = @{}
foreach ($meta in Get-ChildItem -LiteralPath (Join-Path $Root 'Assets') -Recurse -File -Filter '*.meta') {
    $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([0-9a-f]{32})\s*$')
    if (!$match.Success) { throw "Invalid Unity GUID: $($meta.FullName)" }
    $guid = $match.Groups[1].Value
    if ($guids.ContainsKey($guid)) { throw "Duplicate Unity GUID: $($meta.FullName) and $($guids[$guid])" }
    $guids[$guid] = $meta.FullName
    $asset = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
    if (!(Test-Path -LiteralPath $asset)) { throw "Orphaned metadata: $($meta.FullName)" }
}
foreach ($source in Get-ChildItem -LiteralPath (Join-Path $Root 'Assets/Scripts') -File -Filter '*.cs') {
    if (!(Test-Path -LiteralPath ($source.FullName + '.meta'))) { throw "Script has no GUID: $($source.Name)" }
}
$readme = Get-Content -LiteralPath (Join-Path $Root 'README.md') -Raw
foreach ($match in [regex]::Matches($readme, '!\[[^\]]*\]\(([^)]+)\)')) {
    $relative = $match.Groups[1].Value
    if ($relative -notmatch '^https?://' -and !(Test-Path -LiteralPath (Join-Path $Root $relative))) { throw "Missing README image: $relative" }
}
Write-Output "PROJECT_VALIDATION_OK ($($guids.Count) unique Unity GUIDs)"
