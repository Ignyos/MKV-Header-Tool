# MKV Header Tool Icons

This directory contains the icon resources for the MKV Header Tool application.

## Icon Files Created

### Application Icons
- `mkv-logo.svg` - Main application logo (64x64)
- `mkv-icon-64.svg` - Enhanced application icon with shadow and gradient
- `systray-icon.svg` - System tray optimized icon (16x16)
- `systray-32.svg` - Medium size system tray icon (32x32)

### Icon Design
The icons feature:
- **Blue gradient background** - Modern, professional look
- **"MKV" text** - Clear application identification
- **Track bars** - Visual representation of video/audio/subtitle tracks
  - Yellow/Orange bar: Video track
  - Green bars: Audio tracks
  - Purple bar: Subtitle track
- **Gear icon** - Indicates editing/modification capability

## Converting to ICO Format

To create Windows ICO files for system tray use:

### Option 1: Using PowerShell Script
```powershell
.\Scripts\create-icon.ps1
```
Requires ImageMagick to be installed.

### Option 2: Manual Conversion
1. Use an online SVG to ICO converter
2. Convert `systray-icon.svg` to ICO format with multiple sizes (16x16, 32x32, 48x48)
3. Save as `Resources\mkv-icon.ico`

### Option 3: Using ImageMagick (Command Line)
```bash
magick systray-icon.svg -resize 16x16 temp16.png
magick systray-icon.svg -resize 32x32 temp32.png
magick systray-icon.svg -resize 48x48 temp48.png
magick temp16.png temp32.png temp48.png mkv-icon.ico
```

## Implementation

The system tray icon is implemented in:
- `Platforms\Windows\SystemTrayHelper.cs` - System tray functionality
- `Platforms\Windows\WindowsHelper.cs` - Window configuration with icon

The icon will be automatically applied to:
- Application window title bar
- Windows taskbar
- System tray (when minimized)

## Color Scheme

- **Primary Blue**: #2563eb (Main background)
- **Dark Blue**: #1e40af (Borders, accents)
- **Video Track**: #f59e0b (Orange/Yellow)
- **Audio Tracks**: #10b981, #059669 (Green variants)
- **Subtitle Track**: #8b5cf6 (Purple)
- **Text**: White with shadow for readability