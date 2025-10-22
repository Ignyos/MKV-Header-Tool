# Windows Publisher Script using InnoSetup
# Builds the MAUI app and creates Windows installer

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Show-Help {
    Write-Host @"
Windows Publisher for MAUI Template

USAGE:
    .\publish.ps1 -Version <version> [OPTIONS]

REQUIRED:
    -Version <version>      Version to publish (e.g., 1.0.0)

OPTIONS:
    -Configuration <config> Build configuration (Release, Debug) [Default: Release]
    -SkipBuild             Skip the build step (use existing build)
    -Help                  Show this help message

REQUIREMENTS:
    - .NET 9.0 SDK or later
    - InnoSetup 6.0 or later (free download from jrsoftware.org)
    - Windows 10/11

EXAMPLES:
    .\publish.ps1 -Version 1.0.0                    # Build and create installer
    .\publish.ps1 -Version 1.0.1 -SkipBuild         # Create installer only

"@ -ForegroundColor Cyan
}

function Test-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Cyan
    
    # Check .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>$null
    Write-Host "[OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
    }
    catch {
    Write-Host "[ERROR] .NET SDK not found. Please install .NET 9.0 SDK or later." -ForegroundColor Red
        return $false
    }
    
    # Check InnoSetup
    $innoSetupPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    
    $innoSetupPath = $null
    foreach ($path in $innoSetupPaths) {
        if (Test-Path $path) {
            $innoSetupPath = $path
            break
        }
    }
    
    if ($innoSetupPath) {
    Write-Host "[OK] InnoSetup found: $innoSetupPath" -ForegroundColor Green
        return $innoSetupPath
    } else {
    Write-Host "[ERROR] InnoSetup not found. Please install InnoSetup 6.0 from: https://jrsoftware.org/isinfo.php" -ForegroundColor Red
        return $false
    }
}

function Build-MauiApp {
    param(
        [string]$Configuration,
        [string]$Version
    )
    
    Write-Host "`nBuilding MAUI application..." -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration" -ForegroundColor Gray
    Write-Host "Version: $Version" -ForegroundColor Gray
    
    # Project file is three levels up from this script (Platforms/Windows/Scripts)
    $projectPath = "..\..\..\AppShell.csproj"
    $targetFramework = "net9.0-windows10.0.19041.0"
    # Output path referenced implicitly via publish step; explicit variable removed to satisfy analyzer
    
    # Clean previous build
    Write-Host "Cleaning previous build..." -ForegroundColor Gray
    & dotnet clean $projectPath -c $Configuration -f $targetFramework | Out-Null
    
    # Build the application
    Write-Host "Building application..." -ForegroundColor Gray
    $buildResult = & dotnet build $projectPath -c $Configuration -f $targetFramework --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
        return $false
    }
    
    # Publish (first try self-contained; fallback to framework-dependent if necessary)
    Write-Host "Publishing self-contained application..." -ForegroundColor Gray
    $publishResult = & dotnet publish $projectPath -c $Configuration -f $targetFramework -p:TargetFrameworks=$targetFramework -r win10-x64 --self-contained true -p:PublishSingleFile=false --verbosity minimal 2>&1
    $selfContained = $true
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARN] Self-contained publish failed, falling back to framework-dependent publish." -ForegroundColor Yellow
        Write-Host $publishResult -ForegroundColor DarkYellow
        $selfContained = $false
        $publishResult = & dotnet publish $projectPath -c $Configuration -f $targetFramework -p:TargetFrameworks=$targetFramework --no-self-contained --verbosity minimal 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Publish failed (framework-dependent)" -ForegroundColor Red
            Write-Host $publishResult -ForegroundColor Red
            return $false
        }
    }
    
    if ($selfContained) {
        $publishPath = Join-Path (Split-Path $projectPath -Parent) "bin\$Configuration\$targetFramework\win10-x64\publish"
    } else {
        $publishPath = Join-Path (Split-Path $projectPath -Parent) "bin\$Configuration\$targetFramework\publish"
    }
    
    if (-not (Test-Path $publishPath)) {
    Write-Host "[ERROR] Published files not found at: $publishPath" -ForegroundColor Red
        return $false
    }
    
    Write-Host "[OK] Build completed successfully" -ForegroundColor Green
    Write-Host "Published to: $publishPath" -ForegroundColor Gray
    
    return $publishPath
}

