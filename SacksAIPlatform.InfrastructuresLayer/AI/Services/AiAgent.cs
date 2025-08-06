using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using OpenAI.Chat;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Services;

/// <summary>
/// AI Agent that uses real LLM via API key for all decision-making
/// LLM decides user intent and when to activate capabilities
/// Clean, focused implementation with minimal dependencies
/// </summary>
public class AiAgent : IConversationalAgent
{
    private readonly ILogger<AiAgent> _logger;
    private readonly IConfiguration _configuration;
    private readonly AgentConfiguration _agentConfig;
    private readonly Func<string, string, Task<AgentResponse>>? _capabilityHandler;
    private readonly ChatClient _chatClient;
    private readonly string _model;

    public AiAgent(
        ILogger<AiAgent> logger,
        IConfiguration configuration,
        Func<string, string, Task<AgentResponse>>? capabilityHandler = null)
    {
        _logger = logger;
        _configuration = configuration;
        _capabilityHandler = capabilityHandler;
        
        // Load agent configuration
        _agentConfig = LoadAgentConfiguration();
        
        // Initialize OpenAI client - required for this agent
        var apiKey = _configuration["OpenAI:ApiKey"] ?? string.Empty;
        _model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("sk-"))
        {
            throw new InvalidOperationException("Valid OpenAI API key is required for AiAgent. Please configure OpenAI:ApiKey in appsettings.json");
        }
        
        _chatClient = new ChatClient(_model, apiKey);
        _logger.LogInformation("AI Agent initialized with OpenAI model: {Model}", _model);
    }

    public async Task<AgentResponse> ProcessMessageAsync(string message, string userId = "user")
    {
        _logger.LogInformation("Processing message from user {UserId}: {Message}", userId, message);
        
        try
        {
            // Create system prompt with capabilities information
            var systemPrompt = BuildSystemPrompt();
            
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(message)
            };

            // Get LLM response
            var response = await _chatClient.CompleteChatAsync(messages);
            var aiResponse = response.Value.Content[0].Text;

            // Let LLM decide if capabilities are needed
            var capabilityDecision = await AnalyzeCapabilityNeed(message, aiResponse);
            
            if (capabilityDecision.NeedsCapabilities && _capabilityHandler != null)
            {
                // Execute capabilities and get enhanced response
                var capabilityResult = await _capabilityHandler(message, userId);
                
                // Enhance the response with LLM interpretation
                return await EnhanceResponseWithLLM(message, capabilityResult, aiResponse);
            }

            // Direct conversational response
            return new AgentResponse
            {
                Message = aiResponse,
                Type = AgentResponseType.Text,
                Data = new Dictionary<string, object>
                {
                    { "ProcessedBy", "DirectLLM" },
                    { "Model", _model },
                    { "RequiresCapabilities", false }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {ErrorMessage}", ex.Message);
            return new AgentResponse
            {
                Message = GetErrorMessage("generalError"),
                Type = AgentResponseType.Error
            };
        }
    }

    /// <summary>
    /// Build system prompt with capabilities information for LLM decision-making
    /// </summary>
    private string BuildSystemPrompt()
    {
        var capabilitiesInfo = string.Join("\n", _agentConfig.Agent.Capabilities.Select(c => 
            $"- {c.Name}: {c.Description}\n  Examples: {string.Join(", ", c.Examples.Take(2))}"));

        return $@"You are an AI assistant that can decide when to use external capabilities.

{_agentConfig.Agent.SystemPrompt}

AVAILABLE CAPABILITIES:
{capabilitiesInfo}

DECISION RULES:
- Analyze user requests to determine if they need external capabilities
- For file operations, data processing, or analysis tasks, indicate capability usage
- For general conversation, respond directly
- Be natural and helpful in your responses

When you determine capabilities are needed, include a clear indication in your response.
When responding to general questions, provide direct helpful answers.
";
    }

    /// <summary>
    /// Analyze if the user request and LLM response indicate capabilities are needed
    /// </summary>
    private async Task<CapabilityDecision> AnalyzeCapabilityNeed(string userMessage, string llmResponse)
    {
        // Use a second LLM call to make a clear decision
        var analysisPrompt = $@"
Analyze this conversation to determine if external capabilities should be used:

User Request: {userMessage}
AI Response: {llmResponse}

Available capabilities involve: file operations, data processing, Excel/CSV reading, folder operations.

Respond with ONLY:
- 'CAPABILITIES_NEEDED' if the user is asking for file operations, data analysis, or similar tasks
- 'CONVERSATION_ONLY' if this is general conversation

Decision: ";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a decision engine. Analyze requests and respond with only the specified format."),
            new UserChatMessage(analysisPrompt)
        };

        var response = await _chatClient.CompleteChatAsync(messages);
        var decision = response.Value.Content[0].Text.Trim();

        return new CapabilityDecision
        {
            NeedsCapabilities = decision.Contains("CAPABILITIES_NEEDED"),
            Reasoning = decision
        };
    }

    /// <summary>
    /// Enhance capability results with LLM interpretation
    /// </summary>
    private async Task<AgentResponse> EnhanceResponseWithLLM(string originalMessage, AgentResponse capabilityResult, string originalLlmResponse)
    {
        try
        {
            var enhancementPrompt = $@"
User asked: {originalMessage}

Capability execution result: {capabilityResult.Message}

Your initial response was: {originalLlmResponse}

Please provide a natural, helpful response that incorporates the capability results.
Be conversational and explain what was found or accomplished.
";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are helping interpret results from external capabilities. Be natural and helpful."),
                new UserChatMessage(enhancementPrompt)
            };

            var response = await _chatClient.CompleteChatAsync(messages);
            var enhancedMessage = response.Value.Content[0].Text;

            return new AgentResponse
            {
                Message = enhancedMessage,
                Type = capabilityResult.Type,
                Data = new Dictionary<string, object>
                {
                    { "ProcessedBy", "LLM+Capabilities" },
                    { "Model", _model },
                    { "OriginalCapabilityData", capabilityResult.Data },
                    { "RequiresCapabilities", true }
                },
                Actions = capabilityResult.Actions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing response with LLM");
            // Return original capability result if enhancement fails
            return capabilityResult;
        }
    }

    public Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId = "user")
    {
        // Could implement with database storage in the future
        return Task.FromResult(new List<ConversationMessage>());
    }

    public Task ClearConversationAsync(string userId = "user")
    {
        _logger.LogInformation("Conversation cleared for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task TeachAsync(string rule, string example, string userId = "user")
    {
        _logger.LogInformation("Teaching new rule to user {UserId}: {Rule}", userId, rule);
        // Could implement learning storage in the future
        return Task.CompletedTask;
    }

    public Task<List<AgentCapability>> GetCapabilitiesAsync()
    {
        var capabilities = _agentConfig.Agent.Capabilities.Select(c => new AgentCapability
        {
            Name = c.Name,
            Description = c.Description,
            Examples = c.Examples,
            Available = true
        }).ToList();
        
        return Task.FromResult(capabilities);
    }

    private AgentConfiguration LoadAgentConfiguration()
    {
        try
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "agent-config.json");
            var configJson = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AgentConfiguration>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return config ?? new AgentConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent configuration, using defaults");
            return new AgentConfiguration();
        }
    }

    private string GetErrorMessage(string errorType)
    {
        return _agentConfig.Agent.ErrorHandling.TryGetValue(errorType, out var message) 
            ? message 
            : "An unexpected error occurred. Please try again.";
    }
}

/// <summary>
/// Result of capability decision analysis
/// </summary>
public class CapabilityDecision
{
    public bool NeedsCapabilities { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}
