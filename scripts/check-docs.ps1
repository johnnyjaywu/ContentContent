<#
.SYNOPSIS
    Validates Docsify documentation completeness, removed-package references,
    and relative markdown links across Unity/ContentContent/.
#>
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$packagesRoot = Join-Path $repoRoot "Packages"
$docsRoot = Join-Path $repoRoot "docs"
$errors = New-Object System.Collections.Generic.List[string]

$archPath = Join-Path $docsRoot "architecture.md"
$sidebarPath = Join-Path $docsRoot "_sidebar.md"
$docsReadmePath = Join-Path $docsRoot "README.md"
$repoReadmePath = Join-Path $repoRoot "README.md"

if (-not (Test-Path -LiteralPath $archPath)) { $errors.Add("Missing docs/architecture.md") }
$sidebarText = if (Test-Path -LiteralPath $sidebarPath) { Get-Content -LiteralPath $sidebarPath -Raw } else { $errors.Add("Missing docs/_sidebar.md"); "" }
$docsReadmeText = if (Test-Path -LiteralPath $docsReadmePath) { Get-Content -LiteralPath $docsReadmePath -Raw } else { $errors.Add("Missing docs/README.md"); "" }
$repoReadmeText = if (Test-Path -LiteralPath $repoReadmePath) { Get-Content -LiteralPath $repoReadmePath -Raw } else { $errors.Add("Missing README.md"); "" }

$packageDirs = @(Get-ChildItem -Path $packagesRoot -Directory -Filter "com.contentcontent.*")
foreach ($pkg in $packageDirs) {
    $slug = $pkg.Name -replace '^com\.contentcontent\.', ''
    $pkgDocsDir = Join-Path $docsRoot $slug

    foreach ($page in @("quickstart.md", "design.md", "scripting-api.md")) {
        $pagePath = Join-Path $pkgDocsDir $page
        if (-not (Test-Path -LiteralPath $pagePath)) {
            $errors.Add("Missing doc page: docs/$slug/$page")
        }
        $relLink = "$slug/$page"
        if ($sidebarText -and ($sidebarText -notmatch [regex]::Escape($relLink))) {
            $errors.Add("docs/_sidebar.md missing link to $relLink")
        }
    }

    if ($docsReadmeText -and ($docsReadmeText -notmatch [regex]::Escape($pkg.Name))) {
        $errors.Add("docs/README.md missing entry for $($pkg.Name)")
    }
    if ($repoReadmeText -and ($repoReadmeText -notmatch [regex]::Escape($pkg.Name))) {
        $errors.Add("README.md missing entry for $($pkg.Name)")
    }
}

$mdFiles = @()
if (Test-Path -LiteralPath $docsRoot) {
    $mdFiles += Get-ChildItem -Path $docsRoot -Recurse -File -Filter "*.md"
}
if (Test-Path -LiteralPath $repoReadmePath) {
    $mdFiles += Get-Item -LiteralPath $repoReadmePath
}

$linkRegex = [regex]'\[[^\]]*\]\(([^)]+)\)'
foreach ($md in $mdFiles) {
    $content = Get-Content -LiteralPath $md.FullName -Raw
    $relMd = ($md.FullName.Substring($repoRoot.Length).TrimStart('\', '/')) -replace '\\', '/'

    if ($content -match 'com\.contentcontent\.camera') {
        $errors.Add("Removed package reference 'com.contentcontent.camera' found in $relMd")
    }

    # Strip fenced code blocks before checking links
    $stripped = [regex]::Replace($content, '(?s)```.*?```', '')
    foreach ($m in $linkRegex.Matches($stripped)) {
        $rawTarget = $m.Groups[1].Value.Trim()
        if ($rawTarget -match '^(https?:|mailto:|#)') {
            continue
        }

        $target = ($rawTarget -split '\s+')[0]
        $target = ($target -split '#')[0]
        if ([string]::IsNullOrWhiteSpace($target)) {
            continue
        }

        $normalizedTarget = $target -replace '/', [System.IO.Path]::DirectorySeparatorChar
        if ($target.StartsWith("/")) {
            $resolved = Join-Path $docsRoot ($normalizedTarget.TrimStart([System.IO.Path]::DirectorySeparatorChar))
        }
        else {
            $resolved = Join-Path $md.DirectoryName $normalizedTarget
        }

        if (-not (Test-Path -LiteralPath $resolved)) {
            $errors.Add("Broken link in ${relMd}: ($rawTarget)")
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Docs check FAILED with $($errors.Count) issue(s):" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Docs check PASSED ($($packageDirs.Count) packages, $($mdFiles.Count) markdown files checked)." -ForegroundColor Green
exit 0
