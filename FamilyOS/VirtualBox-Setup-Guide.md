# 🖥️ FamilyOS VirtualBox Deployment Guide

## Prerequisites
- VirtualBox 7.0+ installed on host machine
- Internet connection for downloading dependencies
- 8GB+ RAM on host machine (recommended)

## (Windows-only) Virtual Machine Setup

FamilyOS is Windows-only. Use a Windows 11 VM for testing.

## Option 2: Windows 11 Setup

### 1. Create Windows VM
```
- Name: FamilyOS-Windows-Test
- Type: Microsoft Windows
- Version: Windows 11 (64-bit)  
- RAM: 6144 MB (6GB)
- Storage: 40 GB VDI
```

### 2. Install Dependencies
```powershell
# Install .NET 8.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version

# Install Git (optional)
winget install Git.Git
```

### 3. Setup FamilyOS
```powershell
# Navigate to shared folder or copy files
cd C:\FamilyOS

# Build and run
dotnet build
dotnet run
```

## Shared Folder Setup

### VirtualBox Shared Folders
1. VM Settings → Shared Folders
2. Add folder from host containing FamilyOS source
3. Mount in guest OS:

**Windows:**
```
Map Network Drive → \\VBOXSVR\familyos-share
```

## Testing Benefits

### 🛡️ **Isolation**
- Complete isolation from host system
- Safe testing of parental controls
- No risk to host family data

### 👨‍👩‍👧‍👦 **Multi-User Testing**
- Test different family member profiles
- Verify age-appropriate restrictions
- Validate screen time controls

### 🔒 **Security Validation**
- Test content filtering in controlled environment
- Verify encryption and data protection
- Validate authentication systems

### 📊 **Performance Testing**
- Monitor resource usage
- Test with limited resources
- Validate performance on older hardware

## VM Network Configuration

### For PocketFence API Integration
```text
If running PocketFence Kernel on host:
- Use Bridged Adapter for direct network access
- Or Port Forwarding: Host 5000 → Guest 5000

VirtualBox Network Settings:
- Adapter 1: NAT (for internet)
- Advanced → Port Forwarding:
  Name: PocketFence-API
  Host Port: 5000
  Guest Port: 5000
```

## Snapshot Strategy

### Create Testing Snapshots
1. **Base OS + .NET** - Clean starting point
2. **FamilyOS Installed** - Ready for testing
3. **With Family Data** - Pre-configured family profiles
4. **Performance Baseline** - Known good state

```powershell
# VirtualBox CLI snapshots (Windows host)
VBoxManage snapshot "FamilyOS-Windows-Test" take "Base-Setup"
VBoxManage snapshot "FamilyOS-Windows-Test" take "FamilyOS-Installed"
VBoxManage snapshot "FamilyOS-Windows-Test" take "Family-Configured"
```

## Automation Script

Windows automation can be done via PowerShell and Chocolatey. Example:
```powershell
# Install .NET 8 SDK (if not installed)
# Download from https://dotnet.microsoft.com/download/dotnet/8.0

# Install Git (optional)
winget install Git.Git

# Verify
dotnet --info
```

## Performance Monitoring

### VM Resource Monitoring
```powershell
# Windows - Monitor FamilyOS performance
taskmgr
resmon
Get-Process -Name "*dotnet*" | Select-Object Name, CPU, WorkingSet
```

### Windows Performance
```powershell
# Task Manager
taskmgr

# Resource Monitor
resmon

# PowerShell monitoring
Get-Process -Name "*dotnet*" | Select-Object Name, CPU, WorkingSet
```

## Security Considerations

### VM Security
- ✅ Isolated from host system
- ✅ Network segmentation available  
- ✅ Snapshot rollback capability
- ✅ Encrypted VM storage option
- ✅ Controlled internet access

### Testing Scenarios
- Test content filtering via API (from inside FamilyOS)
- Login as different family members to validate authentication
- Simulate extended usage to test screen time controls
- Attempt restricted content to verify parental controls

## CI/CD Integration

### Automated VM Testing
```yaml
# GitHub Actions example (Windows runner)
name: FamilyOS VM Tests
on: [push, pull_request]

jobs:
  vm-test:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup VirtualBox
      run: |
        choco install virtualbox -y
        # Additional VM setup automation
```

## Troubleshooting

### Common Issues
| Issue | Solution |
|-------|----------|
| Slow performance | Increase VM RAM, enable VT-x/AMD-V |
| Network issues | Check NAT/Bridged settings |
| Display problems | Install VirtualBox Guest Additions |
| File sharing | Configure shared folders properly |
| .NET errors | Verify SDK installation |

### Debug Commands
```powershell
# Check .NET installation
dotnet --info

# Check FamilyOS build
dotnet build --verbosity normal

# Check network connectivity
Test-NetConnection -ComputerName "localhost" -Port 5000
```

## Production Deployment Notes

### When Ready for Real Deployment
1. Export VM as OVA for distribution
2. Create deployment documentation  
3. Setup automatic updates
4. Configure backup strategies
5. Implement monitoring and logging

---

**VirtualBox provides an excellent isolated testing environment for FamilyOS, allowing safe validation of all family safety features without affecting the host system.** 🖥️🛡️