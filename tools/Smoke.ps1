param([int]$Seconds = 240, [switch]$Visible, [int]$Width = 1600, [int]$Height = 900)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$player = Join-Path $projectRoot 'Builds/Windows/Squares.exe'
$outputDirectory = Join-Path $projectRoot 'Logs/Smoke'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$result = Join-Path $outputDirectory 'result.json'
$log = Join-Path $outputDirectory 'player.log'
$capture = Join-Path $outputDirectory 'gameplay.png'
if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
$playerArguments = @('-squares-smoke', '-smoke-seconds', $Seconds, '-smoke-result', ('"' + $result + '"'), '-logFile', ('"' + $log + '"'), '-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0')
# A hidden Windows swapchain can produce black screenshots. Capture only in an explicitly
# requested visible preview; a hidden run validates gameplay and logs, not pixels.
$windowStyle = 'Hidden'
if ($Visible) { $windowStyle = 'Normal'; $playerArguments += @('-capture', ('"' + $capture + '"')) }
$playerProcess = Start-Process -FilePath $player -ArgumentList $playerArguments -PassThru -WindowStyle $windowStyle
Write-Output "Smoke PID $($playerProcess.Id); log: $log"
if (-not $playerProcess.WaitForExit(180000)) { throw 'Player smoke timed out; inspect process and player.log' }
if (-not (Test-Path -LiteralPath $result)) { Get-Content -LiteralPath $log -Tail 50; throw 'No player report' }
$report = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
$report | ConvertTo-Json
if (-not $report.passed -or $playerProcess.ExitCode -ne 0) { throw 'Player smoke failed' }
if ($Visible) { Write-Output "Screenshots: $outputDirectory" }
