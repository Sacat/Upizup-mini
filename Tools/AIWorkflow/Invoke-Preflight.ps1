param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Codex', 'Claude', 'User')]
    [string]$Agent,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^MINI-[0-9]+$')]
    [string]$TaskId,

    [switch]$AllowUnclaimed,

    [switch]$Detailed
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$handoffPath = Join-Path $projectRoot 'PROJECT-HANDOFF.md'
$requiredPaths = @(
    'AGENTS.md',
    'PROJECT-HANDOFF.md',
    'TASKS.md',
    'Docs\CURRENT.md',
    'Docs\AI-PRODUCTION-WORKFLOW.md',
    'Docs\WORK-PACKET-TEMPLATE.md',
    'Docs\VISUAL-APPROVAL-REGISTER.md',
    'ProjectSettings\ProjectVersion.txt',
    'Packages\manifest.json'
)

$missing = @($requiredPaths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $projectRoot $_)) })
if ($missing.Count -gt 0) {
    Write-Error ('Missing required workflow files: ' + ($missing -join ', '))
}

$handoff = Get-Content -Raw -LiteralPath $handoffPath
$claimPattern = '(?s)### Current claim\s*```yaml\s*current_owner:\s*(?<owner>[^\r\n]+)\s*active_task:\s*(?<task>[^\r\n]+)'
$claim = [regex]::Match($handoff, $claimPattern)
if (-not $claim.Success) {
    Write-Error 'Could not read the Current claim block in PROJECT-HANDOFF.md.'
}

$owner = $claim.Groups['owner'].Value.Trim()
$activeTask = $claim.Groups['task'].Value.Trim()
$claimMatches = $owner -eq $Agent -and $activeTask -eq $TaskId
$unclaimed = $owner -eq 'None' -and $activeTask -eq 'None'

if (-not $claimMatches -and -not ($AllowUnclaimed -and $unclaimed)) {
    Write-Error "Ownership mismatch. Handoff says owner='$owner', task='$activeTask'; requested owner='$Agent', task='$TaskId'."
}

$versionLine = [string](Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') -First 1)
$gitArgs = @('-c', "safe.directory=$($projectRoot -replace '\\','/')", '-C', $projectRoot, 'status', '--short')
$dirtyLines = @(& git @gitArgs 2>$null)
$unityProcesses = @(Get-Process -Name Unity -ErrorAction SilentlyContinue)

$result = [ordered]@{
    project = $projectRoot
    requestedAgent = $Agent
    requestedTask = $TaskId
    handoffOwner = $owner
    handoffTask = $activeTask
    ownershipReady = ($claimMatches -or ($AllowUnclaimed -and $unclaimed))
    unityVersion = $versionLine
    unityEditorRunning = ($unityProcesses.Count -gt 0)
    dirtyFileCount = $dirtyLines.Count
    dirtyFiles = if ($Detailed) { $dirtyLines } else { @() }
    warning = if ($dirtyLines.Count -gt 0) {
        'Working tree is dirty. Reserve exact files and do not commit or overwrite unrelated changes.'
    } else {
        $null
    }
}

$result | ConvertTo-Json -Depth 4
