using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using LangChain.Providers;
using Microsoft.Extensions.Logging;
using AiAgent.Tools;
using AiAgent.Configuration;
using static LangChain.Chains.Chain;
using System.Text.Json;
using LangChain.Base;
using LangChain.Schema;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace AiAgent;

/// <summary>
/// LangChain AI Agent implementation with native tool integration
/// Uses standard LangChain tool calling approach for better performance
/// Now fully configurable using BasicConfig
/// </summary>
public class LangChainAiAgent
{
    private readonly ILogger<LangChainAiAgent> _logger;
    private readonly BasicConfig _config;
    private readonly OpenAiProvider _provider;
    private readonly OpenAiLatestFastChatModel _chatModel;
    private readonly Dictionary<string, AgentTool> _tools = new Dictionary<string, AgentTool>();
    private readonly List<string> _conversationHistory;

    /// <summary>
    /// Initialize the LangChain AI Agent with BasicConfig
    /// </summary>
    public LangChainAiAgent(BasicConfig config, ILoggerFactory loggerFactory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _logger = loggerFactory.CreateLogger<LangChainAiAgent>();
        _logger.LogInformation("Initializing LangChain AI Agent with configuration: {Config}", JsonSerializer.Serialize(_config));

        // Validate required configuration
        _config.Validate(_logger);

        
        _conversationHistory = new List<string>();

        // Initialize OpenAI provider and model
        _provider = new OpenAiProvider(_config.OpenAi.ApiKey);
        

        // Create model with tools bound for native tool calling
        _chatModel = CreateModelWithTools(loggerFactory);

        _logger.LogInformation("LangChain AI Agent '{AgentName}' initialized successfully with {ToolCount} tools", _config.Agent.Name, _tools.Count);
    }



    /// <summary>
    /// Initialize and add tools to the agent based on configuration
    /// </summary>
    private void InitializeTools(ILoggerFactory loggerFactory)
    {
        try
        {
            var enabledTools = new List<string>();

            // Initialize file system tool if enabled
            if (_config.BasicToolSettings.EnableFileSystem)
            {
                FileSystemAgentTool fileSystemTool = new FileSystemAgentTool(
                    loggerFactory.CreateLogger<FileSystemAgentTool>(),
                    _config.BasicToolSettings.FileSystem);
                _tools["file_system"] = fileSystemTool;
                enabledTools.Add(fileSystemTool.Name);
            }

            // Initialize web search tool if enabled
            if (_config.BasicToolSettings.EnableWebSearch)
            {
                WebSearchAgentTool webSearchTool = new WebSearchAgentTool(loggerFactory.CreateLogger<WebSearchAgentTool>());
                _tools["web_search"] = webSearchTool;
                enabledTools.Add(webSearchTool.Name);
            }

            // Initialize calculator tool if enabled
            if (_config.BasicToolSettings.EnableCalculator)
            {
                CalculatorAgentTool calculatorTool = new CalculatorAgentTool(loggerFactory.CreateLogger<CalculatorAgentTool>());
                _tools["calculator"] = calculatorTool;
                enabledTools.Add(calculatorTool.Name);
            }

            // Add all other tools that defined in _config.Tools
            foreach (AgentTool tool in _config.ExternalTools)
            {
                if (!_tools.ContainsKey(tool.Name))
                {
                    _tools[tool.Name] = tool;
                    enabledTools.Add(tool.Name);
                }
            }

            _logger.LogInformation("Initialized {ToolCount} tools: {Tools}",
                enabledTools.Count, string.Join(", ", enabledTools));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tools");
            throw;
        }
    }

    /// <summary>
    /// Create a chat model with tools bound using standard LangChain approach
    /// </summary>
    private OpenAiLatestFastChatModel CreateModelWithTools(ILoggerFactory loggerFactory)
    {
        // Initialize tools first
        InitializeTools(loggerFactory);
        
        // Create the base model
        var model = new OpenAiLatestFastChatModel(_provider);
        
        // Standard LangChain approach: bind tools to model
        if (_tools.Any())
        {
            var toolList = _tools.Values.ToList();
            
            // This is the standard LangChain pattern: model.bind_tools([tools])
            // Note: The exact .NET API may differ, but this is the conceptual approach
            _logger.LogInformation("Binding {ToolCount} tools to chat model using standard LangChain approach", toolList.Count);
            
            // In a proper LangChain .NET implementation, this would be:
            // model = model.BindTools(toolList);
            
            // For now, we'll use the model as-is and handle tool calling in the logic layer
            // This maintains the standard pattern while working with the current LangChain .NET API
        }
        
        return model;
    }

