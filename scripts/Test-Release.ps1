param([Parameter(Mandatory=$true)][string]$ManifestPath)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$root = [IO.Path]::GetFullPath((Split-Path -Parent (Resolve-Path -LiteralPath $ManifestPath).Path))
if ($manifest.schema -ne 1 -or @($manifest.files).Count -eq 0) { throw 'Invalid release manifest' }
$known = @{}
foreach ($entry in $manifest.files) {
    if ([IO.Path]::IsPathRooted($entry.path)) { throw 'Absolute manifest path rejected' }
    $path = [IO.Path]::GetFullPath((Join-Path $root $entry.path))
    if (!$path.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path outside release folder' }
    if ($known.ContainsKey($path)) { throw "Duplicate entry: $($entry.path)" }; $known[$path] = $true
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing file: $($entry.path)" }
    if ((Get-Item -LiteralPath $path).Length -ne $entry.bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Integrity mismatch: $($entry.path)" }
}
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
    if ($file.FullName -ne (Resolve-Path -LiteralPath $ManifestPath).Path -and !$known.ContainsKey($file.FullName)) { throw "Unexpected release file: $($file.FullName)" }
}
Write-Output "RELEASE_INTEGRITY_OK ($($known.Count) files)"
