using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SacksAIPlatform.LogicLayer.Services;

namespace SacksAIPlatform.GuiLayer.Chat;

/// <summary>
/// Pure presentation layer for conversational AI
/// All logic is handled by the business AI service - this just displays messages
/// </summary>
public class ChatInterface
{
    private readonly ProductsInventoryAIService _aiService;
    private readonly ILogger<ChatInterface> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _userId;
    private readonly bool _showInfoLogs;
    private bool _isRunning;

    public ChatInterface(ProductsInventoryAIService aiService, ILogger<ChatInterface> logger, IConfiguration configuration, string userId = "user")
    {
        _aiService = aiService;
        _logger = logger;
        _configuration = configuration;
        _userId = userId;
        _showInfoLogs = _configuration.GetValue<bool>("Chat:ShowInfoLogs", false);
    }

    public async Task StartAsync()
    {
        _isRunning = true;
        
        Console.Clear();
        await DisplayWelcomeMessage();
        
        // Pure chat loop - let LLM handle everything
        while (_isRunning)
        {
            try
            {
                Console.Write("\n🗣️  You: ");
                var userInput = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(userInput))
                    continue;
                    
                // Handle only basic system commands
                if (HandleSystemCommands(userInput))
                    continue;
                
                // Let LLM handle all conversation
                Console.WriteLine("\n🤖 AI Agent: ");
                Console.Write("    Thinking...");
                
                var response = await _aiService.ProcessMessageAsync(userInput, _userId);
                
                Console.Write("\r    ");
                await DisplayResponse(response);
                
                // Let LLM handle confirmations too
                await HandleAnyFollowUp(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat interface");
                Console.WriteLine("\n❌ Something went wrong. Please try again.");
            }
        }
    }

    private async Task DisplayWelcomeMessage()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    🤖 AI PERFUME ASSISTANT                      ║");
        Console.WriteLine("║                                                                  ║");
        Console.WriteLine("║  Pure conversational AI with full database and file access     ║");
        Console.WriteLine("║  Just chat naturally - I'll understand and execute actions     ║");
        Console.WriteLine("║                                                                  ║");
        Console.WriteLine("║  ⌨️  System commands: /quit, /clear, /logs                      ║");
        if (_showInfoLogs)
        {
            Console.WriteLine("║  🔍  Debug mode: Info logs enabled                              ║");
        }
        else
        {
            Console.WriteLine("║  🔇  Quiet mode: Info logs disabled (set Chat:ShowInfoLogs=true)║");
        }
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        
        var capabilities = await _aiService.GetCapabilitiesAsync();
        Console.WriteLine($"\n📋 Ready with {capabilities.Count} capabilities!");
    }

    private bool HandleSystemCommands(string input)
    {
        switch (input.ToLowerInvariant())
        {
            case "/quit":
            case "/exit":
                Console.WriteLine("\n👋 Goodbye!");
                _isRunning = false;
                return true;
                
            case "/clear":
                Console.Clear();
                Console.WriteLine("🗑️  Chat cleared!");
                return true;
                
            case "/logs":
                var currentStatus = _showInfoLogs ? "enabled" : "disabled";
                Console.WriteLine($"\n🔍 Info logs are currently {currentStatus}");
                Console.WriteLine("   To change this setting, modify 'Chat:ShowInfoLogs' in .env file and restart the application.");
                return true;
                
            default:
                return false;
        }
    }

    private Task DisplayResponse(AgentResponse response)
    {
        Console.WriteLine(response.Message);
        
        if (response.Actions.Any())
        {
            Console.WriteLine("\n📋 Actions:");
            foreach (var action in response.Actions)
            {
                Console.WriteLine($"   • {action.Description}");
            }
        }
        
        if (response.Data.Any())
        {
            Console.WriteLine("\n📊 Data:");
            foreach (var item in response.Data)
            {
                Console.WriteLine($"   • {item.Value}");
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleAnyFollowUp(AgentResponse response)
    {
        if (response.RequiresUserConfirmation && !string.IsNullOrEmpty(response.ConfirmationPrompt))
        {
            Console.WriteLine($"\n❓ {response.ConfirmationPrompt}");
            Console.Write("   Your response: ");
            
            var userResponse = Console.ReadLine()?.Trim();
            
            if (!string.IsNullOrEmpty(userResponse))
            {
                // Let LLM handle the confirmation response
                Console.WriteLine("\n🤖 AI Agent: ");
                Console.Write("    Processing...");
                
                var followUpResponse = await _aiService.ProcessMessageAsync(
                    $"CONFIRMATION: {userResponse} (regarding: {response.ConfirmationPrompt})", 
                    _userId);
                
                Console.Write("\r    ");
                await DisplayResponse(followUpResponse);
            }
        }
    }
}