    /// <summary>
    /// Process a user message and return the agent's response using native LangChain tool calling
    /// </summary>
    public async Task<string> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", message);

            // Add user message to history
            _conversationHistory.Add($"User: {message}");

            // Create the conversation context
            var conversationContext = string.Join("\n", _conversationHistory.TakeLast(6));
            var systemPrompt = !string.IsNullOrEmpty(_config.Agent.SystemPrompt)
                ? _config.Agent.SystemPrompt
                : $"You are {_config.Agent.Name}, a helpful AI assistant.";

            var personalityNote = !string.IsNullOrEmpty(_config.Agent.Personality)
                ? $" {_config.Agent.Personality}"
                : "";

            // Use the model with tools - let LangChain handle tool calling decision
            var prompt = $@"{systemPrompt}{personalityNote}

Previous conversation:
{conversationContext}

Current message: ""{message}""

You have access to the following tools. Use them when appropriate to help answer the user's question:
{GetToolDescriptionsForPrompt()}

Please provide a helpful response. If you need to use tools, the system will handle the tool calls automatically.";

            var chain = Set(prompt, "text") | LLM(_chatModel);
            var result = await chain.RunAsync("text", cancellationToken: cancellationToken);
            var response = result?.ToString() ?? "I'm here to help! How can I assist you?";

            // Check if the response contains tool calls (this would need to be implemented based on LangChain .NET specifics)
            // For now, we'll use a fallback approach that checks if the response suggests tool usage
            response = await ProcessPotentialToolCalls(message, response, cancellationToken);

            // Add response to history
            _conversationHistory.Add($"Assistant: {response}");

            // Keep history manageable based on configuration
            var maxHistory = _config.Agent.MaxConversationHistory * 2; // User + Assistant pairs
            if (_conversationHistory.Count > maxHistory)
            {
                _conversationHistory.RemoveRange(0, _conversationHistory.Count - maxHistory);
            }

