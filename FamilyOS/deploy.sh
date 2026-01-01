#!/bin/bash
# FamilyOS Production Deployment Script

echo "🚀 FamilyOS Production Deployment"
echo "=================================="
echo

# Function to deploy to a specific platform
deploy_platform() {
    local platform=$1
    local target_dir=$2
    
    echo "📦 Deploying FamilyOS for $platform..."
    
    # Create target directory
    mkdir -p "$target_dir"
    
    # Copy deployment files
    cp -r "./publish/$platform/"* "$target_dir/"
    
    # Copy production configuration
    cp "./publish/appsettings.production.json" "$target_dir/"
    
    # Set executable permissions (for Linux/macOS)
    if [[ "$platform" != "win-x64" ]]; then
        chmod +x "$target_dir/FamilyOS"
    fi
    
    # Create startup script
    if [[ "$platform" == "linux-x64" ]]; then
        cat > "$target_dir/start-familyos.sh" << 'EOF'
#!/bin/bash
export ASPNETCORE_ENVIRONMENT=Production
export FAMILYOS_CONFIG_PATH="./appsettings.production.json"
./FamilyOS
EOF
        chmod +x "$target_dir/start-familyos.sh"
    elif [[ "$platform" == "osx-x64" ]]; then
        cat > "$target_dir/start-familyos.command" << 'EOF'
#!/bin/bash
cd "$(dirname "$0")"
export ASPNETCORE_ENVIRONMENT=Production
export FAMILYOS_CONFIG_PATH="./appsettings.production.json"
./FamilyOS
EOF
        chmod +x "$target_dir/start-familyos.command"
    fi
    
    echo "✅ $platform deployment complete: $target_dir"
}

# Main deployment
echo "🎯 Starting cross-platform deployment..."
echo

# Deploy Windows
deploy_platform "win-x64" "./deployment/windows"

# Deploy Linux
deploy_platform "linux-x64" "./deployment/linux"

# Deploy macOS
deploy_platform "osx-x64" "./deployment/macos"

echo
echo "🏆 FamilyOS Production Deployment Complete!"
echo "============================================"
echo
echo "Deployment locations:"
echo "• Windows: ./deployment/windows/"
echo "• Linux:   ./deployment/linux/"
echo "• macOS:   ./deployment/macos/"
echo
echo "To start FamilyOS in production:"
echo "• Windows: Run FamilyOS.exe"
echo "• Linux:   Run ./start-familyos.sh"
echo "• macOS:   Double-click start-familyos.command"
echo
echo "✅ Ready for production use!"