$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$manifest = Join-Path $PSScriptRoot 'app.manifest'
$output = Join-Path $PSScriptRoot 'ImageCompare.exe'
$source = Join-Path $PSScriptRoot 'ImageCompare.cs'
$icon = Join-Path $PSScriptRoot 'ImageCompare.ico'
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/win32manifest:$manifest" "/win32icon:$icon" "/resource:$icon,ImageCompare.ico" "/out:$output" $source
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
