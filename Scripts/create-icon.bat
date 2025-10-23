@echo off
echo Creating ICO file for MKV Header Tool...

REM Check if ImageMagick is available
where magick >nul 2>nul
if %errorlevel% neq 0 (
    echo ImageMagick not found. Please install ImageMagick or use an online converter.
    echo Convert these files to ICO format:
    echo   - Resources\AppIcon\systray-icon.svg ^(16x16^)
    echo   - Resources\AppIcon\systray-32.svg ^(32x32^)
    echo Save as: Resources\mkv-icon.ico
    pause
    exit /b 1
)

REM Create temporary directory
set TEMP_DIR=%TEMP%\mkv-icon-temp
mkdir "%TEMP_DIR%" 2>nul

REM Convert SVG to different PNG sizes
echo Converting SVG to PNG sizes...
magick "Resources\AppIcon\systray-icon.svg" -resize 16x16 "%TEMP_DIR%\mkv-16.png"
magick "Resources\AppIcon\systray-32.svg" -resize 32x32 "%TEMP_DIR%\mkv-32.png"
magick "Resources\AppIcon\mkv-icon-64.svg" -resize 48x48 "%TEMP_DIR%\mkv-48.png"

REM Combine into ICO file
echo Creating ICO file...
magick "%TEMP_DIR%\mkv-16.png" "%TEMP_DIR%\mkv-32.png" "%TEMP_DIR%\mkv-48.png" "Resources\mkv-icon.ico"

REM Clean up
rmdir /s /q "%TEMP_DIR%"

if exist "Resources\mkv-icon.ico" (
    echo ✓ ICO file created successfully: Resources\mkv-icon.ico
) else (
    echo ✗ Failed to create ICO file
)

pause