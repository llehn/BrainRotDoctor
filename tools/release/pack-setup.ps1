<#
.SYNOPSIS
  Appends the published app onto the starter PE so one setup file can extract
  and launch it.
#>
param(
    [Parameter(Mandatory = $true)][string] $Starter,
    [Parameter(Mandatory = $true)][string] $App,
    [Parameter(Mandatory = $true)][string] $Out
)

$starterBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $Starter))
$appBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $App))
$outPath = $Out
$outDir = Split-Path -Parent $outPath
if ($outDir) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$magic = [System.Text.Encoding]::ASCII.GetBytes('BRDSET01')
$size = [BitConverter]::GetBytes([int64]$appBytes.Length)

$stream = [System.IO.File]::Open($outPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
try {
    $stream.Write($starterBytes, 0, $starterBytes.Length)
    $stream.Write($appBytes, 0, $appBytes.Length)
    $stream.Write($size, 0, $size.Length)
    $stream.Write($magic, 0, $magic.Length)
}
finally {
    $stream.Dispose()
}

Write-Output "Wrote $outPath ($((Get-Item $outPath).Length) bytes)"
