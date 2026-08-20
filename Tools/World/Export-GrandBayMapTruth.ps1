param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [switch]$Offline
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$taskDir = Join-Path $ProjectRoot "Logs\Tasks\MINI-094"
$rawPath = Join-Path $taskDir "GrandBayPhase1.osm.json"
$previewPath = Join-Path $taskDir "LalayToBeach-Overhead.png"
$summaryPath = Join-Path $taskDir "MapTruthSummary.json"
$anchorPath = Join-Path $ProjectRoot "Docs\MAP-ANCHORS.json"
New-Item -ItemType Directory -Force -Path $taskDir | Out-Null

$bounds = [ordered]@{
    south = 15.236
    west = -61.326
    north = 15.252
    east = -61.306
}
$origin = [ordered]@{
    latitude = 15.2450638
    longitude = -61.3181049
    unityX = 0
    unityZ = 0
}

if (-not $Offline) {
    $bbox = "$($bounds.south),$($bounds.west),$($bounds.north),$($bounds.east)"
    $query = "[out:json][timeout:60];(way[highway]($bbox);way[waterway]($bbox);way[natural=coastline]($bbox);nwr[name]($bbox););out tags center geom;"
    $uri = "https://overpass-api.de/api/interpreter?data=$([Uri]::EscapeDataString($query))"
    $headers = @{ "User-Agent" = "UpIzUpMiniMapTruth/1.0 (local game development)" }
    $response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get
    $response | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $rawPath -Encoding utf8
}

if (-not (Test-Path -LiteralPath $rawPath)) {
    throw "No OSM source snapshot exists at $rawPath. Run once without -Offline."
}

$osm = Get-Content -Raw -LiteralPath $rawPath | ConvertFrom-Json
$elements = @($osm.elements)

function Get-ElementPoint($element) {
    if ($null -ne $element.lat -and $null -ne $element.lon) {
        return [pscustomobject]@{ lat = [double]$element.lat; lon = [double]$element.lon }
    }
    if ($null -ne $element.center) {
        return [pscustomobject]@{ lat = [double]$element.center.lat; lon = [double]$element.center.lon }
    }
    if ($null -ne $element.geometry -and @($element.geometry).Count -gt 0) {
        $lat = (@($element.geometry) | Measure-Object -Property lat -Average).Average
        $lon = (@($element.geometry) | Measure-Object -Property lon -Average).Average
        return [pscustomobject]@{ lat = [double]$lat; lon = [double]$lon }
    }
    return $null
}

function Find-NamedPoint([string]$name) {
    $element = $elements | Where-Object { $_.tags.name -eq $name } | Select-Object -First 1
    if ($null -eq $element) { return $null }
    return Get-ElementPoint $element
}

function New-Anchor(
    [string]$id,
    [string]$displayName,
    [string]$category,
    [double]$latitude,
    [double]$longitude,
    [string]$source,
    [string]$sourceId,
    [string]$status,
    [bool]$userVerified,
    [string]$notes
) {
    $x = [math]::Round(($longitude - $origin.longitude) * 107500.0, 2)
    $z = [math]::Round(($latitude - $origin.latitude) * 110650.0, 2)
    return [ordered]@{
        id = $id
        displayName = $displayName
        category = $category
        latitude = $latitude
        longitude = $longitude
        localXMetres = $x
        localZMetres = $z
        source = $source
        sourceId = $sourceId
        verificationStatus = $status
        userVerified = $userVerified
        notes = $notes
    }
}

