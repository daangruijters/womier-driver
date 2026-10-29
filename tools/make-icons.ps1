<#
.SYNOPSIS
  Generates the application icons (app.ico and app-offline.ico) used by the tray and window.
.DESCRIPTION
  Draws a simple keyboard glyph with System.Drawing at several sizes and packs them into
  PNG-compressed .ico containers. Run from the repository root:
    pwsh ./tools/make-icons.ps1
#>
param(
  [string]$OutDir = (Join-Path $PSScriptRoot "..\src\WomierDriver.App\Assets")
)
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size, [bool]$offline) {
  $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.Clear([System.Drawing.Color]::Transparent)

  $s = [float]$size
  $pad = [float]($s * 0.04)
  $r = [float]($s * 0.22)
  $rect = New-Object System.Drawing.RectangleF $pad, $pad, ($s - 2 * $pad), ($s - 2 * $pad)

  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $r * 2
  $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
  $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
  $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
  $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
  $path.CloseFigure()

  if ($offline) {
    $c1 = [System.Drawing.Color]::FromArgb(255, 112, 118, 130)
    $c2 = [System.Drawing.Color]::FromArgb(255, 78, 84, 96)
  } else {
    $c1 = [System.Drawing.Color]::FromArgb(255, 86, 140, 255)
    $c2 = [System.Drawing.Color]::FromArgb(255, 44, 84, 214)
  }
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $c1, $c2, 90
  $g.FillPath($brush, $path)

  # Key grid: 3 rows of keys + a space bar, all white with slight transparency.
  $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
  $cols = 4
  $gap = [float]($s * 0.055)
  $inner = [float]($s * 0.17)
  $gridW = $s - 2 * $inner
  $keyW = ($gridW - ($cols - 1) * $gap) / $cols
  $keyH = [float]($s * 0.13)
  $top = [float]($s * 0.2)
  $kr = [Math]::Max(1.0, $s * 0.03)
  for ($row = 0; $row -lt 3; $row++) {
    $offset = if ($row -eq 1) { $keyW * 0.5 + $gap * 0.5 } else { 0 }
    $count = if ($row -eq 1) { $cols - 1 } else { $cols }
    for ($col = 0; $col -lt $count; $col++) {
      $x = $inner + $offset + $col * ($keyW + $gap)
      $y = $top + $row * ($keyH + $gap)
      $kp = New-Object System.Drawing.Drawing2D.GraphicsPath
      $kd = $kr * 2
      $kp.AddArc($x, $y, $kd, $kd, 180, 90)
      $kp.AddArc($x + $keyW - $kd, $y, $kd, $kd, 270, 90)
      $kp.AddArc($x + $keyW - $kd, $y + $keyH - $kd, $kd, $kd, 0, 90)
      $kp.AddArc($x, $y + $keyH - $kd, $kd, $kd, 90, 90)
      $kp.CloseFigure()
      $g.FillPath($white, $kp)
    }
  }
  # Space bar
  $sy = $top + 3 * ($keyH + $gap)
  $sx = $inner + $keyW * 0.5
  $sw = $gridW - $keyW
  $sp = New-Object System.Drawing.Drawing2D.GraphicsPath
  $kd = $kr * 2
  $sp.AddArc($sx, $sy, $kd, $kd, 180, 90)
  $sp.AddArc($sx + $sw - $kd, $sy, $kd, $kd, 270, 90)
  $sp.AddArc($sx + $sw - $kd, $sy + $keyH - $kd, $kd, $kd, 0, 90)
  $sp.AddArc($sx, $sy + $keyH - $kd, $kd, $kd, 90, 90)
  $sp.CloseFigure()
  $g.FillPath($white, $sp)

  $g.Dispose()
  return $bmp
}

function Write-Ico([string]$path, [bool]$offline) {
  $sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
  $pngs = @()
  foreach ($sz in $sizes) {
    $bmp = New-IconBitmap $sz $offline
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,@{ Size = $sz; Bytes = $ms.ToArray() }
    $bmp.Dispose(); $ms.Dispose()
  }
  $fs = [System.IO.File]::Create($path)
  $bw = New-Object System.IO.BinaryWriter $fs
  $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$pngs.Count)
  $offset = 6 + 16 * $pngs.Count
  foreach ($p in $pngs) {
    $dim = if ($p.Size -ge 256) { 0 } else { $p.Size }
    $bw.Write([Byte]$dim); $bw.Write([Byte]$dim); $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$p.Bytes.Length); $bw.Write([UInt32]$offset)
    $offset += $p.Bytes.Length
  }
  foreach ($p in $pngs) { $bw.Write($p.Bytes) }
  $bw.Flush(); $bw.Dispose(); $fs.Dispose()
  Write-Host "wrote $path ($($pngs.Count) sizes)"
}

$OutDir = [System.IO.Path]::GetFullPath($OutDir)
New-Item -ItemType Directory -Force $OutDir | Out-Null
Write-Ico (Join-Path $OutDir "app.ico") $false
Write-Ico (Join-Path $OutDir "app-offline.ico") $true

# Also emit a 256px PNG preview for the README.
$preview = New-IconBitmap 256 $false
$preview.Save((Join-Path $OutDir "app-256.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
Write-Host "wrote $(Join-Path $OutDir 'app-256.png')"
