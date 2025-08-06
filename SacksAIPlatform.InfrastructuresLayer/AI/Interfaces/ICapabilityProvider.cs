using SacksAIPlatform.InfrastructuresLayer.AI.Models;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;

/// <summary>
/// Interface for components that provide capabilities to the AI agent system
/// Allows services to expose their functionality as agent capabilities
/// </summary>
public interface ICapabilityProvider
{
    /// <summary>
    /// Gets the capability information this provider offers
    /// </summary>
    Task<AgentCapability> GetCapabilityAsync();
    
    /// <summary>
    /// Validates if the provider supports a specific operation
    /// </summary>
    bool SupportsOperation(string operation);
    
    /// <summary>
    /// Gets configuration details about the capability
    /// </summary>
    Dictionary<string, object> GetCapabilityConfiguration();
}
