using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using LangChain.Providers;
using Microsoft.Extensions.Logging;
using AiAgent.Tools;
using AiAgent.Configuration;
using static LangChain.Chains.Chain;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiAgent;

/// <summary>
/// LangChain AI Agent implementation with real tool integration
/// Uses a simpler approach with manual tool coordination
/// Now fully configurable using BasicConfig
/// </summary>
public class LangChainAiAgent
{
    private readonly ILogger<LangChainAiAgent> _logger;
    private readonly BasicConfig _config;
    private readonly OpenAiProvider _provider;
    private readonly OpenAiLatestFastChatModel _chatModel;
    private readonly Dictionary<string, object> _tools;
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

        _tools = new Dictionary<string, object>();
        _conversationHistory = new List<string>();

        // Initialize OpenAI provider and model
        _provider = new OpenAiProvider(_config.OpenAi.ApiKey);
        _chatModel = new OpenAiLatestFastChatModel(_provider);

        // Add tools to the agent based on configuration
        InitializeTools(loggerFactory);

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
                var fileSystemTool = new FileSystemAgentTool(
                    loggerFactory.CreateLogger<FileSystemAgentTool>(),
                    _config.BasicToolSettings.FileSystem);
                _tools["file_system"] = fileSystemTool;
                enabledTools.Add(fileSystemTool.Name);
            }

            // Initialize web search tool if enabled
            if (_config.BasicToolSettings.EnableWebSearch)
            {
                var webSearchTool = new WebSearchAgentTool(loggerFactory.CreateLogger<WebSearchAgentTool>());
                _tools["web_search"] = webSearchTool;
                enabledTools.Add(webSearchTool.Name);
            }

            // Initialize calculator tool if enabled
            if (_config.BasicToolSettings.EnableCalculator)
            {
                var calculatorTool = new CalculatorAgentTool(loggerFactory.CreateLogger<CalculatorAgentTool>());
                _tools["calculator"] = calculatorTool;
                enabledTools.Add(calculatorTool.Name);
            }

            // Add all other tools that defined in _config.Tools
            foreach (var tool in _config.ExternalTools)
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
    /// Process a user message and return the agent's response
    /// </summary>
    public async Task<string> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", message);

            // Add user message to history
            _conversationHistory.Add($"User: {message}");

            // Check if the message requires tool usage
            var toolRequest = await AnalyzeForToolUsage(message, cancellationToken);

            string response;
            if (toolRequest.RequiresTool)
            {
                // Execute the tool and get results
                var toolResult = await ExecuteToolAsync(toolRequest.ToolName, toolRequest.ToolInput, cancellationToken);

                // Generate response based on tool results
                response = await GenerateResponseWithToolResult(message, toolRequest.ToolName, toolResult, cancellationToken);
            }
            else
            {
                // Generate direct response without tool usage
                response = await GenerateDirectResponse(message, cancellationToken);
            }

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

    private async Task<ToolRequest> AnalyzeForToolUsage(string message, CancellationToken cancellationToken)
    {
        // Build available tools list based on what's enabled
        var availableToolsDescriptions = new List<string>();

        if (_config.BasicToolSettings.EnableFileSystem)
            availableToolsDescriptions.Add("- file_system: Read, write, list, delete files and directories. Requires JSON input like {\"operation\": \"list\", \"path\": \"C:\\\\temp\"}");

        if (_config.BasicToolSettings.EnableWebSearch)
            availableToolsDescriptions.Add("- web_search: Search the web using DuckDuckGo. Requires plain text search query");

        if (_config.BasicToolSettings.EnableCalculator)
            availableToolsDescriptions.Add("- calculator: Perform mathematical calculations. Requires mathematical expression like \"2 + 2\"");

        var availableToolsText = string.Join("\n", availableToolsDescriptions);

        var analysisPrompt = $@"Analyze the following user message and determine if it requires using one of the available tools.

Available tools:
{availableToolsText}

User message: ""{message}""

Respond with JSON in this format:
{{
  ""requires_tool"": true/false,
  ""tool_name"": ""tool_name_if_needed"",
  ""tool_input"": ""properly_formatted_input_for_tool"",
  ""reasoning"": ""brief explanation""
}}

Examples:
- For ""list files"": {{""requires_tool"": true, ""tool_name"": ""file_system"", ""tool_input"": ""{{\\""operation\\"":\\""list\\""}}""}}
- For ""search for AI news"": {{""requires_tool"": true, ""tool_name"": ""web_search"", ""tool_input"": ""AI news""}}
- For ""calculate 2+2"": {{""requires_tool"": true, ""tool_name"": ""calculator"", ""tool_input"": ""2+2""}}";

        var chain = Set(analysisPrompt, "text") | LLM(_chatModel);
        var result = await chain.RunAsync("text", cancellationToken: cancellationToken);
        var analysisText = result?.ToString() ?? "";

        try
        {
            // Extract JSON from the response
            var jsonMatch = Regex.Match(analysisText, @"\{.*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var jsonElement = JsonDocument.Parse(jsonMatch.Value).RootElement;
                var toolInput = jsonElement.TryGetProperty("tool_input", out var toolInputProp) ? toolInputProp.GetString() ?? "" : "";

                // If it's a file_system tool and the input doesn't look like JSON, wrap it properly
                var toolName = jsonElement.TryGetProperty("tool_name", out var toolNameProp) ? toolNameProp.GetString() ?? "" : "";
                if (toolName == "file_system" && !toolInput.StartsWith("{"))
                {
                    // Default to listing current directory if not specified
                    toolInput = @"{""operation"":""list""}";
                }

                return new ToolRequest
                {
                    RequiresTool = jsonElement.GetProperty("requires_tool").GetBoolean(),
                    ToolName = toolName,
                    ToolInput = toolInput,
                    Reasoning = jsonElement.TryGetProperty("reasoning", out var reasoning) ? reasoning.GetString() ?? "" : ""
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool analysis response: {Response}", analysisText);
        }

        // Default to no tool usage
        return new ToolRequest { RequiresTool = false, ToolName = "", ToolInput = "", Reasoning = "Could not analyze message" };
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

            // Cast to the appropriate tool type and execute
            string result = toolName switch
            {
                "file_system" => await ((FileSystemAgentTool)tool).ToolTask(toolInput, cancellationToken),
                "web_search" => await ((WebSearchAgentTool)tool).ToolTask(toolInput, cancellationToken),
                "calculator" => await ((CalculatorAgentTool)tool).ToolTask(toolInput, cancellationToken),
                _ => $"Error: Unknown tool '{toolName}'"
            };

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
    /// Get available tool information
    /// </summary>
    public List<(string Name, string Description)> GetAvailableTools()
    {
        var tools = new List<(string, string)>();

        if (_config.BasicToolSettings.EnableFileSystem)
            tools.Add(("file_system", "Read, write, list, delete files and directories"));

        if (_config.BasicToolSettings.EnableWebSearch)
            tools.Add(("web_search", "Search the web using DuckDuckGo"));

        if (_config.BasicToolSettings.EnableCalculator)
            tools.Add(("calculator", "Perform mathematical calculations"));

        return tools;
    }

    /// <summary>
    /// Get current agent configuration
    /// </summary>
    public BasicConfig GetConfiguration() => _config;
}

/// <summary>
/// Represents a tool usage request
/// </summary>
public class ToolRequest
{
    public bool RequiresTool { get; set; }
    public string ToolName { get; set; } = "";
    public string ToolInput { get; set; } = "";
    public string Reasoning { get; set; } = "";
}
