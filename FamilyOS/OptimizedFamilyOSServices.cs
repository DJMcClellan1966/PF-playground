using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PocketFence.FamilyOS.Core;
using System.Buffers;
using System.Linq;
using FamilyOS;

namespace PocketFence.FamilyOS.Services.Optimized
{
    /// <summary>
    /// Memory and CPU optimized family manager service
    /// Implements caching, object pooling, and efficient data structures
    /// </summary>
    public class OptimizedFamilyManagerService : IFamilyManager
    {
        private readonly ILogger<OptimizedFamilyManagerService> _logger;
        private readonly ISystemSecurity _systemSecurity;
        private readonly Dictionary<string, (object Data, DateTime Expiry)> _cache;
        private readonly string _dataPath;
        private readonly ConcurrentDictionary<string, FamilyMember> _familyMembersIndex;
        private readonly ObjectPool<StringBuilder> _stringBuilderPool;
        private readonly SemaphoreSlim _saveSemaphore;
        
        // Cache keys
        private const string FAMILY_MEMBERS_CACHE_KEY = "family_members";
        private const string AUTH_CACHE_PREFIX = "auth_";
        private static readonly TimeSpan AuthCacheExpiry = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan FamilyDataCacheExpiry = TimeSpan.FromMinutes(30);

        public OptimizedFamilyManagerService(
            ILogger<OptimizedFamilyManagerService> logger, 
            ISystemSecurity systemSecurity,
            string dataPath = "./FamilyData")
        {
            _logger = logger;
            _systemSecurity = systemSecurity;
            _cache = new Dictionary<string, (object Data, DateTime Expiry)>();
            _dataPath = dataPath;
            _familyMembersIndex = new ConcurrentDictionary<string, FamilyMember>();
            _stringBuilderPool = new ObjectPool<StringBuilder>(() => new StringBuilder(256));
            _saveSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task LoadFamilyProfilesAsync()
        {
            // Check cache first
            if (_cache.TryGetValue(FAMILY_MEMBERS_CACHE_KEY, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                var cachedMembers = (List<FamilyMember>)cached.Data;
                UpdateIndex(cachedMembers);
                _logger.LogDebug("Loaded family profiles from cache");
                return;
            }

            try
            {
                var profilesFile = Path.Combine(_dataPath, "family_profiles.json");
                
                if (!Directory.Exists(_dataPath))
                {
                    Directory.CreateDirectory(_dataPath);
                }

                if (File.Exists(profilesFile))
                {
                    // Use async file operations with buffer pooling
                    using var fileStream = new FileStream(profilesFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);
                    
                    var encryptedData = await reader.ReadToEndAsync();
                    var decryptedData = await _systemSecurity.DecryptFamilyDataAsync(encryptedData);
                    
                    var profiles = FamilyOSJsonHelper.Deserialize<List<FamilyMember>>(decryptedData);
                    
                    if (profiles?.Count > 0)
                    {
                        UpdateIndex(profiles);
                        
                        // Cache the loaded data
                        _cache[FAMILY_MEMBERS_CACHE_KEY] = (profiles, DateTime.UtcNow.Add(FamilyDataCacheExpiry));
                        
                        _logger.LogInformation($"Loaded {profiles.Count} family member profiles (optimized)");
                    }
                }
                else
                {
                    await CreateDefaultFamilyAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load family profiles");
                await CreateDefaultFamilyAsync();
            }
        }

        public async Task<FamilyMember?> AuthenticateAsync(string username, string password)
        {
            // Use index for O(1) lookup instead of LINQ
            if (!_familyMembersIndex.TryGetValue(username.ToLowerInvariant(), out var member))
            {
                _logger.LogWarning($"Failed authentication attempt for unknown username: {username}");
                return null;
            }

            // Check if account is locked
            if (await IsAccountLockedAsync(username))
            {
                var lockTimeRemaining = member.AccountLockedUntil?.Subtract(DateTime.UtcNow);
                _logger.LogWarning($"Authentication failed: Account {username} is locked for {lockTimeRemaining?.Minutes} more minutes");
                return null;
            }

            // Check authentication cache first for successful logins
            var authCacheKey = $"{AUTH_CACHE_PREFIX}{username}_{ComputeQuickHash(password)}";
            
            if (_cache.TryGetValue(authCacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                var cachedAuth = (FamilyMember)cached.Data;
                cachedAuth.LastLoginTime = DateTime.UtcNow;
                cachedAuth.IsOnline = true;
                cachedAuth.FailedLoginAttempts = 0; // Reset on successful cached login
                await SaveFamilyDataAsync();
                return cachedAuth;
            }
            
            if (await VerifyPasswordAsync(password, member.PasswordHash))
            {
                // Successful login - reset failed attempts
                member.LastLoginTime = DateTime.UtcNow;
                member.IsOnline = true;
                member.FailedLoginAttempts = 0;
                
                // Cache successful authentication
                _cache[authCacheKey] = (member, DateTime.UtcNow.Add(AuthCacheExpiry));
                
                await SaveFamilyDataAsync();
                await _systemSecurity.LogFamilyActivityAsync($"User logged in: {username}", member);
                _logger.LogDebug($"Optimized authentication for {member.DisplayName}");
                
                return member;
            }
            else
            {
                // Failed login - increment attempts and potentially lock account
                member.FailedLoginAttempts++;
                
                if (member.FailedLoginAttempts >= 3)
                {
                    // Lock account for 15 minutes
                    member.AccountLockedUntil = DateTime.UtcNow.AddMinutes(15);
                    await SaveFamilyDataAsync();
                    await _systemSecurity.LogFamilyActivityAsync($"Account locked due to failed login attempts: {username}", member);
                    _logger.LogWarning($"Account locked for {username} due to {member.FailedLoginAttempts} failed attempts");
                }
                else
                {
                    await SaveFamilyDataAsync();
                    _logger.LogWarning($"Failed authentication attempt {member.FailedLoginAttempts}/3 for username: {username}");
                }
                
                return null;
            }
        }

        public async Task<List<FamilyMember>> GetFamilyMembersAsync()
        {
            // Return cached data if available
            if (_cache.TryGetValue(FAMILY_MEMBERS_CACHE_KEY, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return (List<FamilyMember>)cached.Data;
            }

            // Convert concurrent dictionary values to list efficiently
            var members = new List<FamilyMember>(_familyMembersIndex.Count);
            foreach (var kvp in _familyMembersIndex)
            {
                members.Add(kvp.Value);
            }

            return members;
        }

        public async Task AddFamilyMemberAsync(FamilyMember member)
        {
            member.Id = Guid.NewGuid().ToString();
            member.PasswordHash = await HashPasswordAsync(member.PasswordHash);
            
            // Add to index
            _familyMembersIndex.TryAdd(member.Username.ToLowerInvariant(), member);
            
            // Invalidate cache
            _cache.Remove(FAMILY_MEMBERS_CACHE_KEY);
            
            await SaveFamilyDataAsync();
            await _systemSecurity.LogFamilyActivityAsync($"New family member added: {member.DisplayName}", member);
            
            _logger.LogInformation($"Added new family member: {member.DisplayName} (optimized)");
        }

        public async Task UpdateFamilyMemberAsync(FamilyMember member)
        {
            if (_familyMembersIndex.TryUpdate(member.Username.ToLowerInvariant(), member, _familyMembersIndex[member.Username.ToLowerInvariant()]))
            {
                // Invalidate cache
                _cache.Remove(FAMILY_MEMBERS_CACHE_KEY);
                
                await SaveFamilyDataAsync();
                await _systemSecurity.LogFamilyActivityAsync($"Family member updated: {member.DisplayName}", member);
                
                _logger.LogInformation($"Updated family member: {member.DisplayName} (optimized)");
            }
        }

        public async Task SaveFamilyDataAsync()
        {
            // Use semaphore to prevent concurrent saves
            await _saveSemaphore.WaitAsync();
            
            try
            {
                var profilesFile = Path.Combine(_dataPath, "family_profiles.json");
                var members = await GetFamilyMembersAsync();
                
                // Use object pooling for StringBuilder
                var sb = _stringBuilderPool.Get();
                try
                {
                    // Serialize directly to StringBuilder for memory efficiency
                    var jsonData = FamilyOSJsonHelper.Serialize(members);
                    var encryptedData = await _systemSecurity.EncryptFamilyDataAsync(jsonData);
                    
                    // Use async file operations
                    await File.WriteAllTextAsync(profilesFile, encryptedData);
                    _logger.LogDebug("Family data saved successfully (optimized)");
                }
                finally
                {
                    _stringBuilderPool.Return(sb);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save family data");
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }

        public int GetFamilyMemberCount()
        {
            return _familyMembersIndex.Count;
        }

        private async Task CreateDefaultFamilyAsync()
        {
            var defaultFamily = new List<FamilyMember>
            {
                new FamilyMember
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "mom",
                    DisplayName = "Mom",
                    PasswordHash = await HashPasswordAsync("parent123"),
                    AgeGroup = AgeGroup.Parent,
                    Role = FamilyRole.Parent,
                    DateOfBirth = DateTime.Now.AddYears(-35),
                    FilterLevel = ContentFilterLevel.Minimal,
                    ScreenTime = new ScreenTimeSettings
                    {
                        DailyLimit = TimeSpan.FromHours(8),
                        EnforceScreenTime = false
                    }
                },
                new FamilyMember
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "dad",
                    DisplayName = "Dad", 
                    PasswordHash = await HashPasswordAsync("parent123"),
                    AgeGroup = AgeGroup.Parent,
                    Role = FamilyRole.Parent,
                    DateOfBirth = DateTime.Now.AddYears(-37),
                    FilterLevel = ContentFilterLevel.Minimal,
                    ScreenTime = new ScreenTimeSettings
                    {
                        DailyLimit = TimeSpan.FromHours(8),
                        EnforceScreenTime = false
                    }
                },
                new FamilyMember
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "sarah",
                    DisplayName = "Sarah",
                    PasswordHash = await HashPasswordAsync("kid123"),
                    AgeGroup = AgeGroup.Elementary,
                    Role = FamilyRole.Child,
                    DateOfBirth = DateTime.Now.AddYears(-8),
                    FilterLevel = ContentFilterLevel.Strict,
                    AllowedApps = new List<string> { "Safe Browser", "Educational Hub", "Family Game Center" },
                    ScreenTime = new ScreenTimeSettings
                    {
                        DailyLimit = TimeSpan.FromMinutes(90),
                        WeekdayLimit = TimeSpan.FromMinutes(60),
                        WeekendLimit = TimeSpan.FromMinutes(120),
                        EnforceScreenTime = true
                    }
                },
                new FamilyMember
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "alex",
                    DisplayName = "Alex",
                    PasswordHash = await HashPasswordAsync("teen123"),
                    AgeGroup = AgeGroup.MiddleSchool,
                    Role = FamilyRole.Teen,
                    DateOfBirth = DateTime.Now.AddYears(-13),
                    FilterLevel = ContentFilterLevel.Moderate,
                    AllowedApps = new List<string> { "Safe Browser", "Educational Hub", "Family Game Center", "Family Chat" },
                    ScreenTime = new ScreenTimeSettings
                    {
                        DailyLimit = TimeSpan.FromHours(3),
                        WeekdayLimit = TimeSpan.FromHours(2),
                        WeekendLimit = TimeSpan.FromHours(4),
                        EnforceScreenTime = true
                    }
                }
            };

            UpdateIndex(defaultFamily);
            
            // Cache the default family
            _cache[FAMILY_MEMBERS_CACHE_KEY] = (defaultFamily, DateTime.UtcNow.Add(FamilyDataCacheExpiry));
            
            await SaveFamilyDataAsync();
            
            _logger.LogInformation("Created default family profiles (optimized)");
        }

        private void UpdateIndex(List<FamilyMember> members)
        {
            _familyMembersIndex.Clear();
            foreach (var member in members)
            {
                _familyMembersIndex.TryAdd(member.Username.ToLowerInvariant(), member);
            }
        }

        private async Task<string> HashPasswordAsync(string password)
        {
            // Use async hashing to avoid blocking thread pool
            return await Task.Run(() =>
            {
                // Generate a cryptographically secure random salt
                var salt = new byte[32];
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                rng.GetBytes(salt);
                
                using (var sha256 = SHA256.Create())
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var saltedPassword = passwordBytes.Concat(salt).ToArray();
                    var hashedBytes = sha256.ComputeHash(saltedPassword);
                    
                    // Return hash:salt format for storage
                    return Convert.ToBase64String(hashedBytes) + ":" + Convert.ToBase64String(salt);
                }
            });
        }

        private async Task<bool> VerifyPasswordAsync(string password, string storedHash)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var parts = storedHash.Split(':');
                    if (parts.Length != 2) return false;
                    
                    var hash = Convert.FromBase64String(parts[0]);
                    var salt = Convert.FromBase64String(parts[1]);
                    
                    using var sha256 = SHA256.Create();
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var saltedPassword = passwordBytes.Concat(salt).ToArray();
                    var computedHash = sha256.ComputeHash(saltedPassword);
                    
                    return hash.SequenceEqual(computedHash);
                }
                catch
                {
                    return false;
                }
            });
        }

        private string ComputeQuickHash(string input)
        {
            // Quick non-cryptographic hash for cache keys
            return input.GetHashCode().ToString("x");
        }

        public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword, FamilyMember requestingUser)
        {
            try
            {
                var member = _familyMembersIndex.Values.FirstOrDefault(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (member == null)
                {
                    _logger.LogWarning($"Password change failed: User {username} not found");
                    return false;
                }

                // Check permissions: user changing own password OR parent changing child's password
                if (member.Id != requestingUser.Id && requestingUser.Role != FamilyRole.Parent)
                {
                    _logger.LogWarning($"Permission denied: {requestingUser.Username} attempted to change password for {username}");
                    return false;
                }

                // If user is changing own password, verify current password
                if (member.Id == requestingUser.Id && !await VerifyPasswordAsync(currentPassword, member.PasswordHash))
                {
                    _logger.LogWarning($"Password change failed: Invalid current password for {username}");
                    return false;
                }

                // Update password
                member.PasswordHash = await HashPasswordAsync(newPassword);
                member.LastPasswordChange = DateTime.UtcNow;
                member.PasswordChangeHistory.Add(DateTime.UtcNow);
                
                // Keep only last 10 password change dates
                if (member.PasswordChangeHistory.Count > 10)
                {
                    member.PasswordChangeHistory = member.PasswordChangeHistory.Skip(member.PasswordChangeHistory.Count - 10).ToList();
                }

                await SaveFamilyDataAsync();
                await _systemSecurity.LogFamilyActivityAsync($"Password changed for {username} by {requestingUser.Username}", requestingUser);
                
                // Clear auth cache for this user
                var authCacheKey = $"{AUTH_CACHE_PREFIX}{username}_";
                var keysToRemove = _cache.Keys.Where(k => k.StartsWith(authCacheKey)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                
                _logger.LogInformation($"Password successfully changed for {username} by {requestingUser.Username}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for {username}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string targetUsername, string newPassword, FamilyMember parentUser)
        {
            try
            {
                // Only parents can reset passwords
                if (parentUser.Role != FamilyRole.Parent)
                {
                    _logger.LogWarning($"Permission denied: {parentUser.Username} attempted to reset password for {targetUsername}");
                    return false;
                }

                var member = _familyMembersIndex.Values.FirstOrDefault(m => m.Username.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));
                if (member == null)
                {
                    _logger.LogWarning($"Password reset failed: User {targetUsername} not found");
                    return false;
                }

                // Reset password and unlock account
                member.PasswordHash = await HashPasswordAsync(newPassword);
                member.LastPasswordChange = DateTime.UtcNow;
                member.PasswordChangeHistory.Add(DateTime.UtcNow);
                member.FailedLoginAttempts = 0;
                member.AccountLockedUntil = null;

                await SaveFamilyDataAsync();
                await _systemSecurity.LogFamilyActivityAsync($"Password reset for {targetUsername} by parent {parentUser.Username}", parentUser);
                
                // Clear auth cache for this user
                var authCacheKey = $"{AUTH_CACHE_PREFIX}{targetUsername}_";
                var keysToRemove = _cache.Keys.Where(k => k.StartsWith(authCacheKey)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                
                _logger.LogInformation($"Password successfully reset for {targetUsername} by parent {parentUser.Username}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for {targetUsername}");
                return false;
            }
        }

        public async Task<bool> IsAccountLockedAsync(string username)
        {
            var member = _familyMembersIndex.Values.FirstOrDefault(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (member?.AccountLockedUntil == null)
                return false;

            if (DateTime.UtcNow >= member.AccountLockedUntil)
            {
                // Auto-unlock expired lockouts
                member.AccountLockedUntil = null;
                member.FailedLoginAttempts = 0;
                await SaveFamilyDataAsync();
                return false;
            }

            return true;
        }

        public async Task UnlockAccountAsync(string username, FamilyMember parentUser)
        {
            try
            {
                if (parentUser.Role != FamilyRole.Parent)
                {
                    _logger.LogWarning($"Permission denied: {parentUser.Username} attempted to unlock account for {username}");
                    return;
                }

                var member = _familyMembersIndex.Values.FirstOrDefault(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (member != null)
                {
                    member.AccountLockedUntil = null;
                    member.FailedLoginAttempts = 0;
                    
                    await SaveFamilyDataAsync();
                    await _systemSecurity.LogFamilyActivityAsync($"Account unlocked for {username} by parent {parentUser.Username}", parentUser);
                    
                    _logger.LogInformation($"Account successfully unlocked for {username} by parent {parentUser.Username}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlocking account for {username}");
            }
        }

        public async Task<List<string>> GetPasswordChangeHistoryAsync(string username, FamilyMember requestingUser)
        {
            try
            {
                var member = _familyMembersIndex.Values.FirstOrDefault(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (member == null)
                    return new List<string>();

                // Check permissions: own history OR parent viewing child's history
                if (member.Id != requestingUser.Id && requestingUser.Role != FamilyRole.Parent)
                {
                    _logger.LogWarning($"Permission denied: {requestingUser.Username} attempted to view password history for {username}");
                    return new List<string>();
                }

                return member.PasswordChangeHistory
                    .Select(date => date.ToString("yyyy-MM-dd HH:mm:ss"))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving password change history for {username}");
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Simple object pool implementation for reducing allocations
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _objects = new ConcurrentBag<T>();
        private readonly Func<T> _objectGenerator;
        private readonly Action<T>? _resetAction;

        public ObjectPool(Func<T> objectGenerator, Action<T>? resetAction = null)
        {
            _objectGenerator = objectGenerator;
            _resetAction = resetAction;
        }

        public T Get()
        {
            if (_objects.TryTake(out T? item))
            {
                return item;
            }
            return _objectGenerator();
        }

        public void Return(T item)
        {
            _resetAction?.Invoke(item);
            _objects.Add(item);
        }
    }

    /// <summary>
    /// Optimized content filter with caching and efficient algorithms
    /// </summary>
    public class OptimizedContentFilterService : IContentFilter
    {
        private readonly ILogger<OptimizedContentFilterService> _logger;
        private readonly ISystemSecurity _systemSecurity;
        private readonly Dictionary<string, (object Data, DateTime Expiry)> _cache;
        private readonly HttpClient _httpClient;
        private readonly string _pocketFenceApiUrl;
        private readonly HashSet<string> _educationalDomains;
        private readonly Dictionary<AgeGroup, HashSet<string>> _blockedDomainsByAge;
        private bool _isActive;

        private const string DOMAIN_CACHE_PREFIX = "domain_";
        private const string CONTENT_CACHE_PREFIX = "content_";
        private static readonly TimeSpan FilterCacheExpiry = TimeSpan.FromMinutes(15);

        public bool IsActive => _isActive;

        public OptimizedContentFilterService(
            ILogger<OptimizedContentFilterService> logger, 
            ISystemSecurity systemSecurity,
            string pocketFenceApiUrl = "https://localhost:5001")
        {
            _logger = logger;
            _systemSecurity = systemSecurity;
            _cache = new Dictionary<string, (object Data, DateTime Expiry)>();
            _httpClient = new HttpClient();
            _pocketFenceApiUrl = pocketFenceApiUrl;

            // Pre-populate domain sets for O(1) lookup
            _educationalDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "khanacademy.org", "education.com", "pbskids.org", "sesamestreet.org",
                "kids.nationalgeographic.com", "funbrain.com", "coolmath4kids.com",
                "nasa.gov", "britannica.com", "scratch.mit.edu"
            };

            _blockedDomainsByAge = new Dictionary<AgeGroup, HashSet<string>>
            {
                [AgeGroup.Toddler] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "youtube.com", "facebook.com", "twitter.com", "instagram.com", "tiktok.com",
                    "reddit.com", "discord.com", "twitch.tv", "pinterest.com"
                },
                [AgeGroup.Elementary] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "facebook.com", "twitter.com", "instagram.com", "tiktok.com",
                    "reddit.com", "discord.com", "pinterest.com"
                },
                [AgeGroup.MiddleSchool] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "tiktok.com", "reddit.com", "discord.com"
                }
            };
        }

        public async Task StartAsync()
        {
            _isActive = true;
            _logger.LogInformation("Optimized content filtering started");
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _isActive = false;
            _httpClient.Dispose();
            _logger.LogInformation("Optimized content filtering stopped");
        }

        public async Task<ContentFilterResult> FilterUrlAsync(string url, FamilyMember user)
        {
            // Check cache first
            var cacheKey = $"{DOMAIN_CACHE_PREFIX}{url}_{user.AgeGroup}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return (ContentFilterResult)cached.Data;
            }

            var result = await LocalUrlFilterAsync(url, user);
            
            // Cache the result
            _cache[cacheKey] = (result, DateTime.Now.Add(FilterCacheExpiry));
            
            return result;
        }

        public async Task<ContentFilterResult> FilterTextAsync(string text, FamilyMember user)
        {
            var textHash = text.GetHashCode().ToString("x");
            var cacheKey = $"{CONTENT_CACHE_PREFIX}{textHash}_{user.AgeGroup}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedEntry) && cachedEntry.Expiry > DateTime.Now)
            {
                return (ContentFilterResult)cachedEntry.Data;
            }

            var result = await LocalTextFilterAsync(text, user);
            
            // Cache the result
            _cache[cacheKey] = (result, DateTime.Now.Add(FilterCacheExpiry));
            
            return result;
        }

        public async Task<ContentFilterResult> FilterImageAsync(byte[] imageData, FamilyMember user)
        {
            // Placeholder for optimized image filtering
            return new ContentFilterResult
            {
                IsAllowed = true,
                Reason = "Image filtering optimized",
                ThreatScore = 0.0,
                TriggeredRules = new List<string>()
            };
        }

        private async Task<ContentFilterResult> LocalUrlFilterAsync(string url, FamilyMember user)
        {
            var uri = new Uri(url);
            var domain = uri.Host.ToLowerInvariant();

            // Check educational domains first (highest priority)
            if (_educationalDomains.Any(ed => domain.Contains(ed)))
            {
                return new ContentFilterResult
                {
                    IsAllowed = true,
                    Reason = "Educational content approved",
                    ThreatScore = 0.0
                };
            }

            // Check blocked domains for user's age group
            if (_blockedDomainsByAge.TryGetValue(user.AgeGroup, out var blockedDomains))
            {
                var isBlocked = blockedDomains.Any(blocked => domain.Contains(blocked));
                
                if (isBlocked)
                {
                    var result = new ContentFilterResult
                    {
                        IsAllowed = false,
                        Reason = $"Blocked for age group {user.AgeGroup}",
                        ThreatScore = 0.8,
                        TriggeredRules = new List<string> { "Age-inappropriate domain" }
                    };

                    await LogFilterDecisionAsync(url, result, user, "URL");
                    return result;
                }
            }

            return new ContentFilterResult
            {
                IsAllowed = true,
                Reason = "Content approved",
                ThreatScore = 0.1
            };
        }

        private async Task<ContentFilterResult> LocalTextFilterAsync(string text, FamilyMember user)
        {
            // Use pre-compiled patterns for better performance
            var inappropriatePatterns = GetInappropriateWordsForAge(user.AgeGroup);
            var foundWords = new List<string>();

            // Efficient pattern matching
            var lowerText = text.ToLowerInvariant();
            foreach (var pattern in inappropriatePatterns)
            {
                if (lowerText.Contains(pattern))
                {
                    foundWords.Add(pattern);
                }
            }

            var result = new ContentFilterResult
            {
                IsAllowed = !foundWords.Any(),
                Reason = foundWords.Any() ? $"Contains inappropriate content for {user.AgeGroup}" : "Content approved",
                ThreatScore = foundWords.Any() ? 0.7 : 0.0,
                TriggeredRules = foundWords.Select(w => $"Inappropriate word: {w}").ToList()
            };

            if (!result.IsAllowed)
            {
                await LogFilterDecisionAsync(text.Substring(0, Math.Min(50, text.Length)), result, user, "Text");
            }

            return result;
        }

        private async Task LogFilterDecisionAsync(string content, ContentFilterResult result, FamilyMember user, string type)
        {
            var action = result.IsAllowed ? "Allowed" : "Blocked";
            await _systemSecurity.LogFamilyActivityAsync(
                $"{type} {action}: {content} (Score: {result.ThreatScore:F2})", user);
        }

        private List<string> GetInappropriateWordsForAge(AgeGroup ageGroup)
        {
            // Return pre-defined lists based on age group for efficiency
            return ageGroup switch
            {
                AgeGroup.Toddler or AgeGroup.Preschool => new List<string>
                {
                    "violence", "drugs", "alcohol", "gambling", "weapon", "fight", "kill",
                    "scary", "monster", "nightmare", "ghost", "spider", "snake"
                },
                AgeGroup.Elementary => new List<string>
                {
                    "violence", "drugs", "alcohol", "gambling", "weapon", "fight", "kill",
                    "dating", "boyfriend", "girlfriend", "kiss"
                },
                _ => new List<string>
                {
                    "violence", "drugs", "alcohol", "gambling", "weapon", "fight", "kill"
                }
            };
        }
    }
}