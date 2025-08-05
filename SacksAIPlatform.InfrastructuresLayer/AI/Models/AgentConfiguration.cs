namespace SacksAIPlatform.InfrastructuresLayer.AI.Models;

public class AgentConfiguration
{
    public AgentSettings Agent { get; set; } = new();
    public DatabaseConfiguration Database { get; set; } = new();
    public FileHandlingConfiguration FileHandling { get; set; } = new();
}

public class AgentSettings
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public List<AgentCapabilityConfig> Capabilities { get; set; } = new();
    public List<string> ConversationRules { get; set; } = new();
    public Dictionary<string, string> ErrorHandling { get; set; } = new();
}

public class AgentCapabilityConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tools { get; set; } = new();
    public List<string> Examples { get; set; } = new();
}

public class DatabaseConfiguration
{
    public List<string> Repositories { get; set; } = new();
    public List<string> Operations { get; set; } = new();
}

public class FileHandlingConfiguration
{
    public List<string> SupportedFormats { get; set; } = new();
    public List<string> InputDirectories { get; set; } = new();
    public string MaxFileSize { get; set; } = string.Empty;
    public List<string> AllowedOperations { get; set; } = new();
}
