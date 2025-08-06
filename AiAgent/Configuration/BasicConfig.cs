namespace AiAgent.Configuration;

using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;

/// <summary>
/// Basic configuration class for LangChainAiAgent containing all available options
/// </summary>
public class BasicConfig
{
    /// <summary>
    /// OpenAI API configuration
    /// </summary>
    public OpenAiSettings OpenAi { get; set; } = new();

    /// <summary>
    /// Agent configuration settings
    /// </summary>
    public AgentSettings Agent { get; set; } = new();

    /// <summary>
    /// Basic Tool configuration settings
    /// </summary>
    public BasicToolSettings BasicToolSettings { get; set; } = new();

    public Collection<AgentTool> ExternalTools { get; set; } = new();

    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Creates a default configuration with all tools enabled and standard settings
    /// </summary>
    /// <param name="openAiApiKey">OpenAI API key (optional - can be set later or via environment)</param>
    /// <returns>A BasicConfig instance with default values</returns>
    public static BasicConfig CreateDefault(string? openAiApiKey = null)
    {
        return new BasicConfig
        {
            OpenAi = new OpenAiSettings
            {
                ApiKey = openAiApiKey ?? string.Empty,
                Model = "gpt-4o-mini",
                Temperature = null, // Use model default
                MaxTokens = null    // Use model default
            },
            Agent = new AgentSettings
            {
                Name = "LangChain AI Agent",
                MaxConversationHistory = 10,
                SystemPrompt = "You are a helpful AI assistant with access to various tools to help users with file operations, web searches, and calculations.",
                Personality = "You are friendly, professional, and efficient."
            },
            BasicToolSettings = new BasicToolSettings
            {
                EnableFileSystem = true,
                EnableWebSearch = true,
                EnableCalculator = true,
                FileSystem = new FileSystemToolSettings
                {
                    MaxFilesToList = 20,
                    MaxDirectoriesToList = 10,
                    MaxFileContentSize = 2000,
                    AllowedRootDirectories = new List<string>(), // Empty = all allowed
                    BlockedExtensions = new List<string> { ".exe", ".dll", ".bin", ".sys" }
                },
                WebSearch = new WebSearchToolSettings
                {
                    MaxResults = 5,
                    TimeoutSeconds = 30,
                    UserAgent = "LangChain AI Agent 1.0"
                },
                Calculator = new CalculatorToolSettings
                {
                    EnableAdvancedFunctions = true,
                    DecimalPrecision = 10
                }
            },
            ExternalTools = new Collection<AgentTool>(),
            Logging = new LoggingSettings
            {
                MinimumLevel = "Information",
                EnableConsole = false,
                EnableFile = true,
                FilePathPattern = "logs/aiagent-.log",
                RetainedFileCountLimit = 7,
                ConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                FileOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            }
        };
    }

