using SacksAIPlatform.InfrastructuresLayer.AI.Models;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;

/// <summary>
/// Conversational AI Agent Interface
/// Provides natural language interaction capabilities similar to ChatGPT
/// </summary>
public interface IConversationalAgent
{
    /// <summary>
    /// Process a natural language message from the user
    /// </summary>
    Task<AgentResponse> ProcessMessageAsync(string userMessage, string userId = "user");
    
    /// <summary>
    /// Get conversation history for a user
    /// </summary>
    Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId = "user");
    
    /// <summary>
    /// Clear conversation history for a user
    /// </summary>
    Task ClearConversationAsync(string userId = "user");
    
    /// <summary>
    /// Teach the agent a new rule or behavior
    /// </summary>
    Task TeachAsync(string rule, string example, string userId = "user");
    
    /// <summary>
    /// Get available capabilities and commands
    /// </summary>
    Task<List<AgentCapability>> GetCapabilitiesAsync();
}
