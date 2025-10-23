# PowerShell script to create ICO file from SVG
# This script requires ImageMagick or similar tool to be installed

param(
    [string]$SvgPath = ".\Resources\AppIcon\systray-icon.svg",
    [string]$OutputPath = ".\Resources\mkv-icon.ico"
)

Write-Host "Creating ICO file for MKV Header Tool system tray..."

# Check if ImageMagick is available
$magickPath = Get-Command "magick" -ErrorAction SilentlyContinue

if ($magickPath) {
    Write-Host "Using ImageMagick to convert SVG to ICO..."
    
    # Create temporary PNG files at different sizes
    $tempDir = [System.IO.Path]::GetTempPath()
    $temp16 = Join-Path $tempDir "mkv-16.png"
    $temp32 = Join-Path $tempDir "mkv-32.png"
    $temp48 = Join-Path $tempDir "mkv-48.png"
    
    try {
        # Convert SVG to different PNG sizes
        & magick $SvgPath -resize 16x16 $temp16
        & magick $SvgPath -resize 32x32 $temp32
        & magick $SvgPath -resize 48x48 $temp48
        
        # Combine into ICO file
        & magick $temp16 $temp32 $temp48 $OutputPath
        
        Write-Host "✅ ICO file created successfully: $OutputPath"
    }
    catch {
        Write-Host "❌ Error creating ICO file: $_"
    }
    finally {
        # Clean up temp files
        Remove-Item $temp16, $temp32, $temp48 -ErrorAction SilentlyContinue
    }
}
else {
    Write-Host "❌ ImageMagick not found. Please install ImageMagick or manually convert the SVG files to ICO format."
    Write-Host "Alternative: Use an online SVG to ICO converter with these files:"
    Write-Host "  - Resources\AppIcon\systray-icon.svg (16x16)"
    Write-Host "  - Resources\AppIcon\systray-32.svg (32x32)"
    Write-Host "Save the result as: $OutputPath"
}