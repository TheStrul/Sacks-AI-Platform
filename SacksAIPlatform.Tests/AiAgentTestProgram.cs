using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using DotNetEnv;

namespace SacksAIPlatform.Tests;

/// <summary>
/// Interactive test program for AiAgent
/// Run this to test the AiAgent with real OpenAI API calls
/// </summary>
public class AiAgentTestProgram
{
    public static async Task RunInteractiveTest(string[] args)
    {
        // Load environment variables from .env file
        try
        {
            Env.Load();
            Console.WriteLine("✅ Environment variables loaded from .env file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Could not load .env file: {ex.Message}");
            Console.WriteLine("Will try to use environment variables directly...");
        }
        
        Console.WriteLine("=== AI Agent Test Program ===");
        Console.WriteLine("Choose test mode:");
        Console.WriteLine("1. Interactive Chat Test");
        Console.WriteLine("2. Quick Manual Test");
        Console.WriteLine();
        Console.Write("Enter choice (1 or 2): ");
        
        var choice = Console.ReadLine();
        
        if (choice == "2")
        {
            await ManualAiAgentTest.RunTest();
            return;
        }

        // Continue with interactive test
        Console.WriteLine("Starting Interactive Chat Test...");
        Console.WriteLine();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<AiAgent>();

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                                   GetApiKeyFromUser(),
                ["OpenAI:Model"] = "gpt-4o"
            })
            .Build();

        try
        {
            // Create capability handler for testing
            Func<string, string, Task<AgentResponse>> testCapabilityHandler = async (message, userId) =>
            {
                Console.WriteLine($"[CAPABILITY] Executing capability for: {message}");
                await Task.Delay(500); // Simulate work

                // Mock file operation result
                return new AgentResponse
                {
                    Message = "Mock capability executed: Found 3 Excel files (ComprehensiveStockAi.xlsx, DIOR 2025.xlsx, sample 1 2.4.25.xlsx)",
                    Type = AgentResponseType.Text,
                    Data = new Dictionary<string, object>
                    {
                        { "FilesFound", 3 },
                        { "Operation", "ListFiles" },
                        { "FileTypes", new[] { "xlsx" } }
                    }
                };
            };

            // Create AiAgent
            var aiAgent = new AiAgent(logger, configuration, testCapabilityHandler);
            
            Console.WriteLine("✅ AiAgent initialized successfully!");
            
            // Test capabilities
            var capabilities = await aiAgent.GetCapabilitiesAsync();
            Console.WriteLine($"📋 Available capabilities: {capabilities.Count}");
            foreach (var cap in capabilities)
            {
                Console.WriteLine($"   • {cap.Name}: {cap.Description}");
            }
            Console.WriteLine();

            // Interactive chat loop
            Console.WriteLine("🤖 AiAgent is ready! Type your messages (or 'quit' to exit):");
            Console.WriteLine("Try: 'Hello', 'List Excel files', 'What files are in the current folder?'");
            Console.WriteLine();

            string userId = "test-user";
            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "quit")
                    break;

                if (userInput.ToLower() == "clear")
                {
                    await aiAgent.ClearConversationAsync(userId);
                    Console.WriteLine("🧹 Conversation cleared!");
                    continue;
                }

                if (userInput.ToLower() == "history")
                {
                    var history = await aiAgent.GetConversationHistoryAsync(userId);
                    Console.WriteLine($"📚 Conversation history ({history.Count} messages):");
                    foreach (var msg in history)
                    {
                        Console.WriteLine($"   {msg.Role}: {msg.Content}");
                    }
                    continue;
                }

                Console.WriteLine("🤔 Processing...");
                
                var startTime = DateTime.Now;
                var response = await aiAgent.ProcessMessageAsync(userInput, userId);
                var endTime = DateTime.Now;

                Console.WriteLine($"🤖 Agent: {response.Message}");
                Console.WriteLine($"⏱️  Response time: {(endTime - startTime).TotalMilliseconds:F0}ms");
                
                if (response.Data.ContainsKey("ProcessedBy"))
                {
                    Console.WriteLine($"🔧 Processed by: {response.Data["ProcessedBy"]}");
                }
                
                if (response.Data.ContainsKey("UsedCapabilities") && (bool)response.Data["UsedCapabilities"])
                {
                    Console.WriteLine("🛠️  Used external capabilities!");
                }
                
                Console.WriteLine();
            }

            Console.WriteLine("👋 Goodbye!");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ Configuration Error: {ex.Message}");
            Console.WriteLine("💡 Make sure to set OPENAI_API_KEY environment variable or provide it when prompted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Test program error");
        }
    }

    private static string GetApiKeyFromUser()
    {
        Console.WriteLine("No OPENAI_API_KEY environment variable found.");
        Console.Write("Please enter your OpenAI API key (starts with sk-): ");
        var apiKey = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("sk-"))
        {
            Console.WriteLine("❌ Invalid API key provided.");
            return "invalid";
        }
        
        return apiKey;
    }
    
    /// <summary>
    /// Program entry point for testing AiAgent
    /// </summary>
    public static async Task Main(string[] args)
    {
        await RunInteractiveTest(args);
    }
}
