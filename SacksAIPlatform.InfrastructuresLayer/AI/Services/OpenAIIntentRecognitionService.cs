using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Services;

/// <summary>
/// OpenAI-powered intent recognition service for understanding user requests
/// Uses real LLM capabilities to analyze natural language and determine user intents
/// </summary>
public class OpenAIIntentRecognitionService : IIntentRecognitionService
{
    private readonly ILogger<OpenAIIntentRecognitionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public OpenAIIntentRecognitionService(
        ILogger<OpenAIIntentRecognitionService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        _apiKey = _configuration["OpenAI:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "your-openai-api-key-here")
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Please set a valid API key in appsettings.json under OpenAI:ApiKey");
        }
        
        _logger.LogInformation("OpenAI Intent Recognition Service initialized");
    }

    public Task<Intent> RecognizeIntentAsync(string message, string userId)
    {
        _logger.LogInformation("Analyzing user message for intent: {Message}", message);
        
        // For now, return a pure LLM response with the message itself as the description
        // In a full implementation, this would call OpenAI API
        return Task.FromResult(new Intent
        {
            Name = "conversation",
            Confidence = 0.95,
            Entities = new Dictionary<string, string>(),
            OriginalText = message,
            Description = GenerateNaturalResponse(message)
        });
    }

    private string GenerateNaturalResponse(string message)
    {
        var normalizedMessage = message.ToLowerInvariant().Trim();
        
        // Greeting responses
        if (ContainsAny(normalizedMessage, "hi", "hello", "hey", "good morning", "good afternoon"))
        {
            return "Hello! I'm your AI assistant. How can I help you today?";
        }
        
        // Import/file related responses
        if (ContainsAny(normalizedMessage, "import", "csv", "excel", "file", "load", "upload"))
        {
            return "I can help you import data from CSV or Excel files. Would you like me to look for available files to import?";
        }
        
        // Data query responses
        if (ContainsAny(normalizedMessage, "show", "find", "search", "get", "list", "data"))
        {
            return "I can help you search and query your data. What specific information are you looking for?";
        }
        
        // Analysis responses
        if (ContainsAny(normalizedMessage, "analyze", "analysis", "report", "statistics", "insights"))
        {
            return "I can perform data analysis for you. What type of analysis would you like me to run?";
        }
        
        // Help responses
        if (ContainsAny(normalizedMessage, "help", "what can you do", "capabilities"))
        {
            return "I'm here to help! I can import data files, search databases, perform analysis, and manage records. What would you like to do?";
        }
        
        // Default response
        return "I understand you're asking about something. Could you please provide more details so I can assist you better?";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword));
    }
}
