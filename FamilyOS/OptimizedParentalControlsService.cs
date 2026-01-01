using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PocketFence.FamilyOS.Core;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using FamilyOS;

namespace PocketFence.FamilyOS.Services.Optimized
{
    /// <summary>
    /// CPU and memory optimized parental controls service
    /// Uses caching, pre-computed rules, and efficient data structures
    /// </summary>
    public class OptimizedParentalControlsService : IParentalControls
    {
        private readonly ILogger<OptimizedParentalControlsService> _logger;
        private readonly ISystemSecurity _systemSecurity;
        private readonly Dictionary<string, (object Data, DateTime Expiry)> _cache;
        private readonly ConcurrentDictionary<string, DateTime> _userLoginTimes;
        private readonly ConcurrentDictionary<string, TimeSpan> _dailyScreenTime;
        private readonly Dictionary<AgeGroup, HashSet<string>> _allowedAppsByAge;
        private readonly Dictionary<AgeGroup, HashSet<string>> _blockedAppsByAge;
        private readonly Dictionary<AgeGroup, List<string>> _ageRestrictionCache;
        private bool _isActive;

        // Cache expiry settings
        private const string ACCESS_CACHE_PREFIX = "access_";
        private const string SCREEN_TIME_CACHE_PREFIX = "screen_";
        private const string ENCRYPTED_CACHE_PREFIX = "encrypted_";
        private const string DECRYPTED_CACHE_PREFIX = "decrypted_";
        private static readonly TimeSpan AccessCacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ScreenTimeCacheExpiry = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan EncryptionCacheExpiry = TimeSpan.FromMinutes(30);

        public bool IsActive => _isActive;

        public OptimizedParentalControlsService(ILogger<OptimizedParentalControlsService> logger, ISystemSecurity systemSecurity)
        {
            _logger = logger;
            _systemSecurity = systemSecurity;
            _cache = new Dictionary<string, (object Data, DateTime Expiry)>();
            _userLoginTimes = new ConcurrentDictionary<string, DateTime>();
            _dailyScreenTime = new ConcurrentDictionary<string, TimeSpan>();
            _allowedAppsByAge = new Dictionary<AgeGroup, HashSet<string>>();
            _blockedAppsByAge = new Dictionary<AgeGroup, HashSet<string>>();
            _ageRestrictionCache = new Dictionary<AgeGroup, List<string>>();
            _isActive = false;
            
            // Pre-populate rule sets for O(1) lookups
            InitializeAppRules();
            InitializeAgeRestrictionCache();
        }

        public async Task InitializeAsync()
        {
            _isActive = true;
            _logger.LogInformation("Optimized parental controls initialized and active");
            await Task.CompletedTask;
        }

        public async Task ApplyUserRestrictionsAsync(FamilyMember member)
        {
            _userLoginTimes.AddOrUpdate(member.Id, DateTime.UtcNow, (key, oldValue) => DateTime.UtcNow);
            
            // Use pre-cached restriction descriptions
            var restrictions = _ageRestrictionCache[member.AgeGroup][0];

            _logger.LogDebug($"Applied optimized restrictions for {member.DisplayName}: {restrictions}");
            await _systemSecurity.LogFamilyActivityAsync($"Parental controls applied for {member.DisplayName}", member);
        }

        public async Task<bool> CanAccessAppAsync(FamilyMember member, string appName)
        {
            // Check cache first
            var cacheKey = $"{ACCESS_CACHE_PREFIX}{member.Id}_{appName}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return (bool)cached.Data;
            }

            bool canAccess;

            // Check explicit allow list first (highest priority)
            if (member.AllowedApps.Contains(appName))
            {
                canAccess = true;
            }
            // Check explicit block list
            else if (member.BlockedApps.Contains(appName))
            {
                canAccess = false;
                await _systemSecurity.LogFamilyActivityAsync($"Blocked app access: {appName}", member);
            }
            else
            {
                // Use pre-computed age-based rules
                canAccess = CheckAgeBasedAppAccess(member.AgeGroup, appName);
                
                if (!canAccess)
                {
                    await _systemSecurity.LogFamilyActivityAsync($"Age restriction: {appName} blocked for {member.AgeGroup}", member);
                }
            }

            // Cache the result
            _cache[cacheKey] = (canAccess, DateTime.Now.Add(AccessCacheExpiry));

            return canAccess;
        }

        public async Task<bool> CanAccessUrlAsync(FamilyMember member, string url)
        {
            var cacheKey = $"{ACCESS_CACHE_PREFIX}{member.Id}_{url.GetHashCode():x}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return (bool)cached.Data;
            }

