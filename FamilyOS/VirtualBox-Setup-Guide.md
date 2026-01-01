# 🖥️ FamilyOS VirtualBox Deployment Guide

## Prerequisites
- VirtualBox 7.0+ installed on host machine
- Internet connection for downloading dependencies
- 8GB+ RAM on host machine (recommended)

## Option 1: Ubuntu 22.04 LTS Setup (Recommended)

### 1. Create Ubuntu VM
```bash
# VM Settings
- Name: FamilyOS-Ubuntu-Test
- Type: Linux
- Version: Ubuntu (64-bit)
- RAM: 4096 MB (4GB)
- Storage: 25 GB VDI (dynamically allocated)
```

### 2. Install Ubuntu and Dependencies
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET 8.0
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Verify installation
dotnet --version
```

### 3. Install FamilyOS
```bash
# Clone or copy FamilyOS files
mkdir ~/familyos
cd ~/familyos

# Copy FamilyOS source files here
# (Transfer via shared folder, USB, or git clone)

# Build and run
dotnet build
dotnet run
```

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

**Ubuntu:**
```bash
sudo mkdir /mnt/familyos
sudo mount -t vboxsf familyos-share /mnt/familyos
cd /mnt/familyos/FamilyOS
```

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
```bash
# If running PocketFence Kernel on host:
# Use Bridged Adapter for direct network access
# Or Port Forwarding: Host 5000 → Guest 5000

# VirtualBox Network Settings:
# Adapter 1: NAT (for internet)
# Advanced → Port Forwarding:
#   Name: PocketFence-API
#   Host Port: 5000
#   Guest Port: 5000
```

## Snapshot Strategy

### Create Testing Snapshots
1. **Base OS + .NET** - Clean starting point
2. **FamilyOS Installed** - Ready for testing
3. **With Family Data** - Pre-configured family profiles
4. **Performance Baseline** - Known good state

```bash
# VirtualBox CLI snapshots
VBoxManage snapshot "FamilyOS-Ubuntu-Test" take "Base-Setup"
VBoxManage snapshot "FamilyOS-Ubuntu-Test" take "FamilyOS-Installed"
VBoxManage snapshot "FamilyOS-Ubuntu-Test" take "Family-Configured"
```

## Automation Script

### Ubuntu Auto-Setup Script
```bash
#!/bin/bash
# save as setup-familyos.sh

echo "🏠 Setting up FamilyOS Testing Environment..."

# Update system
sudo apt update -y

# Install .NET 8.0
wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Install additional tools
sudo apt install -y git curl wget

# Create FamilyOS directory
mkdir -p ~/familyos-test
cd ~/familyos-test

echo "✅ Environment ready for FamilyOS deployment!"
echo "📁 Working directory: $(pwd)"
echo "🔧 .NET Version: $(dotnet --version)"
```

## Performance Monitoring

### VM Resource Monitoring
```bash
# Ubuntu - Monitor FamilyOS performance
sudo apt install htop
htop

# Check .NET process
ps aux | grep dotnet

# Memory usage
free -h

# Disk usage
df -h
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
```bash
# Test content filtering
curl -X POST http://localhost:5000/api/filter/url \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.com"}'

# Test family authentication
# Login as different family members

# Test screen time controls
# Simulate extended usage periods

# Test parental controls
# Attempt access to restricted content
```

## CI/CD Integration

### Automated VM Testing
```yaml
# GitHub Actions example
name: FamilyOS VM Tests
on: [push, pull_request]

jobs:
  vm-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup VirtualBox
      run: |
        sudo apt-get install virtualbox
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
```bash
# Check .NET installation
dotnet --info

# Check FamilyOS build
dotnet build --verbosity normal

# Check network connectivity
ping google.com
curl http://localhost:5000/api/kernel/health
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