using System.Text.Json.Serialization;
using PocketFence.FamilyOS.Core;

namespace FamilyOS
{
    // JSON source generation context for Native AOT optimization
    [JsonSerializable(typeof(FamilyConfig))]
    [JsonSerializable(typeof(ParentalControlsConfig))]
    [JsonSerializable(typeof(PasswordPolicy))]
    [JsonSerializable(typeof(UserSession))]
    [JsonSerializable(typeof(SecurityConfig))]
    [JsonSerializable(typeof(PerformanceMetrics))]
    [JsonSerializable(typeof(ActivityReport))]
    [JsonSerializable(typeof(WebFilterRule))]
    [JsonSerializable(typeof(TimeRestriction))]
    [JsonSerializable(typeof(DeviceInfo))]
    [JsonSerializable(typeof(PocketFence.FamilyOS.Core.FamilyMember))]
    [JsonSerializable(typeof(List<PocketFence.FamilyOS.Core.FamilyMember>))]
    [JsonSerializable(typeof(PocketFence.FamilyOS.Core.ContentFilterResult))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Default,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    )]
    internal partial class FamilyOSJsonContext : JsonSerializerContext
    {
    }

    // Models for JSON serialization
    public class FamilyConfig
    {
        public string FamilyName { get; set; } = "";
        public List<string> Members { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class ParentalControlsConfig
    {
        public bool Enabled { get; set; } = true;
        public List<WebFilterRule> WebFilters { get; set; } = new();
        public List<TimeRestriction> TimeRestrictions { get; set; } = new();
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class PasswordPolicy
    {
        public int MinimumLength { get; set; } = 12;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireNumbers { get; set; } = true;
        public bool RequireSpecialChars { get; set; } = true;
        public int MaxAge { get; set; } = 90;
    }

    public class UserSession
    {
        public string UserId { get; set; } = "";
        public string SessionId { get; set; } = "";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime LastActivity { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public class SecurityConfig
    {
        public bool TwoFactorEnabled { get; set; } = false;
        public string EncryptionLevel { get; set; } = "AES256";
        public int SessionTimeoutMinutes { get; set; } = 30;
        public bool AuditingEnabled { get; set; } = true;
    }

    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public double ResponseTime { get; set; }
    }

    public class ActivityReport
    {
        public string UserId { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public int TotalMinutes { get; set; }
        public List<string> WebsitesVisited { get; set; } = new();
        public List<string> AppsUsed { get; set; } = new();
    }

    public class WebFilterRule
    {
        public string Category { get; set; } = "";
        public string Action { get; set; } = "BLOCK";
        public List<string> Domains { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class TimeRestriction
    {
        public string UserId { get; set; } = "";
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<string> DaysOfWeek { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class DeviceInfo
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Version { get; set; } = "";
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}