param(
    [Parameter(Mandatory = $true)][string]$Manifest,
    [switch]$SkipBlender,
    [switch]$SkipUnity
)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifestPath = (Resolve-Path (Join-Path $projectRoot $Manifest)).Path
$blender = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'

if (-not $SkipBlender) {
    if (-not (Test-Path -LiteralPath $blender)) { throw "Blender 5.0 was not found." }
    & $blender --background --python (Join-Path $PSScriptRoot 'build_character_lods.py') -- $manifestPath
    if ($LASTEXITCODE -ne 0) { throw "Blender character LOD generation failed." }
}

if (-not $SkipUnity) {
    if (-not (Test-Path -LiteralPath $unity)) { throw "Unity 6000.3.10f1 was not found." }
    & $unity -batchmode -projectPath $projectRoot -quit -executeMethod UpIzUpMini.EditorTools.Mini107CharacterMotionProof.BuildValidateCapture -logFile (Join-Path $projectRoot 'Logs\MINI-107-motion-proof.log')
    if ($LASTEXITCODE -ne 0) { throw "Unity character motion proof failed." }
}

Write-Host 'UP IZ UP CHARACTER PIPELINE PASS'
