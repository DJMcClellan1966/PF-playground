using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace PocketFence.FamilyOS.Stealth
{
    /// <summary>
    /// USB Dongle Control System - Hardware-based authentication and automatic child mode activation
    /// </summary>
    public class UsbDongleControlSystem
    {
        private readonly ILogger<UsbDongleControlSystem> _logger;
        private bool _isActive;
        private readonly Dictionary<string, ParentProfile> _authorizedDongles;
        private readonly HashSet<string> _connectedDongles;
        private ManagementEventWatcher _usbWatcher;
        private System.Threading.Timer _dongleMonitorTimer;
        private bool _isParentModeActive;

        public UsbDongleControlSystem(ILogger<UsbDongleControlSystem> logger)
        {
            _logger = logger;
            _authorizedDongles = new Dictionary<string, ParentProfile>();
            _connectedDongles = new HashSet<string>();
            _isParentModeActive = false;
        }

        [SupportedOSPlatform("windows")]
        public async Task ActivateUsbControlSystemAsync()
        {
            try
            {
                _logger.LogInformation("🔐 Activating USB Dongle Control System...");

                // Initialize authorized parent dongles
                await InitializeAuthorizedDonglesAsync();

                // Start USB device monitoring
                await StartUsbMonitoringAsync();

                // Check for existing dongles
                await ScanForExistingUsbDevicesAsync();

                // Start periodic dongle verification
                await StartDongleVerificationAsync();

                _isActive = true;
                _logger.LogInformation("✅ USB dongle control system active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to activate USB dongle control system");
                throw;
            }
        }

        private async Task InitializeAuthorizedDonglesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Load or create authorized dongle database
                    var donglesPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "FamilyOS", "Security", "authorized_dongles.dat");

                    var donglesDir = Path.GetDirectoryName(donglesPath);
                    if (!string.IsNullOrEmpty(donglesDir))
                    {
                        Directory.CreateDirectory(donglesDir);
                    }

                    if (File.Exists(donglesPath))
                    {
                        LoadAuthorizedDonglesFromFile(donglesPath);
                    }
                    else
                    {
                        CreateDefaultAuthorizedDongles();
                        SaveAuthorizedDonglesToFile(donglesPath);
                    }

                    _logger.LogDebug($"Initialized {_authorizedDongles.Count} authorized dongles");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize authorized dongles, using defaults");
                    CreateDefaultAuthorizedDongles();
                }
            });
        }

        private void CreateDefaultAuthorizedDongles()
        {
            // Create some default parent profiles with fake but realistic USB device IDs
            var defaultProfiles = new[]
            {
                new ParentProfile
                {
                    Name = "Parent 1",
                    DongleId = GenerateDeviceId("SanDisk_Ultra_3.0"),
                    PermissionLevel = ParentPermissionLevel.Full,
                    TimeRestrictions = new TimeRestriction { StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(24) }
                },
                new ParentProfile
                {
                    Name = "Parent 2", 
                    DongleId = GenerateDeviceId("Kingston_DataTraveler"),
                    PermissionLevel = ParentPermissionLevel.Restricted,
                    TimeRestrictions = new TimeRestriction { StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(22) }
                },
                new ParentProfile
                {
                    Name = "Emergency Access",
                    DongleId = GenerateDeviceId("Verbatim_Store_n_Go"),
                    PermissionLevel = ParentPermissionLevel.Emergency,
                    TimeRestrictions = new TimeRestriction { StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(24) }
                }
            };

            foreach (var profile in defaultProfiles)
            {
                _authorizedDongles[profile.DongleId] = profile;
            }
        }

        private string GenerateDeviceId(string deviceName)
        {
            // Generate a realistic device ID hash
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{deviceName}_{DateTime.UtcNow:yyyyMMdd}"));
            return Convert.ToHexString(hash)[..16].ToUpper();
        }

        private void LoadAuthorizedDonglesFromFile(string filePath)
        {
            try
            {
                var encryptedData = File.ReadAllBytes(filePath);
                var decryptedData = DecryptData(encryptedData);
                var lines = Encoding.UTF8.GetString(decryptedData).Split('\n');

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('|');
                    if (parts.Length >= 4)
                    {
                        var profile = new ParentProfile
                        {
                            Name = parts[0],
                            DongleId = parts[1],
                            PermissionLevel = Enum.Parse<ParentPermissionLevel>(parts[2]),
                            TimeRestrictions = ParseTimeRestriction(parts[3])
                        };
                        
                        _authorizedDongles[profile.DongleId] = profile;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load authorized dongles from file");
                throw;
            }
        }

        private void SaveAuthorizedDonglesToFile(string filePath)
        {
            try
            {
                var lines = new List<string>();
                foreach (var profile in _authorizedDongles.Values)
                {
                    lines.Add($"{profile.Name}|{profile.DongleId}|{profile.PermissionLevel}|{FormatTimeRestriction(profile.TimeRestrictions)}");
                }

                var data = Encoding.UTF8.GetBytes(string.Join('\n', lines));
                var encryptedData = EncryptData(data);
                File.WriteAllBytes(filePath, encryptedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save authorized dongles to file");
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task StartUsbMonitoringAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Monitor USB device insertion/removal
                    _usbWatcher = new ManagementEventWatcher("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
                    _usbWatcher.EventArrived += OnUsbDeviceChanged;
                    _usbWatcher.Start();

                    _logger.LogDebug("USB monitoring started");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start USB monitoring");
                }
            });
        }

        [SupportedOSPlatform("windows")]
        private async void OnUsbDeviceChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var eventType = (uint)e.NewEvent["EventType"];
                var driveName = e.NewEvent["DriveName"]?.ToString();

                if (eventType == 2) // Device inserted
                {
                    _logger.LogInformation($"🔌 USB device inserted: {driveName}");
                    if (!string.IsNullOrEmpty(driveName))
                    {
                        await HandleUsbDeviceInsertedAsync(driveName);
                    }
                }
                else if (eventType == 3) // Device removed
                {
                    _logger.LogInformation($"🔌 USB device removed: {driveName}");
                    if (!string.IsNullOrEmpty(driveName))
                    {
                        await HandleUsbDeviceRemovedAsync(driveName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling USB device change");
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task HandleUsbDeviceInsertedAsync(string driveName)
        {
            try
            {
                if (string.IsNullOrEmpty(driveName)) return;

                // Get device details
                var deviceId = await GetUsbDeviceIdAsync(driveName);
                if (string.IsNullOrEmpty(deviceId)) return;

                _connectedDongles.Add(deviceId);

                // Check if this is an authorized parent dongle
                if (_authorizedDongles.TryGetValue(deviceId, out var parentProfile))
                {
                    await HandleAuthorizedDongleInsertedAsync(parentProfile);
                }
                else
                {
                    await HandleUnauthorizedDeviceInsertedAsync(deviceId, driveName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling USB device insertion");
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task<string> GetUsbDeviceIdAsync(string driveName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '{driveName}'");
                
                foreach (ManagementObject disk in searcher.Get())
                {
                    var volumeSerialNumber = disk["VolumeSerialNumber"]?.ToString();
                    var size = disk["Size"]?.ToString();
                    var model = disk["VolumeName"]?.ToString() ?? "Unknown";

                    // Create a unique device ID based on multiple properties
                    var deviceInfo = $"{model}_{volumeSerialNumber}_{size}";
                    return GenerateDeviceId(deviceInfo);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to get USB device ID: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task HandleAuthorizedDongleInsertedAsync(ParentProfile parentProfile)
        {
            try
            {
                _logger.LogInformation($"👨‍👩‍👧‍👦 Authorized parent dongle detected: {parentProfile.Name}");

                // Check time restrictions
                if (!IsWithinAllowedTime(parentProfile.TimeRestrictions))
                {
                    _logger.LogWarning($"⏰ Parent dongle outside allowed time window: {parentProfile.Name}");
                    await ShowTimeRestrictionMessageAsync(parentProfile);
                    return;
                }

                // Activate parent mode
                await ActivateParentModeAsync(parentProfile);

                // Show parent access granted notification
                await ShowParentAccessGrantedAsync(parentProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling authorized dongle insertion");
            }
        }

        private bool IsWithinAllowedTime(TimeRestriction timeRestriction)
        {
            var currentTime = DateTime.Now.TimeOfDay;
            
            if (timeRestriction.StartTime <= timeRestriction.EndTime)
            {
                return currentTime >= timeRestriction.StartTime && currentTime <= timeRestriction.EndTime;
            }
            else
            {
                // Overnight period (e.g., 22:00 to 06:00)
                return currentTime >= timeRestriction.StartTime || currentTime <= timeRestriction.EndTime;
            }
        }

        private async Task ActivateParentModeAsync(ParentProfile parentProfile)
        {
            try
            {
                _isParentModeActive = true;
                
                _logger.LogInformation($"🔓 Activating parent mode for: {parentProfile.Name}");

                // Disable all stealth enforcement systems temporarily
                await DisableStealthEnforcementAsync();

                // Grant appropriate permissions based on parent profile
                await GrantParentPermissionsAsync(parentProfile);

                // Start parent session monitoring
                await StartParentSessionMonitoringAsync();

                _logger.LogInformation("✅ Parent mode activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate parent mode");
            }
        }

        private async Task DisableStealthEnforcementAsync()
        {
            try
            {
                _logger.LogInformation("🔧 Temporarily disabling stealth enforcement systems...");

                // This would disable other stealth systems while parent is present
                // - Desktop impersonation
                // - Honeypot bypasses  
                // - DNS hijacking (partially)
                // - Invisible watchdogs (reduce activity)

                await Task.Delay(1000); // Simulate system changes
                _logger.LogDebug("Stealth enforcement temporarily disabled for parent access");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disable some stealth enforcement systems");
            }
        }

        private async Task GrantParentPermissionsAsync(ParentProfile parentProfile)
        {
            try
            {
                switch (parentProfile.PermissionLevel)
                {
                    case ParentPermissionLevel.Full:
                        await GrantFullPermissionsAsync();
                        break;
                    case ParentPermissionLevel.Restricted:
                        await GrantRestrictedPermissionsAsync();
                        break;
                    case ParentPermissionLevel.Emergency:
                        await GrantEmergencyPermissionsAsync();
                        break;
                }

                _logger.LogInformation($"Granted {parentProfile.PermissionLevel} permissions to {parentProfile.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to grant parent permissions");
            }
        }

        private async Task GrantFullPermissionsAsync()
        {
            // Full access - disable all restrictions
            await Task.Delay(100);
            _logger.LogDebug("Full parent permissions granted");
        }

        private async Task GrantRestrictedPermissionsAsync()
        {
            // Limited access - maintain some restrictions
            await Task.Delay(100);
            _logger.LogDebug("Restricted parent permissions granted");
        }

        private async Task GrantEmergencyPermissionsAsync()
        {
            // Emergency access - basic functionality only
            await Task.Delay(100);
            _logger.LogDebug("Emergency parent permissions granted");
        }

        [SupportedOSPlatform("windows")]
        private async Task HandleUsbDeviceRemovedAsync(string driveName)
        {
            try
            {
                var deviceId = await GetUsbDeviceIdAsync(driveName);
                if (deviceId != null)
                {
                    _connectedDongles.Remove(deviceId);

                    // Check if this was an authorized parent dongle
                    if (_authorizedDongles.ContainsKey(deviceId))
                    {
                        await HandleParentDongleRemovedAsync(deviceId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling USB device removal");
            }
        }

        private async Task HandleParentDongleRemovedAsync(string deviceId)
        {
            try
            {
                var parentProfile = _authorizedDongles[deviceId];
                _logger.LogInformation($"👨‍👩‍👧‍👦 Parent dongle removed: {parentProfile.Name}");

                // Wait a few seconds in case they're just reinserting it
                await Task.Delay(5000);

                // Check if the dongle is still gone
                if (!_connectedDongles.Contains(deviceId))
                {
                    await DeactivateParentModeAsync();
                    await ReactivateChildModeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling parent dongle removal");
            }
        }

        private async Task DeactivateParentModeAsync()
        {
            try
            {
                _isParentModeActive = false;
                
                _logger.LogInformation("🔒 Deactivating parent mode - child protection resuming");

                // Re-enable all stealth enforcement systems
                await ReactivateStealthEnforcementAsync();

                _logger.LogInformation("✅ Parent mode deactivated, child protection active");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate parent mode");
            }
        }

        private async Task ReactivateChildModeAsync()
        {
            try
            {
                _logger.LogInformation("👶 Reactivating child protection mode...");

                // Show transition message
                await ShowChildModeReactivatedAsync();

                _logger.LogInformation("✅ Child protection mode fully reactivated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reactivate child mode");
            }
        }

        private async Task ReactivateStealthEnforcementAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Reactivating stealth enforcement systems...");

                // This would re-enable other stealth systems
                // - Desktop impersonation
                // - Honeypot bypasses
                // - DNS hijacking
                // - Invisible watchdogs

                await Task.Delay(2000); // Simulate system reactivation
                _logger.LogDebug("Stealth enforcement systems reactivated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reactivate some stealth enforcement systems");
            }
        }

        private async Task HandleUnauthorizedDeviceInsertedAsync(string deviceId, string driveName)
        {
            _logger.LogWarning($"⚠️ Unauthorized USB device detected: {driveName} ({deviceId})");

            // Log the unauthorized attempt
            await LogSecurityEventAsync($"Unauthorized USB device insertion: {driveName}", SecurityEventLevel.Warning);

            // Could implement additional security measures here:
            // - Disable USB device
            // - Alert parents
            // - Increase monitoring
        }

        [SupportedOSPlatform("windows")]
        private async Task StartDongleVerificationAsync()
        {
            _dongleMonitorTimer = new System.Threading.Timer(async _ => await VerifyConnectedDonglesAsync(),
                null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            
            await Task.CompletedTask;
        }

        [SupportedOSPlatform("windows")]
        private async Task VerifyConnectedDonglesAsync()
        {
            try
            {
                // Periodically verify that connected dongles are still present and valid
                var currentDevices = await GetCurrentUsbDevicesAsync();
                
                // Remove dongles that are no longer connected
                var disconnectedDongles = _connectedDongles.Except(currentDevices).ToList();
                foreach (var dongleId in disconnectedDongles)
                {
                    _connectedDongles.Remove(dongleId);
                    if (_authorizedDongles.ContainsKey(dongleId))
                    {
                        await HandleParentDongleRemovedAsync(dongleId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error during dongle verification: {ex.Message}");
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task<List<string>> GetCurrentUsbDevicesAsync()
        {
            var currentDevices = new List<string>();
            
            try
            {
                await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 2");
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var driveId = disk["DeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(driveId))
                        {
                            var deviceId = GetUsbDeviceIdAsync(driveId).Result;
                            if (!string.IsNullOrEmpty(deviceId))
                            {
                                currentDevices.Add(deviceId);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to get current USB devices: {ex.Message}");
            }

            return currentDevices;
        }

        [SupportedOSPlatform("windows")]
        private async Task ScanForExistingUsbDevicesAsync()
        {
            try
            {
                _logger.LogInformation("Scanning for existing USB devices...");
                
                var existingDevices = await GetCurrentUsbDevicesAsync();
                
                foreach (var deviceId in existingDevices)
                {
                    _connectedDongles.Add(deviceId);
                    
                    if (_authorizedDongles.TryGetValue(deviceId, out var parentProfile))
                    {
                        _logger.LogInformation($"Found existing authorized dongle: {parentProfile.Name}");
                        
                        // Check if this should activate parent mode
                        if (IsWithinAllowedTime(parentProfile.TimeRestrictions))
                        {
                            await ActivateParentModeAsync(parentProfile);
                        }
                    }
                }
                
                _logger.LogInformation($"Scan complete. Found {_connectedDongles.Count} USB device(s)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning existing USB devices");
            }
        }

        private async Task StartParentSessionMonitoringAsync()
        {
            // Monitor parent session for security
            await Task.Delay(100);
            _logger.LogDebug("Parent session monitoring started");
        }

        private async Task ShowParentAccessGrantedAsync(ParentProfile parentProfile)
        {
            // Show notification that parent access has been granted
            await Task.Delay(100);
            _logger.LogInformation($"Parent access notification shown for: {parentProfile.Name}");
        }

        private async Task ShowTimeRestrictionMessageAsync(ParentProfile parentProfile)
        {
            // Show message about time restrictions
            await Task.Delay(100);
            _logger.LogInformation($"Time restriction message shown for: {parentProfile.Name}");
        }

        private async Task ShowChildModeReactivatedAsync()
        {
            // Show notification that child mode has been reactivated
            await Task.Delay(100);
            _logger.LogInformation("Child mode reactivation notification shown");
        }

        private async Task LogSecurityEventAsync(string eventDescription, SecurityEventLevel level)
        {
            try
            {
                var logEntry = new SecurityEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Description = eventDescription,
                    Level = level,
                    Source = "USB Dongle Control"
                };

                // In a real implementation, this would log to a secure event log
                _logger.LogWarning($"Security Event: {eventDescription}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event");
            }
        }

        // Encryption helpers
        private byte[] EncryptData(byte[] data)
        {
            // Simple encryption for demo - in production use proper encryption
            var key = Encoding.UTF8.GetBytes("FamilyOS_Security_Key_32_Chars!!");
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key[i % key.Length];
            }
            return data;
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            // Simple decryption - matches encryption above
            return EncryptData(encryptedData);
        }

        private TimeRestriction ParseTimeRestriction(string timeStr)
        {
            var parts = timeStr.Split('-');
            return new TimeRestriction
            {
                StartTime = TimeSpan.Parse(parts[0]),
                EndTime = TimeSpan.Parse(parts[1])
            };
        }

        private string FormatTimeRestriction(TimeRestriction timeRestriction)
        {
            return $"{timeRestriction.StartTime:hh\\:mm}-{timeRestriction.EndTime:hh\\:mm}";
        }

        public async Task<bool> IsParentDongleConnectedAsync()
        {
            return _connectedDongles.Any(dongleId => _authorizedDongles.ContainsKey(dongleId));
        }

        public async Task<List<string>> GetAuthorizedParentsAsync()
        {
            return _authorizedDongles.Values.Select(p => p.Name).ToList();
        }

        [SupportedOSPlatform("windows")]
        public async Task DeactivateUsbControlSystemAsync()
        {
            try
            {
                _isActive = false;
                _usbWatcher?.Stop();
                _usbWatcher?.Dispose();
                _dongleMonitorTimer?.Dispose();

                _logger.LogInformation("USB dongle control system deactivated");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate USB control system");
            }
        }

        public bool IsActive => _isActive;
        public bool IsParentModeActive => _isParentModeActive;
        public int AuthorizedDonglesCount => _authorizedDongles.Count;
        public int ConnectedDonglesCount => _connectedDongles.Count;
    }

    // Supporting classes
    public class ParentProfile
    {
        public string Name { get; set; }
        public string DongleId { get; set; }
        public ParentPermissionLevel PermissionLevel { get; set; }
        public TimeRestriction TimeRestrictions { get; set; }
    }

    public class TimeRestriction
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public enum ParentPermissionLevel
    {
        Emergency,
        Restricted, 
        Full
    }

    public class SecurityEvent
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public SecurityEventLevel Level { get; set; }
        public string Source { get; set; }
    }

    public enum SecurityEventLevel
    {
        Info,
        Warning,
        Critical
    }
}