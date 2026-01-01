#!/bin/bash
# FamilyOS Production Startup Script for Linux

echo "🏡 Starting FamilyOS in Production Mode"
echo "====================================="
echo

# Set production environment
export ASPNETCORE_ENVIRONMENT=Production
export FAMILYOS_CONFIG_PATH="./appsettings.production.json"

echo "✅ Production environment configured"
echo "🚀 Launching FamilyOS..."
echo

# Make executable if needed
chmod +x ./FamilyOS

# Start FamilyOS
./FamilyOS

exit_code=$?
echo
if [ $exit_code -eq 0 ]; then
    echo "✅ FamilyOS shut down successfully"
else
    echo "❌ FamilyOS encountered an error (Exit Code: $exit_code)"
fi