using SacksAIPlatform.InfrastructuresLayer.AI.Models;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;

/// <summary>
/// Interface for intent recognition services
/// Allows switching between different LLM providers or pseudo-LLM implementations
/// </summary>
public interface IIntentRecognitionService
{
    /// <summary>
    /// Recognizes the intent from a user message
    /// </summary>
    /// <param name="message">The user's message</param>
    /// <param name="userId">The user ID for context</param>
    /// <returns>The recognized intent with confidence and entities</returns>
    Task<Intent> RecognizeIntentAsync(string message, string userId);
}
