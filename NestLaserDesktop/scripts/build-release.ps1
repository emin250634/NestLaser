# build-release.ps1
# NestLaser Desktop Release Build Script
# FAZ 8P - Installer, Packaging & Release Readiness

param(
    [switch]$SkipTests,
    [switch]$Portable,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$SolutionFile = "NestLaserDesktop.sln"
$ProjectFile = "NestLaserDesktop.csproj"
$Version = "1.0.0-RC1"

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = $ScriptDir

Set-Location $RootDir

Write-Host "NestLaser Desktop Release Build Script" -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Target: .NET 8.0-windows" -ForegroundColor Yellow
Write-Host ""

# Step 1: Clean
if ($Clean) {
    Write-Step "Step 1: Cleaning build artifacts"
    dotnet clean $SolutionFile -c Release
    if (Test-Path "dist") {
        Remove-Item -Recurse -Force "dist"
    }
    if (Test-Path "bin\Release") {
        Remove-Item -Recurse -Force "bin\Release"
    }
    if (Test-Path "obj\Release") {
        Remove-Item -Recurse -Force "obj\Release"
    }
    Write-Success "Clean completed"
}

# Step 2: Restore
Write-Step "Step 2: Restoring NuGet packages"
dotnet restore $SolutionFile
if ($LASTEXITCODE -ne 0) {
    Write-Failure "Restore failed"
    exit 1
}
Write-Success "Restore completed"

# Step 3: Build
Write-Step "Step 3: Building Release configuration"
dotnet build $SolutionFile -c Release -v q --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Failure "Build failed"
    exit 1
}
Write-Success "Build completed with 0 errors"

# Step 4: Test
if (-not $SkipTests) {
    Write-Step "Step 4: Running tests"
    dotnet test $SolutionFile -c Release --no-build -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Tests failed"
        exit 1
    }
    Write-Success "All tests passed"
} else {
    Write-Host "[SKIP] Tests skipped" -ForegroundColor Yellow
}

# Step 5: Publish
if ($Portable) {
    Write-Step "Step 5: Publishing Portable build"
    $PublishDir = "dist\portable"
    if (Test-Path $PublishDir) {
        Remove-Item -Recurse -Force $PublishDir
    }

    dotnet publish $ProjectFile `
        -c Release `
        -p:PublishProfile=Properties\PublishProfiles\Portable.pubxml `
        --no-build

    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Publish failed"
        exit 1
    }
    Write-Success "Portable build published to $PublishDir"
} else {
    Write-Host "[SKIP] Portable publish skipped (use -Portable flag)" -ForegroundColor Yellow
}

# Summary
Write-Step "Release Build Summary"
Write-Host "Version:     $Version" -ForegroundColor White
Write-Host "Solution:    $SolutionFile" -ForegroundColor White
Write-Host "Build:       Release" -ForegroundColor White

if (-not $SkipTests) {
    Write-Host "Tests:       Passed" -ForegroundColor Green
} else {
    Write-Host "Tests:       Skipped" -ForegroundColor Yellow
}

if ($Portable) {
    Write-Host "Portable:    dist\portable\" -ForegroundColor Green
}

Write-Host ""
Write-Success "Release build completed successfully!"
Write-Host ""

# Output location
$ExePath = "bin\Release\net8.0-windows\NestLaserDesktop.exe"
if (Test-Path $ExePath) {
    Write-Host "Executable:  $ExePath" -ForegroundColor White
    Write-Host "File size:   $((Get-Item $ExePath).Length / 1KB) KB" -ForegroundColor White
}
