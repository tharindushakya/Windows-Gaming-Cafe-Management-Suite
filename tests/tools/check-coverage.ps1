param(
    [string]$coveragePattern = "**/coverage.cobertura.xml",
    [int]$minPercent = 70
)

Write-Host "Searching for coverage files with pattern: $coveragePattern"
$files = Get-ChildItem -Path . -Recurse -Include $(Split-Path -Leaf $coveragePattern) -ErrorAction SilentlyContinue
if (-not $files) {
    Write-Host "No coverage files found. Exiting with success=0 (consider failing depending on policy)."
    exit 0
}

$totalCovered = 0
$totalElements = 0
# load thresholds if present
$thresholdsPath = Join-Path -Path (Resolve-Path ..\) -ChildPath "tests/coverage-thresholds.json"
$thresholds = $null
if (Test-Path -Path .\tests\coverage-thresholds.json) {
    try { $thresholds = Get-Content .\tests\coverage-thresholds.json | ConvertFrom-Json } catch { $thresholds = $null }
}

if ($thresholds -ne $null) { Write-Host "Loaded coverage thresholds" }

function Enforce-PackageThreshold($packageName, $percentValue) {
    if ($thresholds -eq $null) { return $true }
    $default = $thresholds.default
    $pkgThreshold = $null
    if ($thresholds.packages -and $thresholds.packages.$packageName) { $pkgThreshold = [int]$thresholds.packages.$packageName }
    $limit = if ($pkgThreshold) { $pkgThreshold } else { if ($default) { [int]$default } else { $minPercent } }
    if ($percentValue -lt $limit) {
        Write-Host "Package $packageName coverage $percentValue% is below threshold $limit%"
        return $false
    }
    Write-Host "Package $packageName coverage $percentValue% meets threshold $limit%"
    return $true
}
foreach ($f in $files) {
    Write-Host "Processing $($f.FullName)"
    [xml]$xml = Get-Content $f.FullName
    $coverage = $xml.coverage
    if (-not $coverage) { continue }

    # Prefer top-level attributes lines-covered / lines-valid
    $linesCoveredAttr = $coverage.Attributes["lines-covered"]
    $linesValidAttr = $coverage.Attributes["lines-valid"]
    if ($linesCoveredAttr -and $linesValidAttr) {
        $covered = [int]$linesCoveredAttr.Value
        $elements = [int]$linesValidAttr.Value
        if ($elements -gt 0) {
            $totalCovered += $covered
            $totalElements += $elements
        }
        continue
    }

    # Fallback: try metrics element (older formats)
    $metrics = $xml.coverage.metrics
    if ($metrics) {
        $coveredAttr = $metrics.Attributes["coveredlines"]
        $nclocAttr = $metrics.Attributes["ncloc"]
        $covered = 0
        $elements = 0
        if ($coveredAttr) { $covered = [int]$coveredAttr.Value }
        if ($nclocAttr) { $elements = [int]$nclocAttr.Value }
        if ($elements -gt 0) {
            $totalCovered += $covered
            $totalElements += $elements
        }
    }
}

if ($totalElements -eq 0) {
    Write-Host "No metric elements found in cobertura files. Skipping threshold check."
    exit 0
}

$percent = [math]::Round(($totalCovered / $totalElements) * 100, 2)
Write-Host "Aggregate coverage: $percent% (covered: $totalCovered / $totalElements)"
if ($percent -lt $minPercent) {
    Write-Host "Aggregate coverage $percent% is below global threshold of $minPercent%"
    exit 1
}

# enforce per-package thresholds
$allGood = $true
foreach ($f in $files) {
    [xml]$xml = Get-Content $f.FullName
    $packages = $xml.coverage.packages.package
    if (-not $packages) { continue }
    foreach ($pkg in $packages) {
        $pkgName = $pkg.Attributes["name"].Value
        $linesCoveredAttr = $pkg.Attributes["lines-covered"]
        $linesValidAttr = $pkg.Attributes["lines-valid"]
        if ($linesCoveredAttr -and $linesValidAttr) {
            $pc = [math]::Round(( [int]$linesCoveredAttr.Value / [int]$linesValidAttr.Value ) * 100, 2)
            $ok = Enforce-PackageThreshold $pkgName $pc
            if (-not $ok) { $allGood = $false }
        }
        else {
            # fallback to line-rate attribute
            $lineRate = $pkg.Attributes["line-rate"]
            if ($lineRate) {
                $pc = [math]::Round(([double]$lineRate.Value * 100), 2)
                $ok = Enforce-PackageThreshold $pkgName $pc
                if (-not $ok) { $allGood = $false }
            }
        }
    }
}

if (-not $allGood) { exit 1 }

Write-Host "Coverage meets configured thresholds"
exit 0
