using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling

namespace FamilyOS
{
    /// <summary>
    /// AOT-optimized JSON serialization helper using source generation
    /// </summary>
    public static class FamilyOSJsonHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            TypeInfoResolver = FamilyOSJsonContext.Default,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Serialize object to JSON using AOT-optimized source generation
        /// </summary>
        public static string Serialize<T>(T value) where T : class
        {
            return JsonSerializer.Serialize(value, _options);
        }

        /// <summary>
        /// Deserialize JSON string using AOT-optimized source generation
        /// </summary>
        public static T? Deserialize<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        /// <summary>
        /// Serialize to JSON with specific type info for AOT
        /// </summary>
        public static string SerializeConfig(FamilyConfig config)
        {
            return JsonSerializer.Serialize(config, FamilyOSJsonContext.Default.FamilyConfig);
        }

        /// <summary>
        /// Deserialize from JSON with specific type info for AOT
        /// </summary>
        public static FamilyConfig? DeserializeConfig(string json)
        {
            return JsonSerializer.Deserialize(json, FamilyOSJsonContext.Default.FamilyConfig);
        }
    }
}

#pragma warning restore IL3050