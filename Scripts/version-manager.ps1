# Version Management Utilities for MAUI Template
# Handles version detection, validation, and updates across all project files

function Get-ProjectMetadata {
    param(
        [string]$ProjectPath = "AppShell\AppShell.csproj"
    )
    
    if (-not (Test-Path $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }
    
    [xml]$projectXml = Get-Content $ProjectPath
    
    $metadata = @{
        DisplayTitle = $projectXml.Project.PropertyGroup.ApplicationTitle
        PackageId = $projectXml.Project.PropertyGroup.ApplicationId
        CompanyName = $projectXml.Project.PropertyGroup.Company
        CurrentVersion = $projectXml.Project.PropertyGroup.ApplicationDisplayVersion
        Description = $projectXml.Project.PropertyGroup.Description
    }
    
    return $metadata
}

function Get-LatestPublishedVersion {
    param(
        [string]$PublishDir = "AppShell\Publish"
    )
    
    if (-not (Test-Path $PublishDir)) {
        Write-Host "No publish directory found. This will be the first release." -ForegroundColor Yellow
        return $null
    }
    
    $files = Get-ChildItem -Recurse $PublishDir -File -ErrorAction SilentlyContinue
    $versions = @()
    
    foreach ($file in $files) {
        # Pattern: AppName_Version_Platform.ext
        if ($file.Name -match '^(.+)_(\d+\.\d+\.\d+)_(.+)\.(exe|apk|pkg|dmg)$') {
            try {
                $version = [System.Version]$matches[2]
                $versions += $version
            }
            catch {
                # Skip invalid version formats
                continue
            }
        }
    }
    
    if ($versions.Count -eq 0) {
        return $null
    }
    
    return ($versions | Sort-Object -Descending)[0]
}

function Get-SuggestedNextVersion {
    param(
        [string]$CurrentVersion,
        [System.Version]$LatestPublished
    )
    
    $current = [System.Version]$CurrentVersion
    
    if ($null -eq $LatestPublished) {
        return $CurrentVersion
    }
    
    # If current project version is newer, suggest that
    if ($current -gt $LatestPublished) {
        return $CurrentVersion
    }
    
    # Otherwise increment the latest published version
    $nextVersion = New-Object System.Version($LatestPublished.Major, $LatestPublished.Minor, ($LatestPublished.Build + 1))
    return $nextVersion.ToString()
}

function Update-ProjectVersion {
    param(
        [string]$NewVersion,
        [string]$ProjectPath = "AppShell\AppShell.csproj"
    )
    
    Write-Host "Updating project version to $NewVersion..." -ForegroundColor Cyan
    
    # Validate version format
    try {
        # Validate format (cast only)
        [void][System.Version]$NewVersion
    }
    catch {
        throw "Invalid version format: $NewVersion. Use format: Major.Minor.Build (e.g., 1.0.0)"
    }
    
    # Update project file
    [xml]$projectXml = Get-Content $ProjectPath
    $projectXml.Project.PropertyGroup.ApplicationDisplayVersion = $NewVersion
    $projectXml.Save((Resolve-Path $ProjectPath))
    
    # Update platform-specific manifests
    Update-PlatformManifests -Version $NewVersion
    
    # Using ASCII-only status tag for compatibility across shells
    Write-Host "[OK] Project version updated to $NewVersion" -ForegroundColor Green
}

function Update-PlatformManifests {
    param([string]$Version)
    
    # Windows Package.appxmanifest - needs 4-part version
    $windowsManifest = "AppShell\Platforms\Windows\Package.appxmanifest"
    if (Test-Path $windowsManifest) {
        $fourPartVersion = "$Version.0"
        $content = Get-Content $windowsManifest -Raw
        $content = $content -replace 'Version="[\d\.]+"', "Version=`"$fourPartVersion`""
        Set-Content $windowsManifest $content -NoNewline
    Write-Host "  [OK] Updated Windows manifest" -ForegroundColor Green
    }
    
    # iOS Info.plist
    $iosManifest = "AppShell\Platforms\iOS\Info.plist"
    if (Test-Path $iosManifest) {
        # Placeholder: parse/update Info.plist if needed in future
        $null = Get-Content $iosManifest -ErrorAction SilentlyContinue
    Write-Host "  [OK] iOS manifest ready" -ForegroundColor Green
    }
    
    # Android (version is handled by MAUI project properties)
    Write-Host "  [OK] Android manifest ready" -ForegroundColor Green
    
    # macOS Info.plist
    $macManifest = "AppShell\Platforms\MacCatalyst\Info.plist"
    if (Test-Path $macManifest) {
    Write-Host "  [OK] macOS manifest ready" -ForegroundColor Green
    }
}

function Get-ExistingInstallers {
    param(
        [string]$Version,
        [string]$Platform,
        [string]$PublishDir = "AppShell\Publish"
    )
    
    $platformDir = Join-Path $PublishDir $Platform
    if (-not (Test-Path $platformDir)) {
        return @()
    }
    
    $metadata = Get-ProjectMetadata
    $appName = $metadata.DisplayTitle -replace '[^\w\-_]', ''  # Remove invalid filename chars
    
    $pattern = "${appName}_${Version}_${Platform}.*"
    $existingFiles = Get-ChildItem $platformDir -Filter $pattern -ErrorAction SilentlyContinue
    
    return $existingFiles
}

function Show-VersionSummary {
    param(
        [string]$PublishDir = "AppShell\Publish"
    )
    
    Write-Host "`n=== Version Summary ===" -ForegroundColor Cyan
    
    $metadata = Get-ProjectMetadata
    Write-Host "Project Version: $($metadata.CurrentVersion)" -ForegroundColor White
    
    $latestPublished = Get-LatestPublishedVersion $PublishDir
    if ($latestPublished) {
        Write-Host "Latest Published: $latestPublished" -ForegroundColor White
    } else {
        Write-Host "Latest Published: None (first release)" -ForegroundColor Yellow
    }
    
    # Show versions by platform
    if (Test-Path $PublishDir) {
        $platforms = @(Get-ChildItem $PublishDir -Directory -ErrorAction SilentlyContinue)
        foreach ($platform in $platforms) {
            $files = @(Get-ChildItem $platform.FullName -File -ErrorAction SilentlyContinue)
            if ($files -and $files.Count -gt 0) {
                $platformVersions = @()
                foreach ($file in $files) {
                    if ($file.Name -match '_(\d+\.\d+\.\d+)_') {
                        try {
                            $platformVersions += [System.Version]$matches[1]
                        } catch { }
                    }
                }
                if ($platformVersions.Count -gt 0) {
                    $latest = ($platformVersions | Sort-Object -Descending)[0]
                    Write-Host "  $($platform.Name): $latest" -ForegroundColor Gray
                }
            }
        }
    }
    
    Write-Host "========================`n" -ForegroundColor Cyan
}

# Functions are available when dot-sourced