$anchors = [System.Collections.Generic.List[object]]::new()
$osmAnchorSpecs = @(
    @{ id="berekua"; name="Berekua"; category="settlement"; osm="node/242414515"; note="OSM town node; Grand Bay is listed as an alternate name." },
    @{ id="grand_bay_community_centre"; name="Grand Bay Community Centre"; category="community"; osm="node/2178498248"; note="OSM amenity node; current use and entrance still need local confirmation." },
    @{ id="pierre_charles_secondary_school"; name="Pierre Charles Secondary School"; category="school"; osm="way/198518720"; note="OSM footprint centre; institution also confirmed by Government of Dominica sources." },
    @{ id="grand_bay_primary_school"; name="Grand Bay Primary School"; category="school"; osm="way/371414662"; note="OSM footprint centre; institution also confirmed by Government of Dominica sources." },
    @{ id="grand_bay_catholic_church"; name="Grand Bay Catholic Church"; category="church"; osm="way/392195638"; note="OSM footprint centre; model identity should remain fictionalized unless explicitly approved." }
)
foreach ($spec in $osmAnchorSpecs) {
    $point = Find-NamedPoint $spec.name
    if ($null -ne $point) {
        $anchors.Add((New-Anchor $spec.id $spec.name $spec.category $point.lat $point.lon "OpenStreetMap via Overpass API, extracted 2026-08-20" $spec.osm "osm_mapped_local_confirmation_pending" $false $spec.note))
    }
}

$anchors.Add((New-Anchor "grand_bay_credit_union" "Grand Bay Cooperative Credit Union" "finance" 15.2407793 -61.3166228 "Institution website plus prior place-coordinate cross-check" "candidate/GB-005" "official_institution_candidate_coordinate" $false "Strong Lalay/Main Road anchor; entrance and exact footprint need local confirmation."))
$anchors.Add((New-Anchor "farmers_cooperative" "Grand Bay Farmers and Food Producers Cooperative" "agriculture" 15.2403758 -61.3162141 "Prior place-coordinate research" "candidate/GB-012" "candidate_coordinate" $false "Useful farming-story anchor if current operation is locally confirmed."))
$anchors.Add((New-Anchor "grand_bay_police_station" "Grand Bay Police Station" "emergency" 15.2447591 -61.3223997 "Government directory name plus prior place-coordinate cross-check" "candidate/GB-006" "official_institution_candidate_coordinate" $false "Current entrance and footprint need local confirmation; in-game identity should remain fictionalized."))

$anchorDocument = [ordered]@{
    schemaVersion = 1
    generatedAt = (Get-Date).ToUniversalTime().ToString("o")
    phase = "Grand Bay phase 1: Lalay to beach with future Highland planting connection"
    coordinateSystem = "WGS84 source; local equirectangular metre approximation for preview only"
    unityConvention = "One Unity unit equals one metre; positive X east; positive Z north"
    origin = $origin
    sourceBounds = $bounds
    requiredLocalPins = @(
        "Resident-defined north and south limits of Lalay",
        "Highland road turnoff and first planting clearing",
        "Jetty/boat landing used by the story",
        "Grand Bay Village Council current office entrance"
    )
    anchors = $anchors
}
$anchorDocument | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $anchorPath -Encoding utf8

$width = 1600
$height = 1000
$margin = 72
$mapLeft = $margin
$mapTop = 110
$mapRight = $width - $margin
$mapBottom = $height - 90
$mapWidth = $mapRight - $mapLeft
$mapHeight = $mapBottom - $mapTop

function To-Pixel([double]$lat, [double]$lon) {
    $x = $mapLeft + (($lon - $bounds.west) / ($bounds.east - $bounds.west)) * $mapWidth
    $y = $mapBottom - (($lat - $bounds.south) / ($bounds.north - $bounds.south)) * $mapHeight
    return [System.Drawing.PointF]::new([single]$x, [single]$y)
}

$bitmap = [System.Drawing.Bitmap]::new($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::FromArgb(19, 47, 61))

$landBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(61, 98, 61))
$landRect = [System.Drawing.RectangleF]::new($mapLeft, $mapTop, $mapWidth, $mapHeight)
$graphics.FillRectangle($landBrush, $landRect)

