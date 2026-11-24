param(
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$packagesRoot = Join-Path $repoRoot "Packages"
$errors = New-Object System.Collections.Generic.List[string]
$guidOwners = New-Object 'System.Collections.Generic.Dictionary[string, string]' ([System.StringComparer]::OrdinalIgnoreCase)
$guidRegex = [regex]'(?m)^guid:\s*([0-9a-f]{32})\s*$'

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
$packageDirs = @(Get-ChildItem -Path $packagesRoot -Directory -Filter "com.contentcontent.*")

if ($packageDirs.Count -eq 0) {
    $errors.Add("No com.contentcontent.* packages found under $packagesRoot.")
}

foreach ($pkg in $packageDirs) {
    $pkgRel = $pkg.Name

    # 1. Check .meta pairing (missing .meta, orphan .meta, and GUID format/uniqueness)
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
                continue
            }

            $metaText = Get-Content -LiteralPath $item.FullName -Raw
            $guidMatch = $guidRegex.Match($metaText)
            if (-not $guidMatch.Success) {
                $errors.Add("Invalid or missing 32-hex lowercase GUID in .meta: Packages/$pkgRel/$rel")
            }
            else {
                $guid = $guidMatch.Groups[1].Value
                if ($guidOwners.ContainsKey($guid)) {
                    $errors.Add("Duplicate GUID '$guid' shared by '$($guidOwners[$guid])' and 'Packages/$pkgRel/$rel'")
                }
                else {
                    $guidOwners[$guid] = "Packages/$pkgRel/$rel"
                }
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

        $unityPrefix = (Join-Path $runtimeDir "Unity") + [System.IO.Path]::DirectorySeparatorChar
        $csFiles = Get-ChildItem -Path $runtimeDir -Recurse -File -Filter "*.cs"
        foreach ($cs in $csFiles) {
            if ($cs.FullName.StartsWith($unityPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
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

# 2b. Validate top-level Samples/samples-catalog.json and scene script GUID references
$samplesRoot = Join-Path $repoRoot "Samples"
$catalogPath = Join-Path $samplesRoot "samples-catalog.json"
$packagesWithSamples = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

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

            $pkgName = [string]$sample.packageName
            if ([string]::IsNullOrWhiteSpace($pkgName) -or -not (Test-Path -LiteralPath (Join-Path $packagesRoot $pkgName))) {
                $errors.Add("Sample '$sid' references unknown packageName: '$pkgName'")
            }
            else {
                $null = $packagesWithSamples.Add($pkgName)
            }

            $srcRel = ([string]$sample.sourcePath) -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $srcDir = Join-Path $samplesRoot $srcRel
            if (-not (Test-Path -LiteralPath $srcDir)) {
                $errors.Add("Sample source directory missing for '$sid': Samples/$($sample.sourcePath)")
            }
            else {
                $scenePath = $null
                $entryScene = [string]$sample.entryScene
                if ([string]::IsNullOrWhiteSpace($entryScene)) {
                    $errors.Add("Sample '$sid' in Samples/samples-catalog.json must define entryScene.")
                }
                else {
                    $scenePath = Join-Path $srcDir ($entryScene -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                    if (-not (Test-Path -LiteralPath $scenePath)) {
                        $errors.Add("Missing entryScene for '$sid': Samples/$($sample.sourcePath)/$entryScene")
                    }
                }

                $sampleItems = @(Get-ChildItem -LiteralPath $srcDir -Recurse -Force)
                if ($sampleItems.Count -eq 0) {
                    $errors.Add("Sample source directory is empty for '$sid': Samples/$($sample.sourcePath)")
                }

                $sampleScriptGuids = New-Object System.Collections.Generic.List[string]
                foreach ($sItem in $sampleItems) {
                    if ($sItem.Name -eq ".DS_Store" -or $sItem.Name -eq "Thumbs.db") {
                        continue
                    }
                    $sRel = $sItem.FullName.Substring($srcDir.Length).TrimStart('\', '/')
                    if ($sItem.Extension -eq ".meta") {
                        $sTarget = $sItem.FullName.Substring(0, $sItem.FullName.Length - 5)
                        if (-not (Test-Path -LiteralPath $sTarget)) {
                            $errors.Add("Orphan .meta file in sample '$sid': Samples/$($sample.sourcePath)/$sRel")
                            continue
                        }
                        $sMetaText = Get-Content -LiteralPath $sItem.FullName -Raw
                        $sGuidMatch = $guidRegex.Match($sMetaText)
                        if (-not $sGuidMatch.Success) {
                            $errors.Add("Invalid GUID in sample .meta: Samples/$($sample.sourcePath)/$sRel")
                        }
                        elseif ($sTarget.EndsWith(".cs", [System.StringComparison]::OrdinalIgnoreCase)) {
                            $sampleScriptGuids.Add($sGuidMatch.Groups[1].Value)
                        }
                    }
                    else {
                        $sMeta = "$($sItem.FullName).meta"
                        if (-not (Test-Path -LiteralPath $sMeta)) {
                            $errors.Add("Missing .meta file in sample '$sid': Samples/$($sample.sourcePath)/$sRel")
                        }
                    }
                }

                if ($scenePath -and (Test-Path -LiteralPath $scenePath) -and $sampleScriptGuids.Count -gt 0) {
                    $sceneText = Get-Content -LiteralPath $scenePath -Raw
                    $referencesSampleScript = $false
                    foreach ($sg in $sampleScriptGuids) {
                        if ($sceneText.Contains($sg)) {
                            $referencesSampleScript = $true
                            break
                        }
                    }
                    if (-not $referencesSampleScript) {
                        $errors.Add("Sample entryScene '$entryScene' for '$sid' does not reference any sample script GUID.")
                    }
                }
            }
        }

        foreach ($pkg in $packageDirs) {
            if (-not $packagesWithSamples.Contains($pkg.Name)) {
                $errors.Add("Package '$($pkg.Name)' has no sample entry in Samples/samples-catalog.json.")
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
Write-Host "Verification PASSED ($($packageDirs.Count) packages checked, $($guidOwners.Count) unique GUIDs verified)." -ForegroundColor Green
exit 0
