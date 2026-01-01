# FamilyOS VM Management Script
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("start", "stop", "status", "console", "snapshot", "restore")]
    [string]$Action = "status",
    
    [Parameter(Mandatory=$false)]
    [string]$SnapshotName = "FamilyOS-Clean"
)

$VBoxPath = "C:\Program Files\Oracle\VirtualBox\VBoxManage.exe"
$VMName = "FamilyOS-Test"

function Write-ColorOutput($ForegroundColor, $Message) {
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Show-VMStatus {
    Write-ColorOutput Green "🔍 Checking FamilyOS VM Status..."
    
    try {
        $vmInfo = & $VBoxPath showvminfo $VMName --machinereadable 2>$null
        if ($LASTEXITCODE -eq 0) {
            $state = ($vmInfo | Select-String "VMState=").ToString().Split('=')[1].Trim('"')
            $memory = ($vmInfo | Select-String "memory=").ToString().Split('=')[1]
            $cpus = ($vmInfo | Select-String "cpus=").ToString().Split('=')[1]
            
            Write-ColorOutput Cyan "📊 VM Configuration:"
            Write-Host "   Name: $VMName"
            Write-Host "   State: $state"
            Write-Host "   Memory: $memory MB"
            Write-Host "   CPUs: $cpus"
            
            if ($state -eq "running") {
                Write-ColorOutput Green "✅ FamilyOS VM is running"
                Write-ColorOutput Yellow "🌐 Access FamilyOS at: http://localhost:5000 (from inside VM)"
            } else {
                Write-ColorOutput Yellow "⚠️  FamilyOS VM is $state"
            }
        }
    } catch {
        Write-ColorOutput Red "❌ VM not found or VirtualBox not accessible"
    }
}

function Start-FamilyOSVM {
    Write-ColorOutput Green "🚀 Starting FamilyOS VM..."
    try {
        & $VBoxPath startvm $VMName --type gui
        Start-Sleep 5
        Show-VMStatus
        
        Write-ColorOutput Cyan "🎯 Quick Setup Instructions:"
        Write-Host "1. Install Windows 11 if first time"
        Write-Host "2. Open PowerShell in VM"
        Write-Host "3. Map shared folder: \\VBOXSVR\\familyos-share"
        Write-Host "4. Build: dotnet build; Run: dotnet run"
        Write-Host "5. Verify: FamilyOS starts and logs appear"
    } catch {
        Write-ColorOutput Red "❌ Failed to start VM"
    }
}

function Stop-FamilyOSVM {
    Write-ColorOutput Yellow "🛑 Stopping FamilyOS VM..."
    try {
        & $VBoxPath controlvm $VMName savestate
        Write-ColorOutput Green "✅ VM state saved and stopped"
    } catch {
        Write-ColorOutput Red "❌ Failed to stop VM"
    }
}

function Open-VMConsole {
    Write-ColorOutput Green "🖥️  Opening VM console..."
    try {
        & $VBoxPath startvm $VMName --type gui
    } catch {
        Write-ColorOutput Red "❌ Failed to open VM console"
    }
}

function New-VMSnapshot {
    Write-ColorOutput Green "📸 Creating VM snapshot: $SnapshotName"
    try {
        & $VBoxPath snapshot $VMName take $SnapshotName --description "FamilyOS snapshot created $(Get-Date)"
        Write-ColorOutput Green "✅ Snapshot '$SnapshotName' created successfully"
    } catch {
        Write-ColorOutput Red "❌ Failed to create snapshot"
    }
}

function Restore-VMSnapshot {
    Write-ColorOutput Yellow "🔄 Restoring VM snapshot: $SnapshotName"
    try {
        & $VBoxPath snapshot $VMName restore $SnapshotName
        Write-ColorOutput Green "✅ Snapshot '$SnapshotName' restored successfully"
    } catch {
        Write-ColorOutput Red "❌ Failed to restore snapshot"
    }
}

# Main script logic
Write-ColorOutput Cyan "🏡 FamilyOS VM Management Console"
Write-ColorOutput Cyan "================================="

switch ($Action.ToLower()) {
    "start" { Start-FamilyOSVM }
    "stop" { Stop-FamilyOSVM }
    "status" { Show-VMStatus }
    "console" { Open-VMConsole }
    "snapshot" { New-VMSnapshot }
    "restore" { Restore-VMSnapshot }
    default { 
        Write-ColorOutput Red "❌ Invalid action: $Action"
        Write-Host "Valid actions: start, stop, status, console, snapshot, restore"
    }
}

Write-ColorOutput Green "`n✨ FamilyOS VM Management Complete"