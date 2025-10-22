# Main Publish Script for MAUI Template
# Handles platform selection, version management, and delegates to platform-specific scripts

param(
    [string]$Platform = "",
    [string]$Version = "",
    [switch]$ListVersions,
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import version management utilities (publish.ps1 lives in root Scripts/)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$versionManager = Join-Path $scriptDir 'version-manager.ps1'
if (-not (Test-Path -LiteralPath $versionManager)) {
    Write-Host "[ERROR] version-manager.ps1 not found at $versionManager" -ForegroundColor Red
    exit 1
}
. $versionManager

function Show-Help {
    Write-Host @"
MAUI Template Publisher

USAGE:
    .\publish.ps1 [OPTIONS]

OPTIONS:
    -Platform <platform>    Target platform (Windows, macOS, Android, iOS)
    -Version <version>      Version to publish (e.g., 1.0.0)
    -ListVersions          Show all published versions
    -Help                  Show this help message

EXAMPLES:
    .\publish.ps1                           # Interactive mode
    .\publish.ps1 -Platform Windows         # Publish for Windows with prompts
    .\publish.ps1 -Platform Windows -Version 1.0.1    # Publish specific version
    .\publish.ps1 -ListVersions             # Show version history

SUPPORTED PLATFORMS:
    Windows    - Creates installer using InnoSetup (.exe)
    macOS      - Coming soon (.pkg)
    Android    - Coming soon (.apk)
    iOS        - Coming soon (App Store)

"@ -ForegroundColor Cyan
}

function Show-AllVersions {
    Write-Host "`n=== Published Versions ===" -ForegroundColor Cyan
    
    $publishDir = "AppShell\Publish"
    if (-not (Test-Path $publishDir)) {
        Write-Host "No published versions found." -ForegroundColor Yellow
        return
    }
    
    $platforms = Get-ChildItem $publishDir -Directory -ErrorAction SilentlyContinue
    $allVersions = @{}
    
    foreach ($platformDir in $platforms) {
        $files = Get-ChildItem $platformDir.FullName -File -ErrorAction SilentlyContinue
        $platformVersions = @()
        
        foreach ($file in $files) {
            if ($file.Name -match '^(.+)_(\d+\.\d+\.\d+)_(.+)\.(exe|apk|pkg|dmg)$') {
                $version = $matches[2]
                $platformVersions += [System.Version]$version
                
                if (-not $allVersions.ContainsKey($version)) {
                    $allVersions[$version] = @()
                }
                $allVersions[$version] += $platformDir.Name
            }
        }
        
        if ($platformVersions.Count -gt 0) {
            $latest = ($platformVersions | Sort-Object -Descending)[0]
            Write-Host "  $($platformDir.Name): $($platformVersions.Count) versions, latest: $latest" -ForegroundColor White
        }
    }
    
    Write-Host "`nAll Versions:" -ForegroundColor Cyan
    $sortedVersions = $allVersions.Keys | Sort-Object { [System.Version]$_ } -Descending
    foreach ($version in $sortedVersions) {
        $platformList = $allVersions[$version] -join ", "
        Write-Host "  $version : $platformList" -ForegroundColor Gray
    }
    
    Write-Host ""
}

function Select-Platform {
    Write-Host "`n=== Platform Selection ===" -ForegroundColor Cyan
    Write-Host "Select target platform:"
    Write-Host "  [1] Windows (InnoSetup installer)"
    Write-Host "  [2] macOS (Coming soon)"
    Write-Host "  [3] Android (Coming soon)"
    Write-Host "  [4] iOS (Coming soon)"
    Write-Host ""
    
    do {
        $choice = Read-Host "Enter platform number [1]"
        if ([string]::IsNullOrEmpty($choice)) { $choice = "1" }
        
        switch ($choice) {
            "1" { return "Windows" }
            "2" { 
                Write-Host "macOS publishing is not yet implemented." -ForegroundColor Red
                return $null
            }
            "3" { 
                Write-Host "Android publishing is not yet implemented." -ForegroundColor Red
                return $null
            }
            "4" { 
                Write-Host "iOS publishing is not yet implemented." -ForegroundColor Red
                return $null
            }
            default {
                Write-Host "Invalid choice. Please enter 1-4." -ForegroundColor Red
            }
        }
    } while ($true)
}

function Select-Version {
    param([string]$TargetPlatform)
    
    Write-Host "`n=== Version Selection ===" -ForegroundColor Cyan
    
    # Get current project metadata
    $metadata = Get-ProjectMetadata
    $currentVersion = $metadata.CurrentVersion
    $latestPublished = Get-LatestPublishedVersion
    $suggestedVersion = Get-SuggestedNextVersion $currentVersion $latestPublished
    
    Write-Host "Current project version: $currentVersion" -ForegroundColor White
    if ($latestPublished) {
        Write-Host "Latest published version: $latestPublished" -ForegroundColor White
    } else {
        Write-Host "Latest published version: None (first release)" -ForegroundColor Yellow
    }
    
    # Check for existing installers
    $existingInstallers = @(Get-ExistingInstallers $suggestedVersion $TargetPlatform)
    if ($existingInstallers -and $existingInstallers.Count -gt 0) {
        Write-Host "Warning: Version $suggestedVersion already exists for ${TargetPlatform}:" -ForegroundColor Yellow
        foreach ($installer in $existingInstallers) {
            Write-Host "    $($installer.Name)" -ForegroundColor Yellow
        }
        Write-Host "   Choosing this version will overwrite existing installers." -ForegroundColor Yellow
    }
    
    Write-Host ""
    $versionPrompt = "Enter version [$suggestedVersion]"
    $chosenVersion = Read-Host $versionPrompt
    
    if ([string]::IsNullOrEmpty($chosenVersion)) {
        $chosenVersion = $suggestedVersion
    }
    
    # Validate version format
    try {
        # Cast to System.Version to validate format; result not stored
        [void][System.Version]$chosenVersion
    }
    catch {
    Write-Host "[ERROR] Invalid version format: $chosenVersion" -ForegroundColor Red
        Write-Host "   Use format: Major.Minor.Build (e.g., 1.0.0)" -ForegroundColor Red
        exit 1
    }
    
    return $chosenVersion
}

function Invoke-PlatformPublisher {
    param(
        [string]$Platform,
        [string]$Version
    )
    
    $platformScript = "AppShell\Platforms\$Platform\Scripts\publish.ps1"
    
    if (-not (Test-Path $platformScript)) {
        Write-Host "[ERROR] Platform script not found: $platformScript" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n=== Publishing for $Platform ===" -ForegroundColor Cyan
    Write-Host "Delegating to: $platformScript" -ForegroundColor Gray
    Write-Host "Version: $Version" -ForegroundColor Gray
    Write-Host ""
    
    # Call platform-specific script
    & $platformScript -Version $Version
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[OK] Successfully published $Platform installer version $Version" -ForegroundColor Green
        
        # Show what was created
        $publishDir = "AppShell\Publish\$Platform"
        $metadata = Get-ProjectMetadata
        $appName = $metadata.DisplayTitle -replace '[^\w\-_]', ''
        $pattern = "${appName}_${Version}_${Platform}.*"
        $createdFiles = Get-ChildItem $publishDir -Filter $pattern -ErrorAction SilentlyContinue
        
        if ($createdFiles.Count -gt 0) {
            Write-Host "Created installers:" -ForegroundColor Green
            foreach ($file in $createdFiles) {
                Write-Host "  - $($file.FullName)" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "`n[ERROR] Failed to publish $Platform installer" -ForegroundColor Red
        exit 1
    }
}

# Main script logic
try {
    if ($Help) {
        Show-Help
        exit 0
    }
    
    if ($ListVersions) {
        Show-AllVersions
        exit 0
    }
    
    # Ensure we're in the right directory
    if (-not (Test-Path "AppShell\AppShell.csproj")) {
        Write-Host "[ERROR] Please run this script from the root of the MAUI template project." -ForegroundColor Red
        exit 1
    }
    
    # Token validation (ensure setup ran)
    $projectFile = 'AppShell\AppShell.csproj'
    $projContent = Get-Content -LiteralPath $projectFile -Raw
    $tokens = @('@@DISPLAY_TITLE@@','@@PACKAGE_ID@@','@@APP_DESCRIPTION@@','@@COMPANY_NAME@@')
    $remaining = @()
    foreach ($t in $tokens) { if ($projContent -like "*${t}*") { $remaining += $t } }
    if ($remaining.Count -gt 0) {
        Write-Host "[ERROR] Template tokens still present: $($remaining -join ', ')" -ForegroundColor Red
        Write-Host "        Run the setup script first:" -ForegroundColor Yellow
        Write-Host "          .\\Scripts\\setup.ps1" -ForegroundColor Yellow
        Write-Host "        Optional Parameters:" -ForegroundColor Yellow
        Write-Host "          -Help" -ForegroundColor Yellow
        Write-Host "          -DryRun" -ForegroundColor Yellow
        exit 1
    }

    # Show current version summary
    Show-VersionSummary
    
    # Platform selection
    if ([string]::IsNullOrEmpty($Platform)) {
        $Platform = Select-Platform
        if ($null -eq $Platform) {
            exit 1
        }
    }
    
    # Version selection
    if ([string]::IsNullOrEmpty($Version)) {
        $Version = Select-Version $Platform
    }
    
    # Update project files with new version
    Update-ProjectVersion $Version
    
    # Delegate to platform-specific script
    Invoke-PlatformPublisher $Platform $Version
    
} catch {
    Write-Host "[ERROR] An error occurred: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.InvocationInfo.PositionMessage) { Write-Host $_.InvocationInfo.PositionMessage -ForegroundColor DarkGray }
    exit 1
}
