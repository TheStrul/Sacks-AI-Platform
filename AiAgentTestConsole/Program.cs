using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using DotNetEnv;

/// <summary>
/// Interactive test console for AiAgentBase
/// Run this to test the AiAgentBase with real OpenAI API calls
/// </summary>
class Program
{
    static async Task Main(string[] args)
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
        
        Console.WriteLine("=== AI Agent Base Test Console ===");
        Console.WriteLine("Choose test mode:");
        Console.WriteLine("1. Interactive Chat Test");
        Console.WriteLine("2. Quick Manual Test");
        Console.WriteLine();
        Console.Write("Enter choice (1 or 2): ");
        
        var choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                await RunInteractiveChatTest();
                break;
            case "2":
                await RunQuickManualTest();
                break;
            default:
                Console.WriteLine("Invalid choice. Running interactive test...");
                await RunInteractiveChatTest();
                break;
        }
    }

    private static async Task RunInteractiveChatTest()
    {
        Console.WriteLine("Starting Interactive Chat Test...");
        Console.WriteLine();

        var apiKey = await GetApiKeyAsync();
        if (apiKey == "invalid")
        {
            return;
        }

        // Initialize AiAgentBase
        AiAgentBase agent;
        try
        {
            agent = new AiAgentBase(apiKey);
            Console.WriteLine("✅ AiAgentBase created successfully!");
            
            await agent.InitializeAsync();
            Console.WriteLine("✅ AiAgentBase initialized successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize AiAgentBase: {ex.Message}");
            return;
        }

        // Show capabilities
        var capabilities = await agent.GetCapabilitiesAsync();
        Console.WriteLine($"📋 Available capabilities: {capabilities.Count}");
        foreach (var cap in capabilities)
        {
            Console.WriteLine($"   • {cap.Name}: {cap.Description}");
        }
        Console.WriteLine();

        // Interactive chat loop
        Console.WriteLine("🤖 AiAgentBase is ready! Type your messages (or 'quit' to exit):");
        Console.WriteLine("Try: 'Hello', 'What can you do?', 'Tell me about AI'");
        Console.WriteLine();

        string userId = "test-user-001";
        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "quit")
                break;

            if (userInput.ToLower() == "clear")
            {
                await agent.ClearConversationAsync(userId);
                Console.WriteLine("🧹 Conversation cleared!");
                continue;
            }

            if (userInput.ToLower() == "history")
            {
                var history = await agent.GetConversationHistoryAsync(userId);
                Console.WriteLine($"📚 Conversation history ({history.Count} messages):");
                foreach (var msg in history)
                {
                    Console.WriteLine($"   {msg.Role}: {msg.Content}");
                }
                continue;
            }

            try
            {
                Console.WriteLine("🤔 Thinking...");
                var response = await agent.ProcessMessageAsync(userInput, userId);
                Console.WriteLine($"🤖 Assistant: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("👋 Goodbye!");
    }

    private static async Task RunQuickManualTest()
    {
        Console.WriteLine("Starting Quick Manual Test...");

        var apiKey = await GetApiKeyAsync();
        if (apiKey == "invalid")
        {
            return;
        }

        try
        {
            // Initialize AiAgentBase
            var agent = new AiAgentBase(apiKey);
            Console.WriteLine("✅ AiAgentBase created");
            
            await agent.InitializeAsync();
            Console.WriteLine("✅ AiAgentBase initialized");

            // Test basic conversation
            Console.WriteLine("📝 Testing basic conversation...");
            var response = await agent.ProcessMessageAsync("Hello! Can you introduce yourself?");
            Console.WriteLine($"🤖 Response: {response.Message}");

            // Test capabilities
            Console.WriteLine("\n📋 Testing capabilities...");
            var capabilities = await agent.GetCapabilitiesAsync();
            Console.WriteLine($"Found {capabilities.Count} capabilities:");
            foreach (var cap in capabilities)
            {
                Console.WriteLine($"   • {cap.Name}: {cap.Description}");
            }

            // Test conversation history
            Console.WriteLine("\n📚 Testing conversation history...");
            var history = await agent.GetConversationHistoryAsync();
            Console.WriteLine($"Found {history.Count} messages in history");

            Console.WriteLine("\n✅ All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static async Task<string> GetApiKeyAsync()
    {
        // Try to get API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("✅ Found OpenAI API key in environment variables");
            return apiKey;
        }

        // Prompt user for API key
        Console.WriteLine("❌ OpenAI API key not found in environment variables");
        Console.WriteLine("Please set OPENAI_API_KEY in your .env file or environment variables");
        Console.Write("Or enter your API key now (or 'skip' to exit): ");
        
        var userInput = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "skip")
        {
            Console.WriteLine("❌ No API key provided. Exiting...");
            return "invalid";
        }

        return userInput;
    }
}
