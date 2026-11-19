param(
    [string]$UnityPath,
    [string]$TestFilter,
    [ValidateSet("EditMode", "PlayMode")]
    [string]$TestPlatform = "EditMode"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $UnityPath) {
    if ($env:UNITY_EDITOR_PATH) {
        $UnityPath = $env:UNITY_EDITOR_PATH
    }
    else {
        $versionFile = Join-Path $repoRoot "ProjectSettings\ProjectVersion.txt"
        $versionLine = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
        if (-not $versionLine) {
            Write-Error "Could not read m_EditorVersion from $versionFile."
            exit 1
        }
        $editorVersion = $versionLine.Matches[0].Groups[1].Value.Trim()
        $candidate = "C:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
        if (Test-Path -LiteralPath $candidate) {
            $UnityPath = $candidate
        }
        else {
            Write-Error "Unity $editorVersion not found at '$candidate'. Pass -UnityPath or set UNITY_EDITOR_PATH."
            exit 1
        }
    }
}

$logsDir = Join-Path $repoRoot "Logs"
if (-not (Test-Path -LiteralPath $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

$resultsPath = Join-Path $logsDir "TestResults-$TestPlatform.xml"
$logPath = Join-Path $logsDir "unity-tests-$TestPlatform.log"
if (Test-Path -LiteralPath $resultsPath) {
    Remove-Item -LiteralPath $resultsPath -Force
}

$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $repoRoot,
    "-runTests",
    "-testPlatform", $TestPlatform,
    "-testResults", $resultsPath,
    "-logFile", $logPath
)
if ($TestFilter) {
    $unityArgs += @("-testFilter", $TestFilter)
}

Write-Host "==> Running Unity $TestPlatform tests via $UnityPath" -ForegroundColor Cyan
$proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru

if (-not (Test-Path -LiteralPath $resultsPath)) {
    Write-Host "Unity exited with code $($proc.ExitCode) and produced no test results XML." -ForegroundColor Red
    Write-Host "Check log: $logPath" -ForegroundColor Yellow
    Write-Host "Tip: Close the Unity Editor if this project is currently open." -ForegroundColor Yellow
    exit 1
}

[xml]$xml = Get-Content -LiteralPath $resultsPath
$run = $xml.'test-run'
$total = [int]$run.total
$passed = [int]$run.passed
$failed = [int]$run.failed
$skipped = [int]$run.skipped

Write-Host "Unity $TestPlatform Results: Total=$total Passed=$passed Failed=$failed Skipped=$skipped" -ForegroundColor Cyan

if ($failed -gt 0 -or $proc.ExitCode -ne 0) {
    $failedCases = $xml.SelectNodes("//test-case[@result='Failed']")
    foreach ($tc in $failedCases) {
        $msg = $tc.failure.message.'#cdata-section'
        if (-not $msg) { $msg = $tc.failure.message }
        Write-Host "  FAIL: $($tc.fullname)" -ForegroundColor Red
        if ($msg) { Write-Host "        $msg" -ForegroundColor DarkRed }
    }
    exit 1
}

Write-Host "Unity $TestPlatform tests PASSED." -ForegroundColor Green
exit 0
