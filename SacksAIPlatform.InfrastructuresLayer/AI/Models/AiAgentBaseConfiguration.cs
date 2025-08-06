using System.Text.Json.Serialization;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Models;

/// <summary>
/// Configuration model for OpenAI Assistant creation options
/// </summary>
public class AiAgentBaseConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o";

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("functions")]
    public List<FunctionDefinition>? Functions { get; set; }

    /// <summary>
    /// Load assistant configuration from JSON file
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file</param>
    /// <returns>AssistantConfiguration object</returns>
    public static async Task<AiAgentBaseConfiguration> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Assistant configuration file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var config = System.Text.Json.JsonSerializer.Deserialize<AiAgentBaseConfiguration>(json);
        
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize assistant configuration");
        }

        return config;
    }

    /// <summary>
    /// Get the default configuration file path relative to the AI folder
    /// </summary>
    /// <returns>Default configuration file path</returns>
    public static string GetDefaultConfigPath()
    {
        // Start from the current directory and walk up to find the solution root
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = currentDir;
        
        // Look for the AI configuration file in the InfrastructuresLayer
        while (!string.IsNullOrEmpty(searchDir))
        {
            var infraPath = Path.Combine(searchDir, "SacksAIPlatform.InfrastructuresLayer", "AI", "assistant-config.json");
            if (File.Exists(infraPath))
            {
                return infraPath;
            }
            
            // Also check if we're running from within the InfrastructuresLayer
            var directAiPath = Path.Combine(searchDir, "AI", "assistant-config.json");
            if (File.Exists(directAiPath))
            {
                return directAiPath;
            }
            
            var parent = Directory.GetParent(searchDir);
            searchDir = parent?.FullName;
        }

        // Fallback: use a relative path that should work from most locations
        return Path.Combine("SacksAIPlatform.InfrastructuresLayer", "AI", "assistant-config.json");
    }
}

/// <summary>
/// Function definition for OpenAI Assistant Function Calling
/// </summary>
public class FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public FunctionParameters Parameters { get; set; } = new();
}

/// <summary>
/// Function parameters definition
/// </summary>
public class FunctionParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, PropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// Property definition for function parameters
/// </summary>
public class PropertyDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("default")]
    public object? Default { get; set; }
}