    /// <summary>
    /// Creates a minimal configuration with only basic settings (no tools enabled)
    /// </summary>
    /// <param name="openAiApiKey">OpenAI API key (required)</param>
    /// <returns>A BasicConfig instance with minimal settings</returns>
    public static BasicConfig CreateMinimal(string openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey))
            throw new ArgumentException("OpenAI API key is required for minimal configuration", nameof(openAiApiKey));

        return new BasicConfig
        {
            OpenAi = new OpenAiSettings
            {
                ApiKey = openAiApiKey,
                Model = "gpt-4o-mini"
            },
            Agent = new AgentSettings
            {
                Name = "AI Assistant",
                MaxConversationHistory = 5
            },
            BasicToolSettings = new BasicToolSettings
            {
                EnableFileSystem = false,
                EnableWebSearch = false,
                EnableCalculator = false
            },
            Logging = new LoggingSettings
            {
                MinimumLevel = "Warning",
                EnableConsole = true,
                EnableFile = false
            }
        };
    }

    /// <summary>
    /// Validates the configuration for consistency and required values
    /// </summary>
    /// <param name="logger">Logger for validation messages</param>
    internal void Validate(ILogger<LangChainAiAgent> logger)
    {
        // Validate OpenAI settings
        if (string.IsNullOrEmpty(OpenAi.ApiKey))
        {
            logger.LogError("OpenAI API key is required");
            throw new InvalidOperationException("OpenAI API key is required in configuration");
        }

        if (string.IsNullOrEmpty(OpenAi.Model))
        {
            logger.LogError("OpenAI model is required");
            throw new InvalidOperationException("OpenAI model is required in configuration");
        }

        if (OpenAi.Temperature.HasValue && (OpenAi.Temperature < 0.0f || OpenAi.Temperature > 2.0f))
        {
            logger.LogError("OpenAI temperature must be between 0.0 and 2.0");
            throw new InvalidOperationException("OpenAI temperature must be between 0.0 and 2.0");
        }

        if (OpenAi.MaxTokens.HasValue && OpenAi.MaxTokens <= 0)
        {
            logger.LogError("OpenAI max tokens must be greater than 0");
            throw new InvalidOperationException("OpenAI max tokens must be greater than 0");
        }

        // Validate Agent settings
        if (string.IsNullOrEmpty(Agent.Name))
        {
            logger.LogWarning("Agent name is empty, using default");
            Agent.Name = "LangChain AI Agent";
        }

        if (Agent.MaxConversationHistory <= 0)
        {
            logger.LogWarning("Invalid conversation history limit, using default of 10");
            Agent.MaxConversationHistory = 10;
        }

        // Validate tool settings
        if (BasicToolSettings.FileSystem.MaxFilesToList <= 0)
        {
            logger.LogWarning("Invalid MaxFilesToList, using default of 20");
            BasicToolSettings.FileSystem.MaxFilesToList = 20;
        }

        if (BasicToolSettings.FileSystem.MaxDirectoriesToList <= 0)
        {
            logger.LogWarning("Invalid MaxDirectoriesToList, using default of 10");
            BasicToolSettings.FileSystem.MaxDirectoriesToList = 10;
        }

        if (BasicToolSettings.FileSystem.MaxFileContentSize <= 0)
        {
            logger.LogWarning("Invalid MaxFileContentSize, using default of 2000");
            BasicToolSettings.FileSystem.MaxFileContentSize = 2000;
        }

        if (BasicToolSettings.WebSearch.MaxResults <= 0)
        {
            logger.LogWarning("Invalid web search MaxResults, using default of 5");
            BasicToolSettings.WebSearch.MaxResults = 5;
        }

        if (BasicToolSettings.WebSearch.TimeoutSeconds <= 0)
        {
            logger.LogWarning("Invalid web search timeout, using default of 30 seconds");
            BasicToolSettings.WebSearch.TimeoutSeconds = 30;
        }

        if (BasicToolSettings.Calculator.DecimalPrecision <= 0)
        {
            logger.LogWarning("Invalid calculator precision, using default of 10");
            BasicToolSettings.Calculator.DecimalPrecision = 10;
        }

        // Validate logging settings
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        if (!validLogLevels.Contains(Logging.MinimumLevel, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("Invalid log level '{LogLevel}', using 'Information'", Logging.MinimumLevel);
            Logging.MinimumLevel = "Information";
        }

        if (Logging.RetainedFileCountLimit <= 0)
        {
            logger.LogWarning("Invalid retained file count limit, using default of 7");
            Logging.RetainedFileCountLimit = 7;
        }

        logger.LogInformation("Configuration validation completed successfully");
    }
}

/// <summary>
/// OpenAI API configuration settings
/// </summary>
public class OpenAiSettings
{
    /// <summary>
    /// OpenAI API key (required)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI model to use (default: gpt-4o-mini)
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Temperature for model responses (0.0 to 2.0, default: null for model default)
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens for model responses (default: null for model default)
    /// </summary>
    public int? MaxTokens { get; set; }
}

