#pragma warning disable OPENAI001 // OpenAI API is in preview and subject to change

namespace SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;

/// <summary>
/// Interface for handling function calls from AI Assistant
/// Follows dependency injection and single responsibility principles
/// </summary>
public interface IFunctionHandler
{
    /// <summary>
    /// The name of the function this handler supports
    /// </summary>
    string FunctionName { get; }
    
    /// <summary>
    /// Execute the function with given arguments
    /// </summary>
    /// <param name="argumentsJson">JSON arguments for the function</param>
    /// <returns>Function execution result as JSON</returns>
    Task<string> ExecuteAsync(string argumentsJson);
    
    /// <summary>
    /// Get the OpenAI function tool definition for this handler
    /// </summary>
    /// <returns>Function tool definition</returns>
    OpenAI.Assistants.FunctionToolDefinition GetToolDefinition();
}
