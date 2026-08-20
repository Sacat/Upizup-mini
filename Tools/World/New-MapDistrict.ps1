[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')][string]$MapId,
    [Parameter(Mandatory = $true)][string]$DisplayName,
    [string]$Country = 'Dominica',
    [string]$Island = 'Dominica',
    [Parameter(Mandatory = $true)][string]$Region,
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'
$docsRoot = Join-Path $ProjectRoot "Docs\Maps\$MapId"
$assetsRoot = Join-Path $ProjectRoot "Assets\UpIzUpMini\Maps\Regions\$MapId"
$packetTemplate = Join-Path $ProjectRoot 'Docs\Templates\MAP-DISTRICT-PACKET.md'
$manifestTemplate = Join-Path $ProjectRoot 'Docs\Templates\MAP-DISTRICT-MANIFEST.template.json'

if ((Test-Path -LiteralPath $docsRoot) -or (Test-Path -LiteralPath $assetsRoot)) {
    throw "Map district '$MapId' already exists. Refusing to overwrite it."
}

foreach ($path in @($docsRoot, (Join-Path $docsRoot 'Evidence'), $assetsRoot, (Join-Path $assetsRoot 'Source'), (Join-Path $assetsRoot 'Generated'), (Join-Path $assetsRoot 'Staging'))) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

$tokens = @{
    '__MAP_ID__' = $MapId
    '__DISPLAY_NAME__' = $DisplayName
    '__COUNTRY__' = $Country
    '__ISLAND__' = $Island
    '__REGION__' = $Region
}

function Expand-Template([string]$SourcePath, [string]$DestinationPath) {
    $content = Get-Content -LiteralPath $SourcePath -Raw
    foreach ($entry in $tokens.GetEnumerator()) { $content = $content.Replace($entry.Key, $entry.Value) }
    Set-Content -LiteralPath $DestinationPath -Value $content -Encoding utf8NoBOM
}

Expand-Template $packetTemplate (Join-Path $docsRoot 'DISTRICT-PACKET.md')
Expand-Template $manifestTemplate (Join-Path $docsRoot 'MANIFEST.json')

Set-Content -LiteralPath (Join-Path $docsRoot 'SOURCES.md') -Encoding utf8NoBOM -Value @"
# $DisplayName — Sources

Record every dataset/image with source ID, publisher, retrieval date, bounds, licence, attribution, redistribution limits and exact use. Commercial satellite imagery is reference-only unless its licence explicitly permits production use.
"@

Set-Content -LiteralPath (Join-Path $docsRoot 'APPROVALS.md') -Encoding utf8NoBOM -Value @"
# $DisplayName — Approvals

Record stable evidence IDs, paths, status (`approved`, `rejected`, `pending`), the user's exact words, approved aspects and aspects still editable. Rejected evidence can never be reused as approval evidence.
"@

Set-Content -LiteralPath (Join-Path $docsRoot 'Evidence\.gitkeep') -Value '' -Encoding ascii

& (Join-Path $PSScriptRoot 'Test-MapDistrict.ps1') -MapId $MapId -Stage scaffold -ProjectRoot $ProjectRoot | Out-Null
[pscustomobject]@{ mapId = $MapId; docs = $docsRoot; assets = $assetsRoot; stage = 'scaffold'; valid = $true } | ConvertTo-Json
