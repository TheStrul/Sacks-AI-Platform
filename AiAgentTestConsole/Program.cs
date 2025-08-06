using AiAgent;
using AiAgent.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using DotNetEnv;

/// <summary>
/// Interactive test console for LangChain AI Agent
/// Run this to test the LangChain AI Agent with real OpenAI API calls and tools
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Load environment variables from .env file
        try
        {
            // The .env file is copied to the executable directory by MSBuild
            var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var envPaths = new[]
            {
                ".env",                                              // Current working directory
                Path.Combine(executableDir ?? "", ".env"),          // Same directory as executable (where MSBuild copies it)
                Path.Combine("..", ".env")                          // Parent directory (workspace root)
            };

            string? loadedFromPath = null;
            foreach (var envPath in envPaths)
            {
                if (File.Exists(envPath))
                {
                    Env.Load(envPath);
                    loadedFromPath = Path.GetFullPath(envPath);
                    break;
                }
            }

            if (loadedFromPath != null)
            {
                Console.WriteLine($"✅ Environment variables loaded from .env file");
            }
            else
            {
                Console.WriteLine("⚠️  No .env file found");
                Console.WriteLine("Will try to use environment variables directly...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Could not load .env file: {ex.Message}");
            Console.WriteLine("Will try to use environment variables directly...");
        }

        Console.WriteLine("=== LangChain AI Agent Test (requires OpenAI API key) ===");
        await RunLangChainAgentTest();
    }

    private static async Task RunLangChainAgentTest()
    {
        Console.WriteLine("Starting LangChain AI Agent Test...");
        Console.WriteLine();

        var apiKey = GetApiKey();
        if (apiKey == "invalid")
        {
            return;
        }

        // Configure Serilog - File logging only to avoid interrupting console chat
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/ai-agent-.txt", rollingInterval: RollingInterval.Day,
                         outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Build services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<LangChainAiAgent>();

        // Create BasicConfig using the new static factory method
        var config = BasicConfig.CreateDefault(apiKey);
        
        // Optionally customize the config based on appsettings.json
        try
        {
            var appConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Override with appsettings values if they exist
            if (appConfig["Agent:Name"] != null)
                config.Agent.Name = appConfig["Agent:Name"]!;
            
            if (int.TryParse(appConfig["Agent:MaxConversationHistory"], out var maxHistory))
                config.Agent.MaxConversationHistory = maxHistory;

            if (bool.TryParse(appConfig["Agent:EnableFileSystem"], out var enableFs))
                config.BasicToolSettings.EnableFileSystem = enableFs;

            if (bool.TryParse(appConfig["Agent:EnableWebSearch"], out var enableWeb))
                config.BasicToolSettings.EnableWebSearch = enableWeb;

            if (bool.TryParse(appConfig["Agent:EnableCalculator"], out var enableCalc))
                config.BasicToolSettings.EnableCalculator = enableCalc;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Could not load appsettings.json: {ex.Message}");
            Console.WriteLine("Using default configuration...");
        }

        // Initialize LangChain AI Agent
        LangChainAiAgent agent;
        try
        {
            agent = new LangChainAiAgent(config, loggerFactory);
            Console.WriteLine("✅ LangChain AI Agent created successfully!");
            Console.WriteLine($"🤖 Agent Name: {config.Agent.Name}");
            Console.WriteLine("🔧 Agent configured with the following tools:");
            
            var availableTools = agent.GetAvailableTools();
            foreach (var (name, description) in availableTools)
            {
                Console.WriteLine($"   • {name}: {description}");
            }
            
            if (availableTools.Count == 0)
            {
                Console.WriteLine("   (No tools enabled)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize LangChain AI Agent: {ex.Message}");
            logger.LogError(ex, "Failed to initialize agent");
            return;
        }

        // Interactive chat loop
        Console.WriteLine();
        Console.WriteLine("🤖 LangChain AI Agent is ready! Type your messages (or 'quit' to exit):");
        Console.WriteLine("Try examples:");
        Console.WriteLine("   • 'List files in C:\\temp'");
        Console.WriteLine("   • 'Search for latest AI news'");
        Console.WriteLine("   • 'Calculate 15 * 23 + sqrt(144)'");
        Console.WriteLine("   • 'Create a file called test.txt with hello world'");
        Console.WriteLine();

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "quit")
                break;

            try
            {
                Console.WriteLine("🤔 Processing...");
                var response = await agent.ProcessMessageAsync(userInput);
                Console.WriteLine($"🤖 Assistant: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                logger.LogError(ex, "Error processing message: {Message}", userInput);
            }

            Console.WriteLine();
        }

        Console.WriteLine("👋 Goodbye!");
        Log.CloseAndFlush();
    }

    private static string GetApiKey()
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