/// <summary>
/// Agent behavior configuration settings
/// </summary>
public class AgentSettings
{
    /// <summary>
    /// Name of the AI agent (default: LangChain AI Agent)
    /// </summary>
    public string Name { get; set; } = "LangChain AI Agent";

    /// <summary>
    /// Maximum number of conversation history items to keep (default: 10)
    /// </summary>
    public int MaxConversationHistory { get; set; } = 10;

    /// <summary>
    /// System prompt or instructions for the agent (optional)
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Agent personality description (optional)
    /// </summary>
    public string Personality { get; set; } = string.Empty;
}

/// <summary>
/// Tool configuration settings
/// </summary>
public class BasicToolSettings
{
    /// <summary>
    /// Enable/disable file system tool (default: true)
    /// </summary>
    public bool EnableFileSystem { get; set; } = true;

    /// <summary>
    /// Enable/disable web search tool (default: true)
    /// </summary>
    public bool EnableWebSearch { get; set; } = true;

    /// <summary>
    /// Enable/disable calculator tool (default: true)
    /// </summary>
    public bool EnableCalculator { get; set; } = true;

    /// <summary>
    /// File system tool specific settings
    /// </summary>
    public FileSystemToolSettings FileSystem { get; set; } = new();

    /// <summary>
    /// Web search tool specific settings
    /// </summary>
    public WebSearchToolSettings WebSearch { get; set; } = new();

    /// <summary>
    /// Calculator tool specific settings
    /// </summary>
    public CalculatorToolSettings Calculator { get; set; } = new();

    public Collection<AgentTool> CustomTools { get; set; } = new Collection<AgentTool>();
}

/// <summary>
/// File system tool specific configuration
/// </summary>
public class FileSystemToolSettings
{
    /// <summary>
    /// Maximum number of files to list in directory operations (default: 20)
    /// </summary>
    public int MaxFilesToList { get; set; } = 20;

    /// <summary>
    /// Maximum number of directories to list in directory operations (default: 10)
    /// </summary>
    public int MaxDirectoriesToList { get; set; } = 10;

    /// <summary>
    /// Maximum file content size to read in characters (default: 2000)
    /// </summary>
    public int MaxFileContentSize { get; set; } = 2000;

    /// <summary>
    /// Allowed root directories for file operations (empty = all allowed)
    /// </summary>
    public List<string> AllowedRootDirectories { get; set; } = new();

    /// <summary>
    /// Blocked file extensions for read/write operations
    /// </summary>
    public List<string> BlockedExtensions { get; set; } = new();
}

/// <summary>
/// Web search tool specific configuration
/// </summary>
public class WebSearchToolSettings
{
    /// <summary>
    /// Maximum number of search results to return (default: 5)
    /// </summary>
    public int MaxResults { get; set; } = 5;

    /// <summary>
    /// Search timeout in seconds (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Custom user agent for web requests (optional)
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Calculator tool specific configuration
/// </summary>
public class CalculatorToolSettings
{
    /// <summary>
    /// Enable advanced mathematical functions (default: true)
    /// </summary>
    public bool EnableAdvancedFunctions { get; set; } = true;

    /// <summary>
    /// Maximum precision for decimal calculations (default: 10)
    /// </summary>
    public int DecimalPrecision { get; set; } = 10;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Minimum log level (default: Information)
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Enable console logging (default: true)
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// Enable file logging (default: true)
    /// </summary>
    public bool EnableFile { get; set; } = true;

    /// <summary>
    /// Log file path pattern (default: logs/aiagent-.log)
    /// </summary>
    public string FilePathPattern { get; set; } = "logs/aiagent-.log";

    /// <summary>
    /// Number of log files to retain (default: 7)
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 7;

    /// <summary>
    /// Console log output template
    /// </summary>
    public string ConsoleOutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// File log output template
    /// </summary>
    public string FileOutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
}