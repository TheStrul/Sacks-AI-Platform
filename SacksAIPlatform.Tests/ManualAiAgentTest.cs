using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;

namespace SacksAIPlatform.Tests;

/// <summary>
/// Simple manual test to verify AiAgent works with a real API key
/// Run this method to test the AiAgent interactively
/// </summary>
public static class ManualAiAgentTest
{
    /// <summary>
    /// Run this method to test the AiAgent manually
    /// Make sure you have OPENAI_API_KEY in your .env file
    /// </summary>
    public static async Task RunTest()
    {
        Console.WriteLine("=== Manual AiAgent Test ===");
        Console.WriteLine("Testing AiAgent with real OpenAI API");
        Console.WriteLine();

        // Load environment variables
        DotNetEnv.Env.Load();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<AiAgent>();

        // Setup configuration
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                     Environment.GetEnvironmentVariable("OpenAI__ApiKey");
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("❌ No OPENAI_API_KEY or OpenAI__ApiKey found in environment variables");
            Console.WriteLine("💡 Please add OPENAI_API_KEY=your_key to your .env file");
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = apiKey,
                ["OpenAI:Model"] = "gpt-4o"
            })
            .Build();

        try
        {
            // Create a simple capability handler for testing
            Func<string, string, Task<AgentResponse>> testCapabilityHandler = async (message, userId) =>
            {
                Console.WriteLine($"[CAPABILITY] Executing for: {message}");
                await Task.Delay(200); // Simulate work

                return new AgentResponse
                {
                    Message = "Mock capability result: Found 3 Excel files in the Inputs folder",
                    Type = AgentResponseType.Text,
                    Data = new Dictionary<string, object>
                    {
                        { "FilesFound", 3 },
                        { "Operation", "ListFiles" }
                    }
                };
            };

            // Create AiAgent
            var aiAgent = new AiAgent(logger, configuration, testCapabilityHandler);
            
            Console.WriteLine("✅ AiAgent created successfully!");
            
            // Test capabilities
            var capabilities = await aiAgent.GetCapabilitiesAsync();
            Console.WriteLine($"📋 Available capabilities: {capabilities.Count}");
            
            // Test simple conversation
            Console.WriteLine("\n🤖 Testing simple conversation...");
            var response1 = await aiAgent.ProcessMessageAsync("Hello! How are you?", "test-user");
            Console.WriteLine($"Response: {response1.Message}");
            Console.WriteLine($"Type: {response1.Type}");
            Console.WriteLine($"Processed by: {response1.Data.GetValueOrDefault("ProcessedBy", "Unknown")}");

            // Test potentially capability-triggering message
            Console.WriteLine("\n🛠️ Testing capability-triggering message...");
            var response2 = await aiAgent.ProcessMessageAsync("Can you list all the Excel files in the current directory?", "test-user");
            Console.WriteLine($"Response: {response2.Message}");
            Console.WriteLine($"Type: {response2.Type}");
            Console.WriteLine($"Processed by: {response2.Data.GetValueOrDefault("ProcessedBy", "Unknown")}");
            Console.WriteLine($"Used capabilities: {response2.Data.GetValueOrDefault("UsedCapabilities", false)}");

            // Test conversation history
            Console.WriteLine("\n📚 Testing conversation history...");
            var history = await aiAgent.GetConversationHistoryAsync("test-user");
            Console.WriteLine($"Conversation history has {history.Count} messages");

            Console.WriteLine("\n✅ All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Test error");
        }
    }
}
