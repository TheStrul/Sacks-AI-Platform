using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Services;

/// <summary>
/// Pure LLM-driven conversational agent - completely generic and reusable
/// No business logic dependencies - can be used by any project
/// </summary>
public class GenericLLMAgent : IConversationalAgent
{
    private readonly ILogger<GenericLLMAgent> _logger;
    private readonly IConfiguration _configuration;
    private readonly IIntentRecognitionService _intentService;
    private readonly AgentConfiguration _agentConfig;
    private readonly Func<string, string, Task<AgentResponse>>? _customActionHandler;

    public GenericLLMAgent(
        ILogger<GenericLLMAgent> logger,
        IConfiguration configuration,
        IIntentRecognitionService intentService,
        Func<string, string, Task<AgentResponse>>? customActionHandler = null)
    {
        _logger = logger;
        _configuration = configuration;
        _intentService = intentService;
        _customActionHandler = customActionHandler;
        
        // Load agent configuration from JSON
        _agentConfig = LoadAgentConfiguration();
        
        _logger.LogInformation("Generic LLM Agent initialized with {CapabilityCount} capabilities", 
            _agentConfig.Agent.Capabilities.Count);
    }

    public async Task<AgentResponse> ProcessMessageAsync(string message, string userId = "user")
    {
        _logger.LogInformation("Processing message from user {UserId}: {Message}", userId, message);
        
        try
        {
            // Build context for the LLM including available capabilities
            var systemContext = BuildSystemContext();
            var fullPrompt = $"{systemContext}\n\nUser: {message}";
            
            // Use LLM to process the entire conversation
            var intent = await _intentService.RecognizeIntentAsync(fullPrompt, userId);
            
            // If custom action handler is provided, use it; otherwise use pure LLM response
            if (_customActionHandler != null)
            {
                return await _customActionHandler(message, userId);
            }
            
            // Pure LLM response - no hardcoded logic
            return new AgentResponse
            {
                Message = intent.Description ?? "I'm here to help. How can I assist you?",
                Type = AgentResponseType.Text,
                Data = new Dictionary<string, object>
                {
                    { "Intent", intent.Name },
                    { "Confidence", intent.Confidence },
                    { "ProcessedBy", "GenericLLMAgent" }
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

    public Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId = "user")
    {
        // Return empty list for now - could implement with database storage
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
        // In a full implementation, this would store learning rules
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

    private string BuildSystemContext()
    {
        var context = $@"
{_agentConfig.Agent.SystemPrompt}

AVAILABLE CAPABILITIES:
{string.Join("\n", _agentConfig.Agent.Capabilities.Select(c => $"- {c.Name}: {c.Description}"))}

CONVERSATION RULES:
{string.Join("\n", _agentConfig.Agent.ConversationRules.Select(r => $"- {r}"))}

CURRENT SESSION: You are a helpful AI assistant. Respond naturally and helpfully to user queries.
";

        return context;
    }

    private string GetErrorMessage(string errorType)
    {
        return _agentConfig.Agent.ErrorHandling.TryGetValue(errorType, out var message) 
            ? message 
            : "An unexpected error occurred. Please try again.";
    }
}
