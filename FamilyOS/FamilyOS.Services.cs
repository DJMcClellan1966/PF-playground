using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PocketFence.FamilyOS.Core;

namespace PocketFence.FamilyOS.Services
{
    /// <summary>
    /// Family member management service implementation
    /// </summary>
    public class FamilyManagerService : IFamilyManager
    {
        private readonly ILogger<FamilyManagerService> _logger;
        private readonly ISystemSecurity _systemSecurity;
        private readonly string _dataPath;
        private readonly List<FamilyMember> _familyMembers;

        public FamilyManagerService(ILogger<FamilyManagerService> logger, ISystemSecurity systemSecurity, string dataPath = "./FamilyData")
        {
            _logger = logger;
            _systemSecurity = systemSecurity;
            _dataPath = dataPath;
            _familyMembers = new List<FamilyMember>();
        }

        public async Task LoadFamilyProfilesAsync()
        {
            try
            {
                var profilesFile = Path.Combine(_dataPath, "family_profiles.json");
                
                if (!Directory.Exists(_dataPath))
                {
                    Directory.CreateDirectory(_dataPath);
                }

                if (File.Exists(profilesFile))
                {
                    var encryptedData = await File.ReadAllTextAsync(profilesFile);
                    var decryptedData = await _systemSecurity.DecryptFamilyDataAsync(encryptedData);
                    var profiles = JsonSerializer.Deserialize<List<FamilyMember>>(decryptedData);
                    
                    if (profiles != null)
                    {
                        _familyMembers.AddRange(profiles);
                        _logger.LogInformation($"Loaded {profiles.Count} family member profiles");
                    }
                }
                else
                {
                    // Create default family profiles for demo
                    await CreateDefaultFamilyAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load family profiles");
                await CreateDefaultFamilyAsync();
            }
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
                    PasswordHash = HashPassword("parent123"),
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
                    PasswordHash = HashPassword("parent123"),
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
                    PasswordHash = HashPassword("kid123"),
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
                    PasswordHash = HashPassword("teen123"),
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

            _familyMembers.AddRange(defaultFamily);
            await SaveFamilyDataAsync();
            
            _logger.LogInformation("Created default family profiles");
            _logger.LogInformation("Default login credentials:");
            _logger.LogInformation("  Parents: mom/parent123, dad/parent123");
            _logger.LogInformation("  Children: sarah/kid123, alex/teen123");
        }

        public async Task<FamilyMember?> AuthenticateAsync(string username, string password)
        {
            var member = _familyMembers.FirstOrDefault(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            
            if (member != null && VerifyPassword(password, member.PasswordHash))
            {
                member.LastLoginTime = DateTime.UtcNow;
                member.IsOnline = true;
                
                await _systemSecurity.LogFamilyActivityAsync($"User logged in: {username}", member);
                _logger.LogInformation($"Successful authentication for {member.DisplayName}");
                
                return member;
            }

            _logger.LogWarning($"Failed authentication attempt for username: {username}");
            return null;
        }

        public async Task<List<FamilyMember>> GetFamilyMembersAsync()
        {
            return await Task.FromResult(_familyMembers.ToList());
        }

        public async Task AddFamilyMemberAsync(FamilyMember member)
        {
            member.Id = Guid.NewGuid().ToString();
            member.PasswordHash = HashPassword(member.PasswordHash); // Assume password is passed in PasswordHash field
            _familyMembers.Add(member);
            
            await SaveFamilyDataAsync();
            await _systemSecurity.LogFamilyActivityAsync($"New family member added: {member.DisplayName}", member);
            
            _logger.LogInformation($"Added new family member: {member.DisplayName}");
        }

        public async Task UpdateFamilyMemberAsync(FamilyMember member)
        {
            var existingMember = _familyMembers.FirstOrDefault(m => m.Id == member.Id);
            if (existingMember != null)
            {
                var index = _familyMembers.IndexOf(existingMember);
                _familyMembers[index] = member;
                
                await SaveFamilyDataAsync();
                await _systemSecurity.LogFamilyActivityAsync($"Family member updated: {member.DisplayName}", member);
                
                _logger.LogInformation($"Updated family member: {member.DisplayName}");
            }
        }

        public async Task SaveFamilyDataAsync()
        {
            try
            {
                var profilesFile = Path.Combine(_dataPath, "family_profiles.json");
                var jsonData = JsonSerializer.Serialize(_familyMembers, new JsonSerializerOptions { WriteIndented = true });
                var encryptedData = await _systemSecurity.EncryptFamilyDataAsync(jsonData);
                
                await File.WriteAllTextAsync(profilesFile, encryptedData);
                _logger.LogDebug("Family data saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save family data");
            }
        }

        public int GetFamilyMemberCount()
        {
            return _familyMembers.Count;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "family_salt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }

    /// <summary>
    /// Parental controls service implementation
    /// </summary>
    public class ParentalControlsService : IParentalControls
    {
        private readonly ILogger<ParentalControlsService> _logger;
        private readonly ISystemSecurity _systemSecurity;
        private readonly Dictionary<string, DateTime> _userLoginTimes;
        private readonly Dictionary<string, TimeSpan> _dailyScreenTime;
        private bool _isActive;

        public bool IsActive => _isActive;

        public ParentalControlsService(ILogger<ParentalControlsService> logger, ISystemSecurity systemSecurity)
        {
            _logger = logger;
            _systemSecurity = systemSecurity;
            _userLoginTimes = new Dictionary<string, DateTime>();
            _dailyScreenTime = new Dictionary<string, TimeSpan>();
        }

        public async Task InitializeAsync()
        {
            _isActive = true;
            _logger.LogInformation("Parental Controls initialized and active");
            await Task.CompletedTask;
        }

        public async Task ApplyUserRestrictionsAsync(FamilyMember member)
        {
            _userLoginTimes[member.Id] = DateTime.UtcNow;
            
            var restrictions = member.AgeGroup switch
            {
                AgeGroup.Toddler => "Very restricted - Educational content only, 15-minute sessions",
                AgeGroup.Preschool => "Highly restricted - Basic educational content, 30-minute sessions",
                AgeGroup.Elementary => "Restricted - Age-appropriate educational and entertainment content",
                AgeGroup.MiddleSchool => "Moderate restrictions - Supervised social media and research access",
                AgeGroup.HighSchool => "Light restrictions - Most content allowed with monitoring",
                AgeGroup.Parent => "No restrictions - Full administrative access",
                _ => "Default restrictions applied"
            };

            _logger.LogInformation($"Applied restrictions for {member.DisplayName}: {restrictions}");
            await _systemSecurity.LogFamilyActivityAsync($"Parental controls applied for {member.DisplayName}", member);
        }

        public async Task<bool> CanAccessAppAsync(FamilyMember member, string appName)
        {
            // Check if app is in allowed list
            if (member.AllowedApps.Contains(appName))
            {
                return true;
            }

            // Check if app is blocked
            if (member.BlockedApps.Contains(appName))
            {
                await _systemSecurity.LogFamilyActivityAsync($"Blocked app access: {appName}", member);
                return false;
            }

            // Age-based app restrictions
            var isAllowed = appName switch
            {
                "Safe Browser" => member.AgeGroup >= AgeGroup.Preschool,
                "Educational Hub" => true, // Always allowed
                "Family Game Center" => member.AgeGroup >= AgeGroup.Preschool,
                "Family Chat" => member.AgeGroup >= AgeGroup.Elementary,
                "Family File Manager" => member.AgeGroup >= AgeGroup.Elementary,
                "Screen Time Manager" => true, // Always allowed
                _ => member.AgeGroup >= AgeGroup.HighSchool // Other apps require high school age
            };

            if (!isAllowed)
            {
                await _systemSecurity.LogFamilyActivityAsync($"Age restriction: {appName} blocked for {member.AgeGroup}", member);
            }

            return isAllowed;
        }

        public async Task<bool> CanAccessUrlAsync(FamilyMember member, string url)
        {
            // Check allow list first
            if (member.AllowedWebsites.Any(allowed => url.Contains(allowed, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Age-based URL filtering
            var uri = new Uri(url);
            var domain = uri.Host.ToLowerInvariant();

            var blockedDomains = GetBlockedDomainsForAge(member.AgeGroup);
            var isBlocked = blockedDomains.Any(blocked => domain.Contains(blocked));

            if (isBlocked)
            {
                await _systemSecurity.LogFamilyActivityAsync($"Blocked URL access: {url}", member);
                return false;
            }

            return true;
        }

        public async Task<bool> CanAccessContentAsync(FamilyMember member, string content)
        {
            // Content analysis based on age group
            var inappropriateKeywords = GetInappropriateKeywordsForAge(member.AgeGroup);
            var hasInappropriateContent = inappropriateKeywords.Any(keyword => 
                content.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (hasInappropriateContent)
            {
                await _systemSecurity.LogFamilyActivityAsync($"Blocked inappropriate content for {member.AgeGroup}", member);
                return false;
            }

            return true;
        }

        public async Task<TimeSpan> GetRemainingScreenTimeAsync(FamilyMember member)
        {
            if (!member.ScreenTime.EnforceScreenTime || member.Role == FamilyRole.Parent)
            {
                return TimeSpan.FromHours(24); // Unlimited for parents
            }

            var today = DateTime.Today;
            var isWeekend = today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday;
            
            var dailyLimit = isWeekend ? member.ScreenTime.WeekendLimit : member.ScreenTime.WeekdayLimit;
            var usedTime = GetUsedScreenTimeToday(member.Id);
            var remaining = dailyLimit - usedTime;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public async Task SaveStateAsync()
        {
            // Save current screen time data
            var stateData = new
            {
                UserLoginTimes = _userLoginTimes,
                DailyScreenTime = _dailyScreenTime,
                LastSaved = DateTime.UtcNow
            };

            try
            {
                var jsonData = JsonSerializer.Serialize(stateData);
                var encryptedData = await _systemSecurity.EncryptFamilyDataAsync(jsonData);
                await File.WriteAllTextAsync("./FamilyData/parental_controls_state.json", encryptedData);
                
                _logger.LogDebug("Parental controls state saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save parental controls state");
            }
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

        private List<string> GetBlockedDomainsForAge(AgeGroup ageGroup)
        {
            return ageGroup switch
            {
                AgeGroup.Toddler or AgeGroup.Preschool => new List<string>
                {
                    "youtube.com", "facebook.com", "twitter.com", "instagram.com", "tiktok.com",
                    "reddit.com", "discord.com", "twitch.tv", "pinterest.com"
                },
                AgeGroup.Elementary => new List<string>
                {
                    "facebook.com", "twitter.com", "instagram.com", "tiktok.com",
                    "reddit.com", "discord.com", "pinterest.com"
                },
                AgeGroup.MiddleSchool => new List<string>
                {
                    "tiktok.com", "reddit.com", "discord.com"
                },
                _ => new List<string>() // High school and above - minimal blocking
            };
        }

        private List<string> GetInappropriateKeywordsForAge(AgeGroup ageGroup)
        {
            var baseKeywords = new List<string>
            {
                "violence", "drugs", "alcohol", "gambling", "inappropriate", "adult"
            };

            return ageGroup switch
            {
                AgeGroup.Toddler or AgeGroup.Preschool => baseKeywords.Concat(new[]
                {
                    "scary", "monster", "nightmare", "fight", "weapon"
                }).ToList(),
                AgeGroup.Elementary => baseKeywords.Concat(new[]
                {
                    "dating", "romance", "social media", "chat"
                }).ToList(),
                AgeGroup.MiddleSchool => baseKeywords,
                _ => new List<string>() // Minimal filtering for older users
            };
        }
    }

    /// <summary>
    /// Content filtering service that integrates with PocketFence
    /// </summary>
    public class ContentFilterService : IContentFilter
    {
        private readonly ILogger<ContentFilterService> _logger;
        private readonly ISystemSecurity _systemSecurity;
        private readonly HttpClient _httpClient;
        private readonly string _pocketFenceApiUrl;
        private bool _isActive;

        public bool IsActive => _isActive;

        public ContentFilterService(ILogger<ContentFilterService> logger, ISystemSecurity systemSecurity, 
            string pocketFenceApiUrl = "http://localhost:5000")
        {
            _logger = logger;
            _systemSecurity = systemSecurity;
            _httpClient = new HttpClient();
            _pocketFenceApiUrl = pocketFenceApiUrl;
        }

        public async Task StartAsync()
        {
            try
            {
                // Test connection to PocketFence API
                var response = await _httpClient.GetAsync($"{_pocketFenceApiUrl}/api/kernel/health");
                
                if (response.IsSuccessStatusCode)
                {
                    _isActive = true;
                    _logger.LogInformation("Content filtering active - Connected to PocketFence API");
                }
                else
                {
                    _logger.LogWarning("PocketFence API not available - Using local filtering only");
                    _isActive = true; // Still activate with local filtering
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not connect to PocketFence API - Using local filtering");
                _isActive = true; // Fallback to local filtering
            }
        }

        public async Task StopAsync()
        {
            _isActive = false;
            _httpClient.Dispose();
            _logger.LogInformation("Content filtering stopped");
        }

        public async Task<ContentFilterResult> FilterUrlAsync(string url, FamilyMember user)
        {
            try
            {
                // Try PocketFence API first
                var requestBody = new { url = url };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_pocketFenceApiUrl}/api/filter/url", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var filterResult = JsonSerializer.Deserialize<ContentFilterResult>(result);
                    
                    if (filterResult != null)
                    {
                        await LogFilterDecision(url, filterResult, user, "URL");
                        return filterResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PocketFence API call failed, using local filtering");
            }

            // Fallback to local filtering
            return await LocalUrlFilterAsync(url, user);
        }

        public async Task<ContentFilterResult> FilterTextAsync(string text, FamilyMember user)
        {
            try
            {
                var requestBody = new { text = text };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_pocketFenceApiUrl}/api/filter/content", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var filterResult = JsonSerializer.Deserialize<ContentFilterResult>(result);
                    
                    if (filterResult != null)
                    {
                        await LogFilterDecision(text, filterResult, user, "Content");
                        return filterResult;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PocketFence API call failed, using local filtering");
            }

            return await LocalTextFilterAsync(text, user);
        }

        public async Task<ContentFilterResult> FilterImageAsync(byte[] imageData, FamilyMember user)
        {
            // For now, implement basic image filtering
            // In a full implementation, this would analyze image content
            return await Task.FromResult(new ContentFilterResult
            {
                IsAllowed = true,
                Reason = "Image content filtering not yet implemented",
                ThreatScore = 0.0,
                TriggeredRules = new List<string>()
            });
        }

        private async Task<ContentFilterResult> LocalUrlFilterAsync(string url, FamilyMember user)
        {
            var uri = new Uri(url);
            var domain = uri.Host.ToLowerInvariant();

            // Educational sites - always allowed
            var educationalDomains = new[]
            {
                "khanacademy.org", "education.com", "pbskids.org", "sesamestreet.org",
                "kids.nationalgeographic.com", "funbrain.com", "coolmath4kids.com",
                "nasa.gov", "britannica.com", "scratch.mit.edu"
            };

            if (educationalDomains.Any(ed => domain.Contains(ed)))
            {
                return new ContentFilterResult
                {
                    IsAllowed = true,
                    Reason = "Educational content approved",
                    ThreatScore = 0.0
                };
            }

            // Blocked domains by age
            var blockedDomains = GetBlockedDomainsForAge(user.AgeGroup);
            var isBlocked = blockedDomains.Any(blocked => domain.Contains(blocked));

            var result = new ContentFilterResult
            {
                IsAllowed = !isBlocked,
                Reason = isBlocked ? $"Blocked for age group {user.AgeGroup}" : "Content approved",
                ThreatScore = isBlocked ? 0.8 : 0.1,
                TriggeredRules = isBlocked ? new List<string> { "Age-inappropriate domain" } : new List<string>()
            };

            await LogFilterDecision(url, result, user, "URL");
            return result;
        }

        private async Task<ContentFilterResult> LocalTextFilterAsync(string text, FamilyMember user)
        {
            var inappropriateWords = GetInappropriateWordsForAge(user.AgeGroup);
            var foundWords = inappropriateWords.Where(word => 
                text.Contains(word, StringComparison.OrdinalIgnoreCase)).ToList();

            var result = new ContentFilterResult
            {
                IsAllowed = !foundWords.Any(),
                Reason = foundWords.Any() ? $"Contains inappropriate content for {user.AgeGroup}" : "Content approved",
                ThreatScore = foundWords.Any() ? 0.7 : 0.0,
                TriggeredRules = foundWords.Select(w => $"Inappropriate word: {w}").ToList()
            };

            await LogFilterDecision(text.Substring(0, Math.Min(50, text.Length)), result, user, "Text");
            return result;
        }

        private async Task LogFilterDecision(string content, ContentFilterResult result, FamilyMember user, string type)
        {
            var action = result.IsAllowed ? "Allowed" : "Blocked";
            await _systemSecurity.LogFamilyActivityAsync(
                $"{type} {action}: {content} (Score: {result.ThreatScore:F2})", user);
        }

        private List<string> GetBlockedDomainsForAge(AgeGroup ageGroup)
        {
            return ageGroup switch
            {
                AgeGroup.Toddler or AgeGroup.Preschool => new List<string>
                {
                    "youtube.com", "facebook.com", "twitter.com", "instagram.com", "tiktok.com",
                    "reddit.com", "discord.com", "twitch.tv", "snapchat.com"
                },
                AgeGroup.Elementary => new List<string>
                {
                    "facebook.com", "twitter.com", "instagram.com", "tiktok.com",
                    "reddit.com", "discord.com", "snapchat.com"
                },
                AgeGroup.MiddleSchool => new List<string>
                {
                    "tiktok.com", "reddit.com"
                },
                _ => new List<string>()
            };
        }

        private List<string> GetInappropriateWordsForAge(AgeGroup ageGroup)
        {
            var baseWords = new List<string>
            {
                "violence", "drugs", "alcohol", "gambling", "weapon", "fight", "kill"
            };

            return ageGroup switch
            {
                AgeGroup.Toddler or AgeGroup.Preschool => baseWords.Concat(new[]
                {
                    "scary", "monster", "nightmare", "ghost", "spider", "snake"
                }).ToList(),
                AgeGroup.Elementary => baseWords.Concat(new[]
                {
                    "dating", "boyfriend", "girlfriend", "kiss"
                }).ToList(),
                _ => baseWords
            };
        }
    }

    /// <summary>
    /// System security service implementation
    /// </summary>
    public class SystemSecurityService : ISystemSecurity
    {
        private readonly ILogger<SystemSecurityService> _logger;
        private readonly string _encryptionKey;
        private readonly List<AuditLog> _auditLogs;
        private readonly string _dataPath;

        public SystemSecurityService(ILogger<SystemSecurityService> logger, string dataPath = "./FamilyData")
        {
            _logger = logger;
            _dataPath = dataPath;
            _encryptionKey = GenerateEncryptionKey();
            _auditLogs = new List<AuditLog>();
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            await LoadAuditLogsAsync();
            _logger.LogInformation("System security initialized with encryption and audit logging");
        }

        public async Task<string> EncryptFamilyDataAsync(string data)
        {
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
                        
                        return Convert.ToBase64String(msEncrypt.ToArray());
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
                        return await srDecrypt.ReadToEndAsync();
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
            // Simple PIN verification - in production, use more secure methods
            var validPins = new[] { "1234", "0000", "9999", "parent" };
            var isValid = validPins.Contains(parentPin);
            
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

            _auditLogs.Add(auditLog);
            
            // Save audit logs periodically
            if (_auditLogs.Count % 10 == 0)
            {
                await SaveAuditLogsAsync();
            }

            _logger.LogInformation($"[AUDIT] {auditLog.Level}: {activity} - {member.DisplayName}");
            
            return auditLog;
        }

        public async Task<List<AuditLog>> GetFamilyActivityLogsAsync(DateTime fromDate)
        {
            return await Task.FromResult(_auditLogs.Where(log => log.Timestamp >= fromDate).ToList());
        }

        private async Task LoadAuditLogsAsync()
        {
            try
            {
                var auditFile = Path.Combine(_dataPath, "audit_logs.json");
                if (File.Exists(auditFile))
                {
                    var encryptedData = await File.ReadAllTextAsync(auditFile);
                    var decryptedData = await DecryptFamilyDataAsync(encryptedData);
                    var logs = JsonSerializer.Deserialize<List<AuditLog>>(decryptedData);
                    
                    if (logs != null)
                    {
                        _auditLogs.AddRange(logs);
                        _logger.LogInformation($"Loaded {logs.Count} audit log entries");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load audit logs");
            }
        }

        private async Task SaveAuditLogsAsync()
        {
            try
            {
                var auditFile = Path.Combine(_dataPath, "audit_logs.json");
                var jsonData = JsonSerializer.Serialize(_auditLogs, new JsonSerializerOptions { WriteIndented = true });
                var encryptedData = await EncryptFamilyDataAsync(jsonData);
                
                await File.WriteAllTextAsync(auditFile, encryptedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save audit logs");
            }
        }

        private string GenerateEncryptionKey()
        {
            // In production, use proper key management
            using (var rng = RandomNumberGenerator.Create())
            {
                var keyBytes = new byte[32]; // 256-bit key
                rng.GetBytes(keyBytes);
                return Convert.ToBase64String(keyBytes);
            }
        }

        private AuditLevel DetermineAuditLevel(string activity)
        {
            if (activity.Contains("Blocked", StringComparison.OrdinalIgnoreCase))
                return AuditLevel.Blocked;
            if (activity.Contains("Failed", StringComparison.OrdinalIgnoreCase) || 
                activity.Contains("Error", StringComparison.OrdinalIgnoreCase))
                return AuditLevel.Security;
            if (activity.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                return AuditLevel.Warning;
            return AuditLevel.Information;
        }
    }
}