$coast = $elements | Where-Object { $_.tags.natural -eq "coastline" -and $null -ne $_.geometry } | Select-Object -First 1
if ($null -ne $coast) {
    $coastPoints = @($coast.geometry | Where-Object {
        $_.lat -ge $bounds.south -and $_.lat -le $bounds.north -and $_.lon -ge $bounds.west -and $_.lon -le $bounds.east
    } | ForEach-Object { To-Pixel $_.lat $_.lon })
    if ($coastPoints.Count -ge 2) {
        $seaPolygon = [System.Collections.Generic.List[System.Drawing.PointF]]::new()
        foreach ($p in $coastPoints) { $seaPolygon.Add($p) }
        $seaPolygon.Add([System.Drawing.PointF]::new($mapRight, $mapBottom))
        $seaPolygon.Add([System.Drawing.PointF]::new($mapLeft, $mapBottom))
        $graphics.FillPolygon([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(27, 92, 121)), [System.Drawing.PointF[]]$seaPolygon.ToArray())
        $graphics.DrawLines([System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(151, 220, 223), 5), [System.Drawing.PointF[]]$coastPoints)
    }
}

foreach ($element in $elements | Where-Object { $null -ne $_.tags.waterway -and $null -ne $_.geometry }) {
    $points = @($element.geometry | Where-Object {
        $_.lat -ge $bounds.south -and $_.lat -le $bounds.north -and $_.lon -ge $bounds.west -and $_.lon -le $bounds.east
    } | ForEach-Object { To-Pixel $_.lat $_.lon })
    if ($points.Count -ge 2) {
        $graphics.DrawLines([System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(76, 174, 214), 3), [System.Drawing.PointF[]]$points)
    }
}

$roadOrder = @("track", "path", "service", "residential", "unclassified", "tertiary", "secondary")
foreach ($roadClass in $roadOrder) {
    foreach ($element in $elements | Where-Object { $_.tags.highway -eq $roadClass -and $null -ne $_.geometry }) {
        $points = @($element.geometry | Where-Object {
            $_.lat -ge $bounds.south -and $_.lat -le $bounds.north -and $_.lon -ge $bounds.west -and $_.lon -le $bounds.east
        } | ForEach-Object { To-Pixel $_.lat $_.lon })
        if ($points.Count -lt 2) { continue }
        $outerWidth = switch ($roadClass) { "secondary" { 11 } "tertiary" { 9 } "unclassified" { 6 } "residential" { 5 } default { 3 } }
        $innerWidth = [math]::Max(2, $outerWidth - 4)
        $outerColor = if ($roadClass -in @("track", "path")) { [System.Drawing.Color]::FromArgb(86, 58, 36) } else { [System.Drawing.Color]::FromArgb(40, 46, 44) }
        $innerColor = if ($roadClass -in @("track", "path")) { [System.Drawing.Color]::FromArgb(183, 126, 68) } elseif ($roadClass -in @("secondary", "tertiary")) { [System.Drawing.Color]::FromArgb(232, 207, 132) } else { [System.Drawing.Color]::FromArgb(198, 197, 182) }
        $graphics.DrawLines([System.Drawing.Pen]::new($outerColor, $outerWidth), [System.Drawing.PointF[]]$points)
        $graphics.DrawLines([System.Drawing.Pen]::new($innerColor, $innerWidth), [System.Drawing.PointF[]]$points)
    }
}

$labelFont = [System.Drawing.Font]::new("Segoe UI", 17, [System.Drawing.FontStyle]::Bold)
$smallFont = [System.Drawing.Font]::new("Segoe UI", 13)
$titleFont = [System.Drawing.Font]::new("Segoe UI", 25, [System.Drawing.FontStyle]::Bold)
$whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$mutedBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(220, 230, 220))
$graphics.DrawString("UP IZ UP MINI — GRAND BAY MAP TRUTH PREVIEW", $titleFont, $whiteBrush, 72, 28)
$graphics.DrawString("Lalay → beach/jetty corridor | Highland is the first planting district | no gameplay scene changed", $smallFont, $mutedBrush, 74, 70)

