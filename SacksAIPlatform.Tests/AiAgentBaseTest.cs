using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using DotNetEnv;

namespace SacksAIPlatform.Tests;

/// <summary>
/// Test program for AiAgentBase with JSON configuration loading
/// </summary>
public class AiAgentBaseTest
{
    public static async Task RunTest()
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
        
        Console.WriteLine("=== AiAgentBase Test with JSON Configuration ===");
        Console.WriteLine();

        // Get API key
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? GetApiKeyFromUser();
        
        if (apiKey == "invalid")
        {
            Console.WriteLine("❌ Cannot proceed without a valid API key.");
            return;
        }

        try
        {
            // Create AiAgentBase
            var aiAgent = new AiAgentBase(apiKey);
            
            Console.WriteLine("🤖 Initializing AiAgentBase with JSON configuration...");
            
            // Initialize with default configuration (loads from assistant-config.json)
            await aiAgent.InitializeAsync();
            
            Console.WriteLine("✅ AiAgentBase initialized successfully!");
            Console.WriteLine();

            // Test capabilities
            var capabilities = await aiAgent.GetCapabilitiesAsync();
            Console.WriteLine($"📋 Available capabilities: {capabilities.Count}");
            foreach (var cap in capabilities)
            {
                Console.WriteLine($"   • {cap.Name}: {cap.Description}");
            }
            Console.WriteLine();

            // Interactive chat loop
            Console.WriteLine("🤖 AiAgentBase is ready! Type your messages (or 'quit' to exit):");
            Console.WriteLine("Try: 'Hello', 'What can you help me with?', 'Tell me about file operations'");
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
                Console.WriteLine();
            }

            Console.WriteLine("👋 Goodbye!");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ Configuration Error: {ex.Message}");
            Console.WriteLine("💡 Make sure to set OPENAI_API_KEY environment variable or provide it when prompted.");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"❌ Configuration File Error: {ex.Message}");
            Console.WriteLine("💡 Make sure the assistant-config.json file exists in the AI folder.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
}