function Invoke-SvgSanitization {
    param(
        [string]$Root = '..\\..\\..\\Resources',
        [switch]$Verbose
    )
    if (-not (Test-Path $Root)) { return }
    Write-Host "Scanning SVG assets for XML prologs/DOCTYPE..." -ForegroundColor Cyan
    $svgFiles = Get-ChildItem $Root -Recurse -Filter *.svg -File -ErrorAction SilentlyContinue
    foreach ($file in $svgFiles) {
        try {
            $raw = Get-Content $file.FullName -Raw -Encoding UTF8
            $lines = $raw -split "`r?`n"
            $originalCount = $lines.Count
            $filtered = $lines | Where-Object { $_ -notmatch '^<\?xml' -and $_ -notmatch '^<!DOCTYPE' }
            while ($filtered.Count -gt 0 -and [string]::IsNullOrWhiteSpace($filtered[0])) { $filtered = $filtered[1..($filtered.Count-1)] }
            if ($filtered.Count -lt $originalCount) {
                Set-Content $file.FullName ($filtered -join [Environment]::NewLine) -Encoding UTF8 -NoNewline
                Write-Host "   Sanitized: $($file.FullName)" -ForegroundColor Green
            } elseif ($Verbose) {
                Write-Host "   OK: $($file.FullName)" -ForegroundColor Gray
            }
        } catch {
            Write-Host "   [WARN] Could not process SVG $($file.FullName): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

function Get-ProjectMetadata {
    $projectPath = "..\..\..\AppShell.csproj"
    [xml]$projectXml = Get-Content $projectPath
    
    return @{
        DisplayTitle = $projectXml.Project.PropertyGroup.ApplicationTitle
        PackageId = $projectXml.Project.PropertyGroup.ApplicationId
        CompanyName = $projectXml.Project.PropertyGroup.Company
        Description = $projectXml.Project.PropertyGroup.Description
        Copyright = $projectXml.Project.PropertyGroup.Copyright
    }
}

function New-InnoSetupScript {
    param(
        [string]$Version,
        [string]$PublishPath,
        [hashtable]$Metadata
    )
    
    Write-Host "Creating InnoSetup script..." -ForegroundColor Cyan
    
    # Clean app name for filename
    $appName = $Metadata.DisplayTitle -replace '[^\w\-_]', ''
    $setupFileName = "${appName}_${Version}_Windows"
    
    # Resolve icon (optional). Expect an ICO in Resources\AppIcon or fall back to none.
    $iconCandidate = '..\..\..\Resources\AppIcon\appicon.ico'
    $iconDirective = ''
    if (Test-Path $iconCandidate) {
        $iconDirective = "SetupIconFile=$iconCandidate"
    } else {
        Write-Host '[WARN] appicon.ico not found; installer will use default icon.' -ForegroundColor Yellow
    }

    $innoScript = @"
; InnoSetup Script for $($Metadata.DisplayTitle)
; Generated automatically by MAUI Template Publisher

#define MyAppName "$($Metadata.DisplayTitle)"
#define MyAppVersion "$Version"
#define MyAppPublisher "$($Metadata.CompanyName)"
#define MyAppURL "https://example.com"
#define MyAppExeName "AppShell.exe"
#define MyAppId "$($Metadata.PackageId)"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\..\..\Publish\Windows
OutputBaseFilename=${setupFileName}
$iconDirective
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "$PublishPath\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
"@

    $scriptPath = "AppInstaller.iss"
    Set-Content $scriptPath $innoScript -Encoding UTF8
    
    Write-Host "[OK] InnoSetup script created: $scriptPath" -ForegroundColor Green
    return $scriptPath
}

function Build-Installer {
    param(
        [string]$InnoSetupPath,
        [string]$ScriptPath
    )
    
    Write-Host "`nBuilding Windows installer..." -ForegroundColor Cyan
    
    # Ensure output directory exists
    $outputDir = "..\..\..\Publish\Windows"
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    
    # Run InnoSetup compiler
    Write-Host "Running InnoSetup compiler..." -ForegroundColor Gray
    $result = & $InnoSetupPath $ScriptPath
    
    if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] InnoSetup compilation failed" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        return $false
    }
    
    Write-Host "[OK] Installer created successfully" -ForegroundColor Green
    return $true
}

function Remove-TempFiles {
    Write-Host "Cleaning up temporary files..." -ForegroundColor Gray
    
    $tempFiles = @("AppInstaller.iss")
    foreach ($file in $tempFiles) {
        if (Test-Path $file) {
            Remove-Item $file -Force
        }
    }
}

# Main script execution
try {
    if ($Help) {
        Show-Help
        exit 0
    }
    
    # Change to script directory
    Set-Location (Split-Path -Parent $MyInvocation.MyCommand.Path)
    
    Write-Host "=== Windows Publisher for MAUI Template ===" -ForegroundColor Cyan
    Write-Host "Version: $Version" -ForegroundColor White
    Write-Host "Configuration: $Configuration" -ForegroundColor White
    Write-Host ""
    
    # Check prerequisites
    $innoSetupPath = Test-Prerequisites
    if (-not $innoSetupPath) {
        exit 1
    }
    
    # Get project metadata
    $metadata = Get-ProjectMetadata
    Write-Host "App: $($metadata.DisplayTitle)" -ForegroundColor White
    Write-Host "Publisher: $($metadata.CompanyName)" -ForegroundColor White
    Write-Host ""
    
    # Build the application (unless skipped)
    if (-not $SkipBuild) {
    # Pre-build sanitization to avoid Resizetizer XML declaration issues
    Invoke-SvgSanitization
        $publishPath = Build-MauiApp $Configuration $Version
        if (-not $publishPath) {
            exit 1
        }
    } else {
    # Determine existing publish path (prefer self-contained if both exist)
    $scPath = "..\..\..\bin\$Configuration\net9.0-windows10.0.19041.0\win10-x64\publish"
    $fdPath = "..\..\..\bin\$Configuration\net9.0-windows10.0.19041.0\publish"
    if (Test-Path $scPath) { $publishPath = $scPath } elseif (Test-Path $fdPath) { $publishPath = $fdPath } else { $publishPath = $scPath }
        if (-not (Test-Path $publishPath)) {
            Write-Host "[ERROR] Published files not found. Run without -SkipBuild first." -ForegroundColor Red
            exit 1
        }
    Write-Host "[WARN] Using existing build at: $publishPath" -ForegroundColor Yellow
    }
    
    # Create InnoSetup script
    $scriptPath = New-InnoSetupScript $Version $publishPath $metadata
    
    # Build installer
    $success = Build-Installer $innoSetupPath $scriptPath
    
    # Cleanup
    Remove-TempFiles
    
    if ($success) {
        Write-Host "`n[SUCCESS] Windows installer created successfully" -ForegroundColor Green
        
        # Show created files
        $appName = $metadata.DisplayTitle -replace '[^\w\-_]', ''
        $installerName = "${appName}_${Version}_Windows.exe"
    $installerPath = "..\..\..\Publish\Windows\$installerName"
        
        if (Test-Path $installerPath) {
            $fileSize = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
            Write-Host "[INFO] Installer: $installerPath ($fileSize MB)" -ForegroundColor Green
        }
        
        exit 0
    } else {
        exit 1
    }
    
} catch {
    Write-Host "[ERROR] An error occurred: $($_.Exception.Message)" -ForegroundColor Red
    Remove-TempFiles
    exit 1
}
