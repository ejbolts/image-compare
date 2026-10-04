$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$manifest = Join-Path $PSScriptRoot 'app.manifest'
$output = Join-Path $PSScriptRoot 'ImageCompare.exe'
$source = Join-Path $PSScriptRoot 'ImageCompare.cs'
$icon = Join-Path $PSScriptRoot 'ImageCompare.ico'
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/win32manifest:$manifest" "/win32icon:$icon" "/resource:$icon,ImageCompare.ico" "/out:$output" $source
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

# Ensure pinned taskbar shortcut points to this build
$taskbarLnk = Join-Path $env:APPDATA 'Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\ImageCompare.lnk'
if (Test-Path $taskbarLnk) {
    try {
        $sh = New-Object -ComObject WScript.Shell
        $lnk = $sh.CreateShortcut($taskbarLnk)
        if ($lnk.TargetPath -ne $output) {
            $lnk.TargetPath = $output
            $lnk.WorkingDirectory = $PSScriptRoot
            $lnk.IconLocation = "$icon,0"
            $lnk.Save()
        }
    } catch { }
}

# Sync to previous output location if it exists
$legacyOutput = 'C:\Users\Ethan\Documents\Codex\2026-10-04\https-tryshotline-com-tools-before-after\outputs\ImageCompare.exe'
if (Test-Path $legacyOutput) {
    try { Copy-Item -Path $output -Destination $legacyOutput -Force } catch { }
}
