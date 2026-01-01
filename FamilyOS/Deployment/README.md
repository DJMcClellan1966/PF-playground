# FamilyOS Installation & Deployment

This directory contains all the files needed to create a professional installation package for FamilyOS that families can easily install on Windows systems.

## 📦 Contents

### Installation Scripts
- `Install-FamilyOS.ps1` - Main installation script
- `Create-Installer.ps1` - Builds the installation package
- `Uninstall-FamilyOS.ps1` - Clean removal script

### Deployment Files  
- `FamilyOS.iss` - Inno Setup installer configuration
- `install-icon.ico` - Installation icon
- `FamilyOS.exe.manifest` - Windows application manifest

### Setup Files
- `README-INSTALLATION.md` - Instructions for families
- `SYSTEM-REQUIREMENTS.md` - Minimum system requirements

## 🚀 Quick Build

To create the installer package:

```powershell
.\Create-Installer.ps1
```

This will create `FamilyOS-Setup.exe` ready for distribution to families.

## 📋 Installation Features

✅ **Self-Contained** - No .NET installation required  
✅ **Windows Integration** - Start Menu shortcuts and desktop icons  
✅ **Clean Uninstall** - Complete removal support  
✅ **Family-Friendly** - Simple one-click installation  
✅ **Professional** - Digitally signed and Windows-compliant  

## 🎯 For Families

Just run `FamilyOS-Setup.exe` and follow the simple wizard!