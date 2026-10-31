param([string]$Unity = 'C:/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$buildLog = Join-Path $logDirectory 'Build.log'
$unityArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', 'Squares.Editor.BuildProject.BuildAll', '-logFile', ('"' + $buildLog + '"'))
$unityProcess = Start-Process -FilePath $Unity -ArgumentList $unityArguments -PassThru -WindowStyle Hidden
Write-Output "Unity build PID $($unityProcess.Id); log: $buildLog"
$unityProcess.WaitForExit()
if ($unityProcess.ExitCode -ne 0) {
    Get-Content -LiteralPath $buildLog -Tail 60
    throw "Unity exited with code $($unityProcess.ExitCode)"
}
if (-not (Select-String -LiteralPath $buildLog -SimpleMatch 'SQUARES_BUILD_PASS')) { throw 'Build success marker missing' }
Select-String -LiteralPath $buildLog -Pattern 'SQUARES_(CHECKS|BOT|BUILD)' | ForEach-Object { $_.Line }
Write-Output (Join-Path $projectRoot 'Builds/Windows/Squares.exe')
