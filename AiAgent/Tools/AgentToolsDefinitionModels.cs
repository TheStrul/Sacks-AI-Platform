using System.Text.Json.Serialization;

namespace AiAgent.Tools;

/// <summary>
/// Root model for agent tools definition JSON
/// </summary>
public class AgentToolsDefinition
{
    [JsonPropertyName("metadata")]
    public ToolsMetadata? Metadata { get; set; }

    [JsonPropertyName("tools")]
    public List<ToolDefinitionModel>? Tools { get; set; }

    [JsonPropertyName("toolIntegration")]
    public ToolIntegration? ToolIntegration { get; set; }

    [JsonPropertyName("usagePatterns")]
    public UsagePatterns? UsagePatterns { get; set; }

    [JsonPropertyName("extensibility")]
    public Extensibility? Extensibility { get; set; }

    [JsonPropertyName("examples")]
    public Dictionary<string, List<string>>? Examples { get; set; }
}

/// <summary>
/// Metadata information about the tools definition
/// </summary>
public class ToolsMetadata
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("baseClass")]
    public string? BaseClass { get; set; }
}

/// <summary>
/// Model for individual tool definition
/// </summary>
public class ToolDefinitionModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("fullDescription")]
    public string? FullDescription { get; set; }

    [JsonPropertyName("inputFormat")]
    public string? InputFormat { get; set; }

    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    [JsonPropertyName("configurable")]
    public bool Configurable { get; set; }

    [JsonPropertyName("securityRestrictions")]
    public bool SecurityRestrictions { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }

    [JsonPropertyName("operations")]
    public List<ToolOperation>? Operations { get; set; }

    [JsonPropertyName("configuration")]
    public ToolConfiguration? Configuration { get; set; }

    [JsonPropertyName("security")]
    public Dictionary<string, bool>? Security { get; set; }

    [JsonPropertyName("searchCapabilities")]
    public Dictionary<string, object>? SearchCapabilities { get; set; }

    [JsonPropertyName("supportedOperations")]
    public Dictionary<string, object>? SupportedOperations { get; set; }

    [JsonPropertyName("features")]
    public Dictionary<string, bool>? Features { get; set; }

    [JsonPropertyName("apiEndpoint")]
    public string? ApiEndpoint { get; set; }

    [JsonPropertyName("responseTypes")]
    public List<string>? ResponseTypes { get; set; }
}

/// <summary>
/// Model for tool operation definition
/// </summary>
public class ToolOperation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, ParameterDefinition>? Parameters { get; set; }

    [JsonPropertyName("example")]
    public OperationExample? Example { get; set; }

    [JsonPropertyName("examples")]
    public List<OperationExample>? Examples { get; set; }
}

/// <summary>
/// Model for parameter definition
/// </summary>
public class ParameterDefinition
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("default")]
    public string? Default { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Model for operation example
/// </summary>
public class OperationExample
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Model for tool configuration
/// </summary>
public class ToolConfiguration
{
    [JsonPropertyName("settings")]
    public string? Settings { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, ConfigurationProperty>? Properties { get; set; }
}

/// <summary>
/// Model for configuration property
/// </summary>
public class ConfigurationProperty
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("default")]
    public string? Default { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Model for tool integration information
/// </summary>
public class ToolIntegration
{
    [JsonPropertyName("baseClass")]
    public string? BaseClass { get; set; }

    [JsonPropertyName("interface")]
    public string? Interface { get; set; }

    [JsonPropertyName("initialization")]
    public Dictionary<string, List<string>>? Initialization { get; set; }

    [JsonPropertyName("configuration")]
    public Dictionary<string, bool>? Configuration { get; set; }
}

/// <summary>
/// Model for usage patterns information
/// </summary>
public class UsagePatterns
{
    [JsonPropertyName("toolAnalysis")]
    public Dictionary<string, string>? ToolAnalysis { get; set; }

    [JsonPropertyName("toolExecution")]
    public Dictionary<string, string>? ToolExecution { get; set; }

    [JsonPropertyName("responseGeneration")]
    public Dictionary<string, string>? ResponseGeneration { get; set; }
}

/// <summary>
/// Model for extensibility information
/// </summary>
public class Extensibility
{
    [JsonPropertyName("customTools")]
    public bool CustomTools { get; set; }

    [JsonPropertyName("externalToolsSupport")]
    public bool ExternalToolsSupport { get; set; }

    [JsonPropertyName("pluginArchitecture")]
    public bool PluginArchitecture { get; set; }

    [JsonPropertyName("dynamicLoading")]
    public bool DynamicLoading { get; set; }
}