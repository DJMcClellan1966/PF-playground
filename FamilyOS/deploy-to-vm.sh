#!/bin/bash

# FamilyOS VM Deployment Script
# Run this inside the Ubuntu VM after OS installation

echo "🚀 Starting FamilyOS deployment to VM..."

# Update system
echo "📦 Updating system packages..."
sudo apt update && sudo apt upgrade -y

# Install .NET 8.0
echo "🔧 Installing .NET 8.0..."
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Install additional dependencies
echo "📚 Installing additional dependencies..."
sudo apt install -y git curl wget htop

# Add user to vboxsf group for shared folders
echo "🔗 Setting up shared folder access..."
sudo usermod -aG vboxsf $USER

# Create FamilyOS directory
echo "📁 Creating FamilyOS directory..."
mkdir -p ~/FamilyOS
cd ~/FamilyOS

# Copy FamilyOS files from shared folder (if available)
if [ -d "/media/sf_familyos-share/FamilyOS" ]; then
    echo "📋 Copying FamilyOS files from shared folder..."
    cp -r /media/sf_familyos-share/FamilyOS/* .
    cp -r /media/sf_familyos-share/*.cs .
    cp -r /media/sf_familyos-share/*.csproj .
    cp -r /media/sf_familyos-share/Security.cs .
else
    echo "⚠️  Shared folder not mounted. You'll need to manually copy files."
    echo "   Mount shared folder with: sudo mount -t vboxsf familyos-share /media/sf_familyos-share"
fi

# Create project file if not exists
if [ ! -f "*.csproj" ]; then
    echo "🔧 Creating .NET project..."
    dotnet new console -n FamilyOSTest
    cd FamilyOSTest
    
    # Add required NuGet packages
    echo "📦 Adding required packages..."
    dotnet add package Microsoft.Extensions.Hosting
    dotnet add package Microsoft.Extensions.Logging
    dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
    dotnet add package Microsoft.IdentityModel.Tokens
    dotnet add package System.Security.Cryptography
fi

# Create startup script
echo "📜 Creating startup scripts..."
cat > start-familyos.sh << 'EOF'
#!/bin/bash
echo "🏡 Starting FamilyOS..."
echo "Family Computing Environment - Safe • Secure • Educational"
echo "=========================================================="
dotnet run
EOF
chmod +x start-familyos.sh

# Create monitoring script
cat > monitor-familyos.sh << 'EOF'
#!/bin/bash
echo "📊 FamilyOS System Monitor"
echo "=========================="
echo "CPU Usage:"
top -bn1 | grep "Cpu(s)" | awk '{print $2}' | sed 's/%us,//'
echo "Memory Usage:"
free -h | grep "Mem:"
echo "Network Status:"
ping -c 1 google.com > /dev/null && echo "✅ Internet Connected" || echo "❌ No Internet"
echo "FamilyOS Process:"
pgrep -f "dotnet.*FamilyOS" > /dev/null && echo "✅ FamilyOS Running" || echo "❌ FamilyOS Not Running"
EOF
chmod +x monitor-familyos.sh

# Create desktop shortcut
echo "🖥️  Creating desktop shortcuts..."
mkdir -p ~/Desktop
cat > ~/Desktop/FamilyOS.desktop << 'EOF'
[Desktop Entry]
Version=1.0
Type=Application
Name=FamilyOS
Comment=Family Computing Environment
Exec=gnome-terminal --working-directory=/home/$USER/FamilyOS -e "./start-familyos.sh"
Icon=applications-system
Terminal=true
Categories=System;
EOF
chmod +x ~/Desktop/FamilyOS.desktop

echo "✅ FamilyOS VM deployment complete!"
echo ""
echo "🎯 Next steps:"
echo "1. Restart the VM to activate vboxsf group membership"
echo "2. Run: ./start-familyos.sh to start FamilyOS"
echo "3. Run: ./monitor-familyos.sh to check system status"
echo "4. Access FamilyOS at: http://localhost:5000"
echo ""
echo "👨‍👩‍👧‍👦 Default Family Login Credentials:"
echo "  Parents: mom/parent123, dad/parent123"
echo "  Children: sarah/kid123, alex/teen123"
echo ""
echo "🔒 Security Features Active:"
echo "  • AES-256 Encryption"
echo "  • Content Filtering"
echo "  • Parental Controls"
echo "  • Screen Time Management"
echo "  • Audit Logging"