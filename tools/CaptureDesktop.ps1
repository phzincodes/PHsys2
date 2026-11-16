param([Parameter(Mandatory)][int]$TargetProcessId, [string]$Name = 'desktop-preview')
$ErrorActionPreference = 'Stop'
if ($Name -notmatch '^[a-z0-9-]+$') { throw 'Nome de captura inválido.' }
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$targetProcess = Get-Process -Id $TargetProcessId
if ($targetProcess.ProcessName -ne 'PHsystem.Desktop' -or -not $targetProcess.Path.StartsWith($workspacePath + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'A captura é limitada à janela do PHsystem neste workspace.'
}
Add-Type -AssemblyName System.Drawing
if (-not ('PHDesktopCapture' -as [type])) { Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class PHDesktopCapture {
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr handle, out Rect rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr handle, IntPtr hdc, uint flags);
}
'@ }
$bounds = New-Object PHDesktopCapture+Rect
if (-not [PHDesktopCapture]::GetWindowRect($targetProcess.MainWindowHandle, [ref]$bounds)) { throw 'Janela não encontrada.' }
$bitmap = New-Object System.Drawing.Bitmap(($bounds.Right - $bounds.Left), ($bounds.Bottom - $bounds.Top))
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$hdc = $graphics.GetHdc()
try { $captured = [PHDesktopCapture]::PrintWindow($targetProcess.MainWindowHandle, $hdc, 2) }
finally { $graphics.ReleaseHdc($hdc) }
try {
    if (-not $captured) { throw 'Captura da janela indisponível.' }
    $destination = Join-Path $workspacePath ('artifacts/' + $Name + '.png')
    [void][IO.Directory]::CreateDirectory((Split-Path $destination))
    $bitmap.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output $destination
} finally { $graphics.Dispose(); $bitmap.Dispose() }
