# Generates Assets\app.ico and build\splash.png (run once; outputs are committed).
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root 'src\DAXLogin\Assets'
$build = Join-Path $root 'build'
New-Item -ItemType Directory -Force $assets, $build | Out-Null

function New-RoundRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function Draw-Logo([System.Drawing.Graphics]$g, [float]$ox, [float]$oy, [float]$s) {
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $blue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(37, 99, 235))
    $bg = New-RoundRect $ox $oy $s $s ($s * 0.22)
    $g.FillPath($blue, $bg)
    $white = [System.Drawing.Color]::White
    $pen = New-Object System.Drawing.Pen $white, ($s * 0.085)
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'; $pen.LineJoin = 'Round'
    # key: ring at top-left, shaft to bottom-right, two teeth
    $g.DrawEllipse($pen, $ox + $s * 0.20, $oy + $s * 0.20, $s * 0.30, $s * 0.30)
    $g.DrawLine($pen, $ox + $s * 0.46, $oy + $s * 0.46, $ox + $s * 0.78, $oy + $s * 0.78)
    $g.DrawLine($pen, $ox + $s * 0.66, $oy + $s * 0.66, $ox + $s * 0.76, $oy + $s * 0.56)
    $g.DrawLine($pen, $ox + $s * 0.76, $oy + $s * 0.76, $ox + $s * 0.84, $oy + $s * 0.68)
}

# --- icon ---
$sizes = 16, 24, 32, 48, 64, 256
$pngs = foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $sz, $sz
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    Draw-Logo $g 0 0 $sz
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    , $ms.ToArray()
}
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]
    $dim = if ($sz -ge 256) { 0 } else { $sz }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assets 'app.ico'), $out.ToArray())

# --- installer splash ---
$w = 520; $h = 300
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(245, 246, 248))
$g.TextRenderingHint = 'AntiAliasGridFit'
Draw-Logo $g (($w - 88) / 2) 56 88
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = 'Center'
$title = New-Object System.Drawing.Font 'Segoe UI Semibold', 22
$sub = New-Object System.Drawing.Font 'Segoe UI', 11
$dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(27, 29, 33))
$gray = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(107, 114, 128))
$g.DrawString('DAXLogin', $title, $dark, (New-Object System.Drawing.RectangleF 0, 160, $w, 44), $fmt)
$g.DrawString('One-click sign-in for Dynamics AX', $sub, $gray, (New-Object System.Drawing.RectangleF 0, 206, $w, 28), $fmt)
$g.Dispose()
$bmp.Save((Join-Path $build 'splash.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host 'Assets generated.'