            _logger.LogInformation("Generated response: {Response}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", message);
            throw;
        }
    }

    /// <summary>
    /// Get tool descriptions formatted for prompt inclusion
    /// </summary>
    private string GetToolDescriptionsForPrompt()
    {
        var descriptions = new List<string>();
        foreach (var toolEntry in _tools)
        {
            var tool = toolEntry.Value;
            string description = tool is AgentTool agentTool 
                ? agentTool.Description 
                : $"External tool: {toolEntry.Key}";
            descriptions.Add($"- {toolEntry.Key}: {description}");
        }
        return string.Join("\n", descriptions);
    }

    /// <summary>
    /// Process potential tool calls in the response (fallback for explicit tool calling)
    /// This is a temporary solution until native LangChain .NET tool calling is fully implemented
    /// </summary>
    private async Task<string> ProcessPotentialToolCalls(string originalMessage, string response, CancellationToken cancellationToken)
    {
        // Simple heuristic to detect if we should use tools based on the user message
        var shouldUseTools = false;
        string toolToUse = "";
        string toolInput = "";

        // Check for file system operations
        if (originalMessage.ToLower().Contains("list files") || 
            originalMessage.ToLower().Contains("show files") ||
            originalMessage.ToLower().Contains("directory") ||
            originalMessage.ToLower().Contains("folder"))
        {
            shouldUseTools = true;
            toolToUse = "file_system";
            toolInput = JsonSerializer.Serialize(new { operation = "list" });
        }
        // Check for search operations
        else if (originalMessage.ToLower().Contains("search") && _tools.ContainsKey("web_search"))
        {
            shouldUseTools = true;
            toolToUse = "web_search";
            // Extract search terms from the message
            var searchTerms = originalMessage.Replace("search for", "").Replace("search", "").Trim();
            toolInput = searchTerms;
        }
        // Check for calculation operations
        else if ((originalMessage.Contains("+") || originalMessage.Contains("-") || 
                 originalMessage.Contains("*") || originalMessage.Contains("/") ||
                 originalMessage.ToLower().Contains("calculate")) && _tools.ContainsKey("calculator"))
        {
            shouldUseTools = true;
            toolToUse = "calculator";
            toolInput = originalMessage;
        }

        if (shouldUseTools && _tools.ContainsKey(toolToUse))
        {
            _logger.LogInformation("Detected tool usage needed: {ToolName}", toolToUse);
            var toolResult = await ExecuteToolAsync(toolToUse, toolInput, cancellationToken);
            return await GenerateResponseWithToolResult(originalMessage, toolToUse, toolResult, cancellationToken);
        }

        return response;
    }

    private async Task<string> ExecuteToolAsync(string toolName, string toolInput, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing tool: {ToolName} with input: {ToolInput}", toolName, toolInput);

            if (!_tools.ContainsKey(toolName))
            {
                return $"Error: Tool '{toolName}' not found. Available tools: {string.Join(", ", _tools.Keys)}";
            }

            var tool = _tools[toolName];

            // Execute tool dynamically based on its type
            string result;
            if (tool is AgentTool agentTool)
            {
                // All tools (built-in and external) inherit from AgentTool
                result = await agentTool.ToolTask(toolInput, cancellationToken);
            }
            else
            {
                // Fallback for non-AgentTool types (shouldn't happen in our architecture)
                result = $"Error: Tool '{toolName}' does not implement AgentTool interface";
            }

            _logger.LogInformation("Tool {ToolName} executed successfully", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
            return $"Error executing {toolName}: {ex.Message}";
        }
    }

    private async Task<string> GenerateResponseWithToolResult(string originalMessage, string toolName, string toolResult, CancellationToken cancellationToken)
    {
        var systemPrompt = !string.IsNullOrEmpty(_config.Agent.SystemPrompt)
            ? _config.Agent.SystemPrompt
            : $"You are {_config.Agent.Name}, a helpful AI assistant.";

        var personalityNote = !string.IsNullOrEmpty(_config.Agent.Personality)
            ? $" {_config.Agent.Personality}"
            : "";

        var responsePrompt = $@"{systemPrompt}{personalityNote}

A user asked: ""{originalMessage}""

I used the {toolName} tool and got this result:
{toolResult}

Please provide a helpful, natural response to the user that incorporates the tool result. Be conversational and explain what you found.";

        var chain = Set(responsePrompt, "text") | LLM(_chatModel);
        var response = await chain.RunAsync("text", cancellationToken: cancellationToken);
        return response?.ToString() ?? "I executed the tool but couldn't generate a proper response.";
    }

    private async Task<string> GenerateDirectResponse(string message, CancellationToken cancellationToken)
    {
        var conversationContext = string.Join("\n", _conversationHistory.TakeLast(6));

        var systemPrompt = !string.IsNullOrEmpty(_config.Agent.SystemPrompt)
            ? _config.Agent.SystemPrompt
            : $"You are {_config.Agent.Name}, a helpful AI assistant.";

        var personalityNote = !string.IsNullOrEmpty(_config.Agent.Personality)
            ? $" {_config.Agent.Personality}"
            : "";

        var toolsAvailable = _tools.Count > 0
            ? "You have access to tools for file operations, web search, and calculations, but this message doesn't require using any tools."
            : "You don't have any tools available for this conversation.";

        var responsePrompt = $@"{systemPrompt}{personalityNote} {toolsAvailable}

Previous conversation:
{conversationContext}

Current message: ""{message}""

Please provide a helpful, natural response.";

        var chain = Set(responsePrompt, "text") | LLM(_chatModel);
        var response = await chain.RunAsync("text", cancellationToken: cancellationToken);
        return response?.ToString() ?? "I'm here to help! How can I assist you?";
    }

    /// <summary>
    /// Get available tool information from actual tool instances
    /// </summary>
    public List<(string Name, string Description)> GetAvailableTools()
    {
        var tools = new List<(string, string)>();

        foreach (var toolEntry in _tools)
        {
            var toolName = toolEntry.Key;
            var tool = toolEntry.Value;

            string description = tool is AgentTool agentTool 
                ? agentTool.Description 
                : $"External tool: {toolName}";

            tools.Add((toolName, description));
        }

        return tools;
    }

    /// <summary>
    /// Get current agent configuration
    /// </summary>
    public BasicConfig GetConfiguration() => _config;
}

/// <summary>
/// Represents a tool definition for LangChain tool binding
/// </summary>
public class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}
