param([int]$Seconds = 240, [switch]$Visible, [switch]$Realtime, [switch]$ConstellationDemo, [switch]$Stargazer,
    [int]$Seed = 735193, [int]$Core = 0, [int]$Pattern = 0, [int]$Width = 1600, [int]$Height = 900,
    [string]$Output = 'Logs/Smoke')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$player = Join-Path $projectRoot 'Builds/Windows/Squares.exe'
$outputDirectory = Join-Path $projectRoot $Output
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$result = Join-Path $outputDirectory 'result.json'
$log = Join-Path $outputDirectory 'player.log'
$capture = Join-Path $outputDirectory 'gameplay.png'
if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
$playerArguments = @('-squares-smoke', '-smoke-seconds', $Seconds, '-smoke-result', ('"' + $result + '"'), '-logFile', ('"' + $log + '"'), '-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0')
$playerArguments += @('-review-seed', $Seed, '-review-core', $Core, '-review-pattern', $Pattern)
if ($Realtime) { $playerArguments += '-realtime-review' }
if ($ConstellationDemo) { $playerArguments += '-constellation-demo' }
if ($Stargazer) { $playerArguments += '-review-stargazer' }
# A hidden Windows swapchain can produce black screenshots. Capture only in an explicitly
# requested visible preview; a hidden run validates gameplay and logs, not pixels.
$windowStyle = 'Hidden'
if ($Visible) { $windowStyle = 'Normal'; $playerArguments += @('-capture', ('"' + $capture + '"')) }
$playerProcess = Start-Process -FilePath $player -ArgumentList $playerArguments -PassThru -WindowStyle $windowStyle
Write-Output "Smoke PID $($playerProcess.Id); log: $log"
$timeout = 180000
if ($Realtime) { $timeout += $Seconds * 1000 }
if (-not $playerProcess.WaitForExit($timeout)) { throw 'Player smoke timed out; inspect process and player.log' }
if (-not (Test-Path -LiteralPath $result)) { Get-Content -LiteralPath $log -Tail 50; throw 'No player report' }
$report = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
$report | ConvertTo-Json
if (-not $report.passed -or $playerProcess.ExitCode -ne 0) { throw 'Player smoke failed' }
if ($Visible) { Write-Output "Screenshots: $outputDirectory" }
