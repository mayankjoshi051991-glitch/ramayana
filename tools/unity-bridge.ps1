# Sends a command to the open Unity Editor via the AgentBridge and waits for the result.
# Usage: .\tools\unity-bridge.ps1 "menu:Ramayana/Build Test Level"
param(
    [Parameter(Mandatory)] [string] $Command,
    [int] $TimeoutSec = 600
)

$bridge = Join-Path $PSScriptRoot "..\RamayanaGame\Temp\AgentBridge"
if (-not (Test-Path "$bridge\ready.txt")) { throw "AgentBridge not loaded. Open the project in Unity first." }

$console = "$bridge\console.log"
$startLine = if (Test-Path $console) { (Get-Content $console).Count } else { 0 }
Remove-Item "$bridge\result.txt" -ErrorAction SilentlyContinue
Set-Content "$bridge\command.txt" $Command

$sw = [Diagnostics.Stopwatch]::StartNew()
while (-not (Test-Path "$bridge\result.txt")) {
    if ($sw.Elapsed.TotalSeconds -gt $TimeoutSec) { throw "Timed out after $TimeoutSec s waiting for Unity." }
    Start-Sleep -Milliseconds 500
}
Get-Content "$bridge\result.txt"
"--- console ---"
if (Test-Path $console) { Get-Content $console | Select-Object -Skip $startLine }
