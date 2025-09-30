#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks and validates package versions across the solution
.DESCRIPTION
    This script verifies that all package versions are consistent and up-to-date
.EXAMPLE
    ./check-versions.ps1
#>

param(
    [switch]$UpdateOutdated,
    [switch]$CheckSecurity
)

Write-Host "NumbatWallet Version Check" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

# Check .NET SDK version
Write-Host "`nChecking .NET SDK version..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
$requiredVersion = "9.0"
if ($dotnetVersion -notlike "$requiredVersion*") {
    Write-Warning "Expected .NET $requiredVersion, but found $dotnetVersion"
} else {
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
}

# Check for central package management
Write-Host "`nChecking central package management..." -ForegroundColor Yellow
if (Test-Path "./Directory.Packages.props") {
    Write-Host "✓ Central package management configured" -ForegroundColor Green
} else {
    Write-Warning "Directory.Packages.props not found"
}

# List outdated packages
Write-Host "`nChecking for outdated packages..." -ForegroundColor Yellow
$outdatedPackages = dotnet list package --outdated --format json 2>$null | ConvertFrom-Json

if ($outdatedPackages.projects.Count -gt 0) {
    $hasOutdated = $false
    foreach ($project in $outdatedPackages.projects) {
        if ($project.frameworks[0].topLevelPackages.Count -gt 0) {
            $hasOutdated = $true
            Write-Host "`nProject: $($project.path)" -ForegroundColor Cyan
            foreach ($package in $project.frameworks[0].topLevelPackages) {
                Write-Host "  - $($package.id): $($package.requestedVersion) → $($package.latestVersion)" -ForegroundColor Yellow
            }
        }
    }

    if ($hasOutdated -and $UpdateOutdated) {
        Write-Host "`nUpdating outdated packages..." -ForegroundColor Yellow
        dotnet outdated --upgrade
    }
} else {
    Write-Host "✓ All packages are up-to-date" -ForegroundColor Green
}

# Check for vulnerable packages
if ($CheckSecurity) {
    Write-Host "`nChecking for security vulnerabilities..." -ForegroundColor Yellow
    $vulnerablePackages = dotnet list package --vulnerable --format json 2>$null | ConvertFrom-Json

    if ($vulnerablePackages.projects.Count -gt 0) {
        $hasVulnerable = $false
        foreach ($project in $vulnerablePackages.projects) {
            if ($project.frameworks[0].topLevelPackages.Count -gt 0) {
                $hasVulnerable = $true
                Write-Warning "Security vulnerabilities found in: $($project.path)"
                foreach ($package in $project.frameworks[0].topLevelPackages) {
                    Write-Warning "  - $($package.id): $($package.requestedVersion)"
                }
            }
        }

        if (-not $hasVulnerable) {
            Write-Host "✓ No security vulnerabilities found" -ForegroundColor Green
        }
    } else {
        Write-Host "✓ No security vulnerabilities found" -ForegroundColor Green
    }
}

# Check Aspire version consistency
Write-Host "`nChecking Aspire version consistency..." -ForegroundColor Yellow
$aspireVersion = "9.4.2"
$aspirePackages = Get-Content "./Directory.Packages.props" | Select-String "Aspire" | Select-String $aspireVersion

if ($aspirePackages.Count -gt 0) {
    Write-Host "✓ Aspire packages are at version $aspireVersion" -ForegroundColor Green
} else {
    Write-Warning "Aspire package versions may be inconsistent"
}

# Summary
Write-Host "`n=========================" -ForegroundColor Cyan
Write-Host "Version check complete!" -ForegroundColor Cyan

# Return exit code
if ($hasVulnerable) {
    exit 1
}
exit 0