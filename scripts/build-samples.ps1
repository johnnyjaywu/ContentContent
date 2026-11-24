<#
.SYNOPSIS
    Packages top-level Samples/ folders into individual cross-platform .zip archives,
    generates samples-manifest.json with SHA-256 checksums for GitHub Releases,
    and self-verifies archive contents and checksums.
#>
param(
    [string]$OutputDir,
    [string]$ReleaseTag,
    [string]$Repository
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path -Parent $PSScriptRoot
$samplesRoot = Join-Path $repoRoot "Samples"
$catalogPath = Join-Path $samplesRoot "samples-catalog.json"

if (-not (Test-Path -LiteralPath $catalogPath)) {
    throw "Sample catalog not found at: $catalogPath"
}

$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json

if (-not $OutputDir) {
    $OutputDir = Join-Path (Join-Path $repoRoot "Build") "Samples"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = Join-Path $repoRoot $OutputDir
}

if (-not $ReleaseTag) {
    $ReleaseTag = if ($catalog.defaultReleaseTag) { [string]$catalog.defaultReleaseTag } else { "samples-latest" }
}

if (-not $Repository) {
    $Repository = if ($catalog.repository) { [string]$catalog.repository } else { "johnnyjaywu/ContentContent" }
}

if (Test-Path -LiteralPath $OutputDir) {
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$seenIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$manifestSamples = New-Object System.Collections.Generic.List[object]
$totalBytes = [long]0
$sha256 = [System.Security.Cryptography.SHA256]::Create()

try {
    foreach ($entry in $catalog.samples) {
        $sampleId = [string]$entry.sampleId
        if ([string]::IsNullOrWhiteSpace($sampleId)) {
            throw "Catalog contains an entry with an empty sampleId."
        }
        if (-not $seenIds.Add($sampleId)) {
            throw "Duplicate sampleId in catalog: $sampleId"
        }

        $sourceRel = ([string]$entry.sourcePath) -replace '/', [System.IO.Path]::DirectorySeparatorChar
        $sourceDir = Join-Path $samplesRoot $sourceRel
        if (-not (Test-Path -LiteralPath $sourceDir)) {
            throw "Sample source directory missing for '$sampleId': $sourceDir"
        }

        $entryScene = [string]$entry.entryScene
        if ([string]::IsNullOrWhiteSpace($entryScene)) {
            throw "Catalog entry '$sampleId' must specify entryScene."
        }
        $entryScenePath = Join-Path $sourceDir ($entryScene -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $entryScenePath)) {
            throw "Entry scene missing for '$sampleId': $entryScenePath"
        }

        $files = @(Get-ChildItem -LiteralPath $sourceDir -Recurse -File -Force | Where-Object {
            $_.Name -ne ".DS_Store" -and $_.Name -ne "Thumbs.db" -and $_.Name -ne ".contentcontent-sample.json"
        })
        if ($files.Count -eq 0) {
            throw "Sample source directory is empty for '$sampleId': $sourceDir"
        }

        $archiveFileName = "$sampleId.zip"
        $archivePath = Join-Path $OutputDir $archiveFileName

        $zip = [System.IO.Compression.ZipFile]::Open($archivePath, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            $sourcePrefix = $sourceDir.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
            foreach ($file in $files) {
                $relPath = $file.FullName.Substring($sourcePrefix.Length) -replace '\\', '/'
                $zipEntry = $zip.CreateEntry($relPath, [System.IO.Compression.CompressionLevel]::Optimal)
                $entryStream = $zipEntry.Open()
                try {
                    $fileStream = [System.IO.File]::OpenRead($file.FullName)
                    try {
                        $fileStream.CopyTo($entryStream)
                    }
                    finally {
                        $fileStream.Dispose()
                    }
                }
                finally {
                    $entryStream.Dispose()
                }
            }
        }
        finally {
            $zip.Dispose()
        }

        $zipBytes = [System.IO.File]::ReadAllBytes($archivePath)
        $hashBytes = $sha256.ComputeHash($zipBytes)
        $hashHex = ([System.BitConverter]::ToString($hashBytes)).Replace("-", "").ToLowerInvariant()
        $sizeBytes = [long]$zipBytes.Length
        $totalBytes += $sizeBytes

        $downloadUrl = "https://github.com/$Repository/releases/download/$ReleaseTag/$archiveFileName"

        $manifestSamples.Add([ordered]@{
            packageName     = [string]$entry.packageName
            sampleId        = $sampleId
            displayName     = [string]$entry.displayName
            description     = [string]$entry.description
            version         = [string]$entry.version
            installSubPath  = [string]$entry.installSubPath
            entryScene      = $entryScene
            archiveFileName = $archiveFileName
            downloadUrl     = $downloadUrl
            sha256          = $hashHex
            sizeBytes       = $sizeBytes
        })

        Write-Host ("  Built {0,-28} ({1,6} bytes, sha256={2}...)" -f $archiveFileName, $sizeBytes, $hashHex.Substring(0, 12))
    }
}
finally {
    $sha256.Dispose()
}

$manifestObj = [ordered]@{
    schemaVersion  = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", [System.Globalization.CultureInfo]::InvariantCulture)
    releaseTag     = $ReleaseTag
    samples        = $manifestSamples.ToArray()
}

$manifestPath = Join-Path $OutputDir "samples-manifest.json"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($manifestPath, (($manifestObj | ConvertTo-Json -Depth 6) + "`n"), $utf8NoBom)

# Self-verify generated manifest and every built .zip archive
$parsedManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($null -eq $parsedManifest.samples -or $parsedManifest.samples.Count -ne $manifestSamples.Count) {
    throw "Manifest verification failed: sample count mismatch in $manifestPath"
}

$verifySha = [System.Security.Cryptography.SHA256]::Create()
try {
    foreach ($mEntry in $parsedManifest.samples) {
        $zipPath = Join-Path $OutputDir ([string]$mEntry.archiveFileName)
        if (-not (Test-Path -LiteralPath $zipPath)) {
            throw "Built sample archive missing: $zipPath"
        }

        $actualBytes = [System.IO.File]::ReadAllBytes($zipPath)
        if ([long]$actualBytes.Length -ne [long]$mEntry.sizeBytes) {
            throw "Size mismatch in '$($mEntry.archiveFileName)': expected $($mEntry.sizeBytes), got $($actualBytes.Length)"
        }

        $actualHash = ([System.BitConverter]::ToString($verifySha.ComputeHash($actualBytes))).Replace("-", "").ToLowerInvariant()
        if ($actualHash -ne [string]$mEntry.sha256) {
            throw "SHA-256 mismatch in '$($mEntry.archiveFileName)'."
        }

        $zipRead = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
        try {
            if ($zipRead.Entries.Count -eq 0) {
                throw "Archive '$($mEntry.archiveFileName)' has 0 entries."
            }

            $entryNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
            foreach ($ze in $zipRead.Entries) {
                if ($ze.FullName.Contains("\")) {
                    throw "Archive '$($mEntry.archiveFileName)' contains non-POSIX backslash path: $($ze.FullName)"
                }
                $null = $entryNames.Add($ze.FullName)
            }

            $normScene = ([string]$mEntry.entryScene) -replace '\\', '/'
            if (-not $entryNames.Contains($normScene)) {
                throw "Archive '$($mEntry.archiveFileName)' missing entryScene '$normScene'."
            }
            if (-not $entryNames.Contains("$normScene.meta")) {
                throw "Archive '$($mEntry.archiveFileName)' missing entryScene meta '$normScene.meta'."
            }
        }
        finally {
            $zipRead.Dispose()
        }
    }
}
finally {
    $verifySha.Dispose()
}

Write-Host ""
Write-Host "Packaged & verified $($manifestSamples.Count) sample archive(s) ($totalBytes bytes total) into $OutputDir" -ForegroundColor Green
Write-Host "Manifest: $manifestPath" -ForegroundColor Cyan
