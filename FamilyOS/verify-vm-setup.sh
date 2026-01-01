#!/bin/bash

echo "🔍 FamilyOS Mount and Deployment Verification"
echo "============================================="

# Check if shared folder is mounted
if [ -d "/media/sf_familyos-share" ]; then
    echo "✅ Shared folder mount point exists"
    
    if mountpoint -q /media/sf_familyos-share; then
        echo "✅ Shared folder is mounted"
        
        # Check for FamilyOS files
        if [ -f "/media/sf_familyos-share/FamilyOS/deploy-to-vm.sh" ]; then
            echo "✅ FamilyOS deployment script found"
        else
            echo "❌ FamilyOS deployment script not found"
            echo "   Expected: /media/sf_familyos-share/FamilyOS/deploy-to-vm.sh"
        fi
        
        if [ -f "/media/sf_familyos-share/FamilyOS/FamilyOS.Core.cs" ]; then
            echo "✅ FamilyOS core files found"
        else
            echo "❌ FamilyOS core files not found"
        fi
        
        # List shared folder contents
        echo ""
        echo "📁 Shared folder contents:"
        ls -la /media/sf_familyos-share/
        
    else
        echo "❌ Shared folder not mounted"
        echo "   Run: sudo mount -t vboxsf familyos-share /media/sf_familyos-share"
    fi
else
    echo "❌ Shared folder mount point missing"
    echo "   Run: sudo mkdir -p /media/sf_familyos-share"
fi

# Check user groups
echo ""
echo "👤 User group membership:"
groups $USER | grep -q vboxsf && echo "✅ User in vboxsf group" || echo "❌ User not in vboxsf group (run: sudo usermod -aG vboxsf $USER)"

# Check if .NET is available
echo ""
echo "🔧 Development environment:"
if command -v dotnet &> /dev/null; then
    echo "✅ .NET is installed: $(dotnet --version)"
else
    echo "❌ .NET not installed"
fi

echo ""
echo "🎯 Ready for FamilyOS deployment!"