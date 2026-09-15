$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$d=Get-Content "$PSScriptRoot/GENEVA-SOURCE.json" -Raw | ConvertFrom-Json
$b=New-Object Drawing.Bitmap(1500,1000)
$g=[Drawing.Graphics]::FromImage($b); $g.SmoothingMode='AntiAlias'; $g.Clear([Drawing.Color]::FromArgb(239,241,229))
function Pt([double]$lon,[double]$lat) { [Drawing.PointF]::new([single](110+(($lon+61.3181049)*107500/3+75)*3.15),[single](820-(($lat-15.2450638)*110650/3+100)*3.15)) }
$f=New-Object Drawing.Font('Arial',16); $s=New-Object Drawing.Font('Arial',12); $t=New-Object Drawing.Font('Arial',25,[Drawing.FontStyle]::Bold); $ink=[Drawing.Brushes]::DarkSlateGray
$g.DrawString('GENEVA / PCSS - ROAD CONNECTION PLAN',$t,$ink,40,25)
$g.DrawString('Research preview of the separate map copy - not a new Unity render',$f,$ink,40,72)
$g.SetClip([Drawing.Rectangle]::new(20,110,1460,775))
foreach($item in $d.features) {
 $pts=[Drawing.PointF[]]@($item.geometry | ForEach-Object { Pt $_.lon $_.lat })
 if($item.id -eq 142337516) { $g.FillPolygon([Drawing.Brushes]::DarkSeaGreen,$pts); $g.DrawPolygon([Drawing.Pens]::ForestGreen,$pts) }
 elseif($item.id -ne 440104499) { $pen=New-Object Drawing.Pen([Drawing.Color]::FromArgb(78,85,89),13); if($item.id -ne 22917921) {$pen.Color=[Drawing.Color]::FromArgb(38,114,151)}; $g.DrawLines($pen,$pts); $pen.Dispose() }
}
$g.ResetClip()
$c=Pt -61.3103161 15.2432575
$g.FillEllipse([Drawing.Brushes]::SteelBlue,($c.X-28),($c.Y-28),56,56); $g.FillEllipse([Drawing.Brushes]::DarkSeaGreen,($c.X-12),($c.Y-12),24,24)
$p=Pt (-61.3181049+319.58/107500) (15.2450638+147.89/110650)
$g.FillRectangle([Drawing.Brushes]::Tan,($p.X-63),($p.Y-41),126,82)
$g.FillRectangle([Drawing.Brushes]::SlateGray,($p.X-57),($p.Y-33),25,70); $g.FillRectangle([Drawing.Brushes]::SlateGray,($p.X+32),($p.Y-33),25,70); $g.FillRectangle([Drawing.Brushes]::SlateGray,($p.X-40),($p.Y+19),80,21)
$g.DrawString('PCSS',$f,$ink,($p.X-26),($p.Y+45)); $g.DrawString('Courtyard opens toward main road',$s,$ink,($p.X-140),($p.Y+75))
$q=Pt -61.31048 15.24661
$g.DrawString('GENEVA',$f,$ink,($q.X-50),($q.Y-12)); $g.DrawString('Playing Field',$s,$ink,($q.X-52),($q.Y+15))
$g.DrawString('ROUNDABOUT',$f,$ink,($c.X-170),($c.Y+38)); $g.DrawString('Bay road',$f,$ink,665,815)
$g.DrawString('Coastal stretch, then left',$s,$ink,920,480); $g.DrawString('toward the school road',$s,$ink,920,502)
$g.DrawString('NORTH ^',$f,$ink,1340,130)
$g.DrawString('Blue: loop to restore | Grey: existing bay road | Green: mapped sports-centre boundary',$f,$ink,40,903)
$g.DrawString('OpenStreetMap contributors (ODbL), retained MINI-094 snapshot. North up; 1/3 game compression.',$s,$ink,40,940)
$g.DrawString('Roundabout enlarged for gameplay. Campus massing and proposed sports details are approximate.',$s,$ink,40,967)
$b.Save("$PSScriptRoot/Evidence/05-Geneva-Connection-Plan.png",[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $b.Dispose()

