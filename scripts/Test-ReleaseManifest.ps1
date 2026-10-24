$ErrorActionPreference = 'Stop'
$folder = Join-Path ([IO.Path]::GetTempPath()) ('sunset-manifest-check-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $folder | Out-Null
try {
    $payload = Join-Path $folder 'game.txt'; $manifestPath = Join-Path $folder 'release-manifest.json'
    [IO.File]::WriteAllText($payload, 'verified build')
    $entry = @{path='game.txt';bytes=(Get-Item $payload).Length;sha256=(Get-FileHash $payload -Algorithm SHA256).Hash}
    @{schema=1;files=@($entry)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'Test-Release.ps1') -ManifestPath $manifestPath
    [IO.File]::WriteAllText($payload, 'modified build')
    $rejected = $false
    try { & (Join-Path $PSScriptRoot 'Test-Release.ps1') -ManifestPath $manifestPath } catch { $rejected = $true }
    if (!$rejected) { throw 'Manifest accepted modified build' }
    $entry.path = '../outside.txt'
    @{schema=1;files=@($entry)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $rejected = $false
    try { & (Join-Path $PSScriptRoot 'Test-Release.ps1') -ManifestPath $manifestPath } catch { $rejected = $true }
    if (!$rejected) { throw 'Manifest accepted a path outside the release folder' }
    Write-Output 'RELEASE_MANIFEST_NEGATIVE_TESTS_OK'
} finally {
    foreach ($file in Get-ChildItem -LiteralPath $folder -File) { Remove-Item -LiteralPath $file.FullName -Force }
    [IO.Directory]::Delete($folder)
}
