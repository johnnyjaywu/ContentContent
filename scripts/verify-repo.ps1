param(
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$packagesRoot = Join-Path $repoRoot "Packages"
$errors = New-Object System.Collections.Generic.List[string]

function Test-IsUnityIgnoredPath {
    param([string]$RelativePath)
    $segments = $RelativePath -split '[\\/]'
    foreach ($seg in $segments) {
        if ($seg.EndsWith("~") -or $seg.StartsWith(".")) {
            return $true
        }
    }
    return $false
}

Write-Host "==> Checking UPM packages in $packagesRoot" -ForegroundColor Cyan
$packageDirs = Get-ChildItem -Path $packagesRoot -Directory -Filter "com.contentcontent.*"

if ($packageDirs.Count -eq 0) {
    $errors.Add("No com.contentcontent.* packages found under $packagesRoot.")
}

foreach ($pkg in $packageDirs) {
    $pkgRel = $pkg.Name

    # 1. Check .meta pairing (missing .meta and orphan .meta)
    $items = Get-ChildItem -Path $pkg.FullName -Recurse -Force
    foreach ($item in $items) {
        $rel = $item.FullName.Substring($pkg.FullName.Length).TrimStart('\', '/')
        if (Test-IsUnityIgnoredPath $rel) {
            continue
        }

        if ($item.Extension -eq ".meta") {
            $targetPath = $item.FullName.Substring(0, $item.FullName.Length - 5)
            if (-not (Test-Path -LiteralPath $targetPath)) {
                $errors.Add("Orphan .meta file: Packages/$pkgRel/$rel")
            }
        }
        else {
            $metaPath = "$($item.FullName).meta"
            if (-not (Test-Path -LiteralPath $metaPath)) {
                $errors.Add("Missing .meta file for: Packages/$pkgRel/$rel")
            }
        }
    }

    # Ensure samples live in top-level Samples/, not embedded in Packages/
    if (Test-Path -LiteralPath (Join-Path $pkg.FullName "Samples~")) {
        $errors.Add("Packages/$pkgRel/Samples~ must not exist; store samples in top-level Samples/ instead.")
    }

    # 2. Check pure Runtime/ boundary (noEngineReferences: true & no Unity usings)
    $runtimeDir = Join-Path $pkg.FullName "Runtime"
    if (Test-Path -LiteralPath $runtimeDir) {
        $runtimeAsmdefs = Get-ChildItem -Path $runtimeDir -File -Filter "*.asmdef"
        foreach ($asmdef in $runtimeAsmdefs) {
            $json = Get-Content -LiteralPath $asmdef.FullName -Raw | ConvertFrom-Json
            if ($json.noEngineReferences -ne $true) {
                $errors.Add("Pure Runtime asmdef must set `"noEngineReferences`": true -> Packages/$pkgRel/Runtime/$($asmdef.Name)")
            }
        }

        $unityDir = Join-Path $runtimeDir "Unity"
        $csFiles = Get-ChildItem -Path $runtimeDir -Recurse -File -Filter "*.cs"
        foreach ($cs in $csFiles) {
            if ($cs.FullName.StartsWith($unityDir, [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }
            $matches = Select-String -LiteralPath $cs.FullName -Pattern '^\s*using\s+Unity(Engine|Editor)\b'
            if ($matches) {
                $rel = $cs.FullName.Substring($pkg.FullName.Length).TrimStart('\', '/')
                $errors.Add("Pure Runtime C# file references UnityEngine/UnityEditor: Packages/$pkgRel/$rel")
            }
        }
    }
}

# 2b. Validate top-level Samples/samples-catalog.json
$samplesRoot = Join-Path $repoRoot "Samples"
$catalogPath = Join-Path $samplesRoot "samples-catalog.json"
if (-not (Test-Path -LiteralPath $catalogPath)) {
    $errors.Add("Missing sample catalog: Samples/samples-catalog.json")
}
else {
    try {
        $catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
        $seenSampleIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($sample in $catalog.samples) {
            $sid = [string]$sample.sampleId
            if ([string]::IsNullOrWhiteSpace($sid) -or -not $seenSampleIds.Add($sid)) {
                $errors.Add("Invalid or duplicate sampleId in Samples/samples-catalog.json: '$sid'")
                continue
            }
            $srcRel = ([string]$sample.sourcePath) -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $srcDir = Join-Path $samplesRoot $srcRel
            if (-not (Test-Path -LiteralPath $srcDir)) {
                $errors.Add("Sample source directory missing for '$sid': Samples/$($sample.sourcePath)")
            }
            else {
                $sampleFiles = @(Get-ChildItem -LiteralPath $srcDir -Recurse -File -Force)
                if ($sampleFiles.Count -eq 0) {
                    $errors.Add("Sample source directory is empty for '$sid': Samples/$($sample.sourcePath)")
                }
            }
        }
    }
    catch {
        $errors.Add("Failed to parse Samples/samples-catalog.json: $($_.Exception.Message)")
    }
}

# 3. Run pure C# unit tests
if (-not $SkipTests) {
    Write-Host "==> Running pure C# unit tests (dotnet test)" -ForegroundColor Cyan
    $pureTestsDir = Join-Path (Join-Path $repoRoot "tools") "PureTests"
    $testProj = Join-Path $pureTestsDir "ContentContent.PureTests.csproj"
    $assetsJson = Join-Path (Join-Path $pureTestsDir "obj") "project.assets.json"
    $dotnetArgs = @("test", $testProj, "--nologo", "--verbosity", "minimal")
    if (Test-Path -LiteralPath $assetsJson) {
        $dotnetArgs += "--no-restore"
    }

    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) {
        $errors.Add("dotnet test failed with exit code $LASTEXITCODE.")
    }
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Verification FAILED with $($errors.Count) issue(s):" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "Verification PASSED ($($packageDirs.Count) packages checked)." -ForegroundColor Green
exit 0