$labelSpecs = @(
    @{ id="grand_bay_credit_union"; short="LALAY / CREDIT UNION"; dx=14; dy=-32 },
    @{ id="farmers_cooperative"; short="FARMERS CO-OP"; dx=14; dy=8 },
    @{ id="grand_bay_police_station"; short="POLICE"; dx=14; dy=-28 },
    @{ id="grand_bay_primary_school"; short="PRIMARY SCHOOL"; dx=14; dy=-30 },
    @{ id="pierre_charles_secondary_school"; short="SECONDARY SCHOOL"; dx=14; dy=8 },
    @{ id="grand_bay_community_centre"; short="COMMUNITY CENTRE"; dx=14; dy=8 }
)
foreach ($spec in $labelSpecs) {
    $anchor = $anchors | Where-Object { $_.id -eq $spec.id } | Select-Object -First 1
    if ($null -eq $anchor) { continue }
    $p = To-Pixel $anchor.latitude $anchor.longitude
    $graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(244, 114, 58)), $p.X - 7, $p.Y - 7, 14, 14)
    $graphics.DrawEllipse([System.Drawing.Pen]::new([System.Drawing.Color]::White, 2), $p.X - 7, $p.Y - 7, 14, 14)
    $graphics.DrawString($spec.short, $smallFont, $whiteBrush, $p.X + $spec.dx, $p.Y + $spec.dy)
}

$calloutBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(218, 125, 49))
$calloutText = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(28, 28, 25))
$graphics.FillRectangle($calloutBrush, 90, 760, 430, 82)
$graphics.DrawString("HIGHLAND PLANTING ROUTE", $labelFont, $calloutText, 108, 773)
$graphics.DrawString("Exact local turnoff pin still needed", $smallFont, $calloutText, 109, 807)

$graphics.FillRectangle($calloutBrush, 1080, 760, 400, 82)
$graphics.DrawString("BEACH / JETTY ZONE", $labelFont, $calloutText, 1100, 773)
$graphics.DrawString("Exact story landing pin still needed", $smallFont, $calloutText, 1101, 807)

$northPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 5)
$graphics.DrawLine($northPen, 1500, 190, 1500, 135)
$graphics.DrawLine($northPen, 1500, 135, 1487, 155)
$graphics.DrawLine($northPen, 1500, 135, 1513, 155)
$graphics.DrawString("N", $labelFont, $whiteBrush, 1489, 100)

$metresPerLon = 107500.0
$scalePixels = (250.0 / (($bounds.east - $bounds.west) * $metresPerLon)) * $mapWidth
$graphics.DrawLine([System.Drawing.Pen]::new([System.Drawing.Color]::White, 5), 90, 900, 90 + $scalePixels, 900)
$graphics.DrawString("250 m", $smallFont, $whiteBrush, 90, 908)
$graphics.DrawString("Map data from OpenStreetMap (ODbL) — preview only; orange pins need local confirmation", $smallFont, $mutedBrush, 470, 944)

$bitmap.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()

$summary = [ordered]@{
    generatedAt = (Get-Date).ToUniversalTime().ToString("o")
    sourceTimestamp = $osm.osm3s.timestamp_osm_base
    bounds = $bounds
    origin = $origin
    elementCount = $elements.Count
    highwayWays = @($elements | Where-Object { $null -ne $_.tags.highway }).Count
    waterwayWays = @($elements | Where-Object { $null -ne $_.tags.waterway }).Count
    coastlineWays = @($elements | Where-Object { $_.tags.natural -eq "coastline" }).Count
    namedElements = @($elements | Where-Object { $null -ne $_.tags.name }).Count
    anchorCount = $anchors.Count
    preview = $previewPath
    rawSnapshot = $rawPath
}
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summaryPath -Encoding utf8
$summary | ConvertTo-Json -Depth 10
