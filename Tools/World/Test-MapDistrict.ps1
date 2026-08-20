[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')][string]$MapId,
    [ValidateSet('scaffold', 'map_truth', 'approved_preview', 'graybox', 'approved_graybox', 'migration', 'runtime_acceptance', 'decorated')][string]$Stage = 'scaffold',
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'
$docsRoot = Join-Path $ProjectRoot "Docs\Maps\$MapId"
$assetsRoot = Join-Path $ProjectRoot "Assets\UpIzUpMini\Maps\Regions\$MapId"
$manifestPath = Join-Path $docsRoot 'MANIFEST.json'
$errors = [System.Collections.Generic.List[string]]::new()
$manifest = $null

function Require([bool]$Condition, [string]$Message) { if (-not $Condition) { $errors.Add($Message) } }

foreach ($required in @($manifestPath, (Join-Path $docsRoot 'DISTRICT-PACKET.md'), (Join-Path $docsRoot 'SOURCES.md'), (Join-Path $docsRoot 'APPROVALS.md'))) {
    Require (Test-Path -LiteralPath $required) "Missing required path: $required"
}

if (Test-Path -LiteralPath $manifestPath) {
    try { $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json } catch { $errors.Add("Invalid MANIFEST.json: $($_.Exception.Message)") }
}

if ($null -ne $manifest) {
    Require ($manifest.schemaVersion -eq 1) 'schemaVersion must be 1.'
    Require ($manifest.mapId -eq $MapId) "Manifest mapId must equal '$MapId'."
    Require (-not [string]::IsNullOrWhiteSpace($manifest.displayName)) 'displayName is required.'
    Require (-not [string]::IsNullOrWhiteSpace($manifest.identity.country)) 'identity.country is required.'
    Require (-not [string]::IsNullOrWhiteSpace($manifest.identity.island)) 'identity.island is required.'
    Require ($manifest.coordinateSystem.metresPerUnityUnit -eq 1.0) 'metresPerUnityUnit must remain 1.0.'
    Require ($manifest.coordinateSystem.travelCompression -gt 0 -and $manifest.coordinateSystem.travelCompression -le 1) 'travelCompression must be > 0 and <= 1.'
    Require (($manifest.evidenceCameras | ForEach-Object id | Sort-Object -Unique).Count -ge 3) 'At least three fixed evidence cameras are required.'
    Require ($manifest.mobileBudget.maximumActiveAnimatedNpcs -gt 0) 'A positive animated-NPC budget is required.'
    Require ($manifest.mobileBudget.maximumRoadMeshVertices -gt 0) 'A positive road vertex budget is required.'

    $stageOrder = @('scaffold', 'map_truth', 'approved_preview', 'graybox', 'approved_graybox', 'migration', 'runtime_acceptance', 'decorated')
    $requestedIndex = [Array]::IndexOf($stageOrder, $Stage)
    if ($requestedIndex -ge 1) {
        Require ($manifest.sources.Count -gt 0) 'Map-truth requires at least one licensed source.'
        Require ($null -ne $manifest.bounds.south -and $null -ne $manifest.bounds.west -and $null -ne $manifest.bounds.north -and $null -ne $manifest.bounds.east) 'Map-truth requires numeric bounds.'
        Require ($null -ne $manifest.coordinateSystem.originLatitude -and $null -ne $manifest.coordinateSystem.originLongitude) 'Map-truth requires a metric origin.'
        Require ($manifest.anchorsFile -ne 'pending') 'Map-truth requires an anchors file.'
    }
    if ($requestedIndex -ge 2) {
        Require ($manifest.approvals.mapTruthStatus -eq 'approved') 'Approved-preview requires user-approved map truth.'
        Require ($manifest.approvals.mapTruthEvidence.Count -gt 0) 'Approved-preview requires retained evidence.'
    }
    if ($requestedIndex -ge 3) {
        Require ($manifest.passability.roadsContinuous -eq $true) 'Graybox requires continuous roads.'
        Require ($manifest.passability.roadRootCount -gt 0) 'Graybox requires generated road roots.'
        Require ($manifest.passability.roadColliderCount -eq $manifest.passability.roadRootCount) 'Every road root requires a collider.'
        Require ($manifest.passability.buildingsClearDrivableRoads -eq $true) 'Graybox requires building/road clearance.'
        Require ($manifest.passability.requiredConnectionsPresent -eq $true) 'Graybox requires all named connections.'
    }
    if ($requestedIndex -ge 4) {
        Require ($manifest.approvals.grayboxStatus -eq 'approved') 'Approved-graybox requires user approval.'
        Require ($manifest.approvals.grayboxEvidence.Count -gt 0) 'Approved-graybox requires retained evidence.'
    }
    if ($requestedIndex -ge 5) {
        Require ($manifest.migration.rollbackCommit -ne 'pending') 'Migration requires a rollback commit.'
        Require ($manifest.migration.roleMappingFile -ne 'pending') 'Migration requires a role-to-anchor mapping.'
        Require ($manifest.migration.authoritativeBuilder -ne 'pending') 'Migration requires an authoritative builder.'
        Require ($manifest.migration.saveCompatibilityReviewed -eq $true) 'Migration requires save compatibility review.'
        Require ($manifest.passability.edgeAndFallRecoveryPlanned -eq $true) 'Migration requires edge/fall recovery planning.'
    }
    if ($requestedIndex -ge 6) {
        Require ($manifest.approvals.runtimeStatus -eq 'accepted') 'Runtime acceptance requires a user-accepted walking/driving test.'
    }
}

$result = [pscustomobject]@{ mapId = $MapId; requestedStage = $Stage; valid = ($errors.Count -eq 0); errorCount = $errors.Count; errors = $errors }
$result | ConvertTo-Json -Depth 6
if ($errors.Count -gt 0) { exit 1 }