            // Check allow list first (most permissive)
            var urlLower = url.ToLowerInvariant();
            var canAccess = member.AllowedWebsites.Any(allowed => urlLower.Contains(allowed.ToLowerInvariant()));

            if (!canAccess)
            {
                // Use pre-computed blocked domains for age group
                var uri = new Uri(url);
                var domain = uri.Host.ToLowerInvariant();

                if (_blockedAppsByAge.TryGetValue(member.AgeGroup, out var blockedDomains))
                {
                    canAccess = !blockedDomains.Any(blocked => domain.Contains(blocked));
                }
                else
                {
                    canAccess = true; // Default allow for unmapped age groups
                }

                if (!canAccess)
                {
                    await _systemSecurity.LogFamilyActivityAsync($"Blocked URL access: {url}", member);
                }
            }

            // Cache the result
            _cache[cacheKey] = (canAccess, DateTime.Now.Add(AccessCacheExpiry));

            return canAccess;
        }

        public async Task<bool> CanAccessContentAsync(FamilyMember member, string content)
        {
            var contentHash = content.GetHashCode().ToString("x");
            var cacheKey = $"{ACCESS_CACHE_PREFIX}{member.Id}_content_{contentHash}";
            
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.Now)
            {
                return (bool)cached.Data;
            }

            // Use optimized pattern matching
            var canAccess = !ContainsInappropriateContent(content, member.AgeGroup);

            if (!canAccess)
            {
                await _systemSecurity.LogFamilyActivityAsync($"Blocked inappropriate content for {member.AgeGroup}", member);
            }

            // Cache the result
            _cache[cacheKey] = (canAccess, DateTime.Now.Add(AccessCacheExpiry));

            return canAccess;
        }

        public async Task<TimeSpan> GetRemainingScreenTimeAsync(FamilyMember member)
        {
            if (!member.ScreenTime.EnforceScreenTime || member.Role == FamilyRole.Parent)
            {
                return TimeSpan.FromHours(24); // Unlimited for parents
            }

            var cacheKey = $"{SCREEN_TIME_CACHE_PREFIX}{member.Id}_{DateTime.Today:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return (TimeSpan)cached.Data;
            }

            var today = DateTime.Today;
            var isWeekend = today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday;
            
            var dailyLimit = isWeekend ? member.ScreenTime.WeekendLimit : member.ScreenTime.WeekdayLimit;
            var usedTime = GetUsedScreenTimeToday(member.Id);
            var remaining = dailyLimit - usedTime;
            
            var result = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;

            // Cache for a short time since screen time changes frequently
            _cache[cacheKey] = (result, DateTime.UtcNow.Add(ScreenTimeCacheExpiry));

            return result;
        }

        public async Task SaveStateAsync()
        {
            // Use minimal serialization for better performance
            var stateData = new
            {
                UserLoginTimes = _userLoginTimes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                DailyScreenTime = _dailyScreenTime.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TotalMinutes),
                LastSaved = DateTime.UtcNow
            };

            try
            {
                var jsonData = FamilyOSJsonHelper.Serialize(stateData);
                var encryptedData = await _systemSecurity.EncryptFamilyDataAsync(jsonData);
                await File.WriteAllTextAsync("./FamilyData/parental_controls_state.json", encryptedData);
                
                _logger.LogDebug("Optimized parental controls state saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save optimized parental controls state");
            }
        }

        private void InitializeAppRules()
        {
            _allowedAppsByAge[AgeGroup.Toddler] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Educational Hub", "Screen Time Manager"
            };
            _allowedAppsByAge[AgeGroup.Preschool] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Safe Browser", "Educational Hub", "Family Game Center", "Screen Time Manager"
            };
            _allowedAppsByAge[AgeGroup.Elementary] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Safe Browser", "Educational Hub", "Family Game Center", "Family Chat", "Family File Manager", "Screen Time Manager"
            };
            _allowedAppsByAge[AgeGroup.MiddleSchool] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Safe Browser", "Educational Hub", "Family Game Center", "Family Chat", "Family File Manager", "Screen Time Manager"
            };
            _allowedAppsByAge[AgeGroup.HighSchool] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Safe Browser", "Educational Hub", "Family Game Center", "Family Chat", "Family File Manager", "Screen Time Manager"
            };

            _blockedAppsByAge[AgeGroup.Toddler] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "facebook.com", "twitter.com", "instagram.com", "tiktok.com", "reddit.com", "discord.com", "youtube.com"
            };
            _blockedAppsByAge[AgeGroup.Preschool] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "facebook.com", "twitter.com", "instagram.com", "tiktok.com", "reddit.com", "discord.com"
            };
            _blockedAppsByAge[AgeGroup.Elementary] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "facebook.com", "twitter.com", "instagram.com", "tiktok.com", "reddit.com", "discord.com"
            };
            _blockedAppsByAge[AgeGroup.MiddleSchool] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tiktok.com", "reddit.com", "discord.com"
            };
        }

        private void InitializeAgeRestrictionCache()
        {
            _ageRestrictionCache[AgeGroup.Toddler] = new List<string> { "Very restricted - Educational content only, 15-minute sessions" };
            _ageRestrictionCache[AgeGroup.Preschool] = new List<string> { "Highly restricted - Basic educational content, 30-minute sessions" };
            _ageRestrictionCache[AgeGroup.Elementary] = new List<string> { "Restricted - Age-appropriate educational and entertainment content" };
            _ageRestrictionCache[AgeGroup.MiddleSchool] = new List<string> { "Moderate restrictions - Supervised social media and research access" };
            _ageRestrictionCache[AgeGroup.HighSchool] = new List<string> { "Light restrictions - Most content allowed with monitoring" };
            _ageRestrictionCache[AgeGroup.Parent] = new List<string> { "No restrictions - Full administrative access" };
        }

        private bool CheckAgeBasedAppAccess(AgeGroup ageGroup, string appName)
        {
            // Check if app is explicitly allowed for this age group
            if (_allowedAppsByAge.TryGetValue(ageGroup, out var allowedApps))
            {
                return allowedApps.Contains(appName);
            }

            // Default to parent access rules if age group not found
            return ageGroup == AgeGroup.Parent;
        }

        private bool ContainsInappropriateContent(string content, AgeGroup ageGroup)
        {
            var inappropriateKeywords = GetInappropriateKeywordsForAge(ageGroup);
            var lowerContent = content.ToLowerInvariant();

            // Optimized pattern matching
            foreach (var keyword in inappropriateKeywords)
            {
                if (lowerContent.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private HashSet<string> GetInappropriateKeywordsForAge(AgeGroup ageGroup)
        {
            var baseKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "violence", "drugs", "alcohol", "gambling", "inappropriate", "adult"
            };

            return ageGroup switch
            {
                AgeGroup.Toddler or AgeGroup.Preschool => new HashSet<string>(baseKeywords.Concat(new[]
                {
                    "scary", "monster", "nightmare", "fight", "weapon"
                }), StringComparer.OrdinalIgnoreCase),
                
                AgeGroup.Elementary => new HashSet<string>(baseKeywords.Concat(new[]
                {
                    "dating", "romance", "social media", "chat"
                }), StringComparer.OrdinalIgnoreCase),
                
                AgeGroup.MiddleSchool => baseKeywords,
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase) // Minimal filtering for older users
            };
        }

        private TimeSpan GetUsedScreenTimeToday(string userId)
        {
            if (_userLoginTimes.TryGetValue(userId, out var loginTime))
            {
                var today = DateTime.Today;
                if (loginTime.Date == today)
                {
                    return DateTime.UtcNow - loginTime;
                }
            }
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Optimized system security service with reduced CPU overhead
    /// </summary>
    public class OptimizedSystemSecurityService : ISystemSecurity
    {
        private readonly ILogger<OptimizedSystemSecurityService> _logger;
        private readonly Dictionary<string, (object Data, DateTime Expiry)> _cache;
        private readonly string _encryptionKey;
        private readonly ConcurrentQueue<AuditLog> _auditLogQueue;
        private readonly System.Threading.Timer _auditFlushTimer;
        private readonly string _dataPath;
        private readonly SemaphoreSlim _auditSemaphore;

        // Cache settings
        private const string ENCRYPTED_CACHE_PREFIX = "enc_";
        private const string DECRYPTED_CACHE_PREFIX = "dec_";
        private static readonly TimeSpan EncryptionCacheExpiry = TimeSpan.FromMinutes(10);

        public OptimizedSystemSecurityService(ILogger<OptimizedSystemSecurityService> logger, string dataPath = "./FamilyData")
        {
            _logger = logger;
            _cache = new Dictionary<string, (object Data, DateTime Expiry)>();
            _dataPath = dataPath;
            _encryptionKey = GenerateEncryptionKey();
            _auditLogQueue = new ConcurrentQueue<AuditLog>();
            _auditSemaphore = new SemaphoreSlim(1, 1);

            // Batch audit log writes for better performance
            _auditFlushTimer = new System.Threading.Timer(FlushAuditLogs, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            _logger.LogInformation("Optimized system security initialized");
        }

        public async Task<string> EncryptFamilyDataAsync(string data)
        {
            var cacheKey = $"{ENCRYPTED_CACHE_PREFIX}{data.GetHashCode():x}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.Now)
            {
                return (string)cached.Data;
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(_encryptionKey);
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            await swEncrypt.WriteAsync(data);
                        }
                        
                        var result = Convert.ToBase64String(msEncrypt.ToArray());
                        
                        // Cache encrypted result
                        _cache[cacheKey] = (result, DateTime.Now.Add(EncryptionCacheExpiry));
                        
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt family data");
                return data; // Return unencrypted as fallback
            }
        }

        public async Task<string> DecryptFamilyDataAsync(string encryptedData)
        {
            var cacheKey = $"{DECRYPTED_CACHE_PREFIX}{encryptedData.GetHashCode():x}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.Now)
            {
                return (string)cached.Data;
            }

            try
            {
                var fullCipher = Convert.FromBase64String(encryptedData);

                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(_encryptionKey);
                    
                    var iv = new byte[aes.BlockSize / 8];
                    var cipher = new byte[fullCipher.Length - iv.Length];
                    
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);
                    
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        var result = await srDecrypt.ReadToEndAsync();
                        
                        // Cache decrypted result
                        _cache[cacheKey] = (result, DateTime.Now.Add(EncryptionCacheExpiry));
                        
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt family data");
                return encryptedData; // Return original as fallback
            }
        }

        public async Task<bool> VerifyParentPermissionAsync(string parentPin)
        {
            // Cache PIN verification to reduce hashing overhead
            var cacheKey = $"pin_{parentPin.GetHashCode():x}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return (bool)cached.Data;
            }

            var validPins = new[] { "1234", "0000", "9999", "parent" };
            var isValid = validPins.Contains(parentPin);
            
            // Cache the result briefly
            _cache[cacheKey] = (isValid, DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));
            
            await LogFamilyActivityAsync($"Parent permission verification: {(isValid ? "Success" : "Failed")}", 
                new FamilyMember { DisplayName = "System" });
            
            return isValid;
        }

        public async Task<AuditLog> LogFamilyActivityAsync(string activity, FamilyMember member)
        {
            var auditLog = new AuditLog
            {
                MemberId = member.Id,
                Activity = activity,
                Details = $"User: {member.DisplayName}, Age: {member.AgeGroup}",
                Level = DetermineAuditLevel(activity)
            };

            // Queue audit log for batch processing
            _auditLogQueue.Enqueue(auditLog);

            _logger.LogDebug($"[AUDIT] {auditLog.Level}: {activity} - {member.DisplayName}");
            
            return auditLog;
        }

        public async Task<List<AuditLog>> GetFamilyActivityLogsAsync(DateTime fromDate)
        {
            // In a production system, this would query from persistent storage
            // For now, return empty list as logs are batched
            return new List<AuditLog>();
        }

        private string GenerateEncryptionKey()
        {
            // Use a deterministic key for caching effectiveness
            // In production, use proper key management
            return "optimized_family_key_for_performance_testing_only";
        }

        private AuditLevel DetermineAuditLevel(string activity)
        {
            var activityLower = activity.ToLowerInvariant();
            if (activityLower.Contains("blocked"))
                return AuditLevel.Blocked;
            if (activityLower.Contains("failed") || activityLower.Contains("error"))
                return AuditLevel.Security;
            if (activityLower.Contains("warning"))
                return AuditLevel.Warning;
            return AuditLevel.Information;
        }

        private async void FlushAuditLogs(object? state)
        {
            if (_auditLogQueue.IsEmpty) return;

            await _auditSemaphore.WaitAsync();
            
            try
            {
                var logsToFlush = new List<AuditLog>();
                
                // Drain the queue
                while (_auditLogQueue.TryDequeue(out var log) && logsToFlush.Count < 100)
                {
                    logsToFlush.Add(log);
                }

                if (logsToFlush.Count > 0)
                {
                    var auditFile = Path.Combine(_dataPath, "audit_logs.json");
                    var jsonData = FamilyOSJsonHelper.Serialize(logsToFlush);
                    var encryptedData = await EncryptFamilyDataAsync(jsonData);
                    
                    await File.AppendAllTextAsync(auditFile, encryptedData + Environment.NewLine);
                    
                    _logger.LogDebug($"Flushed {logsToFlush.Count} audit logs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush audit logs");
            }
            finally
            {
                _auditSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _auditFlushTimer?.Dispose();
            _auditSemaphore?.Dispose();
        }
    }
}