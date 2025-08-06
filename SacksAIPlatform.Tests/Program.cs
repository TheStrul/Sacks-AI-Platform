using SacksAIPlatform.Tests;

/// <summary>
/// Simple entry point to run the AiAgent tests
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sacks AI Platform Test Suite ===");
        Console.WriteLine("Choose which test to run:");
        Console.WriteLine("1. AiAgent (Original with capabilities)");
        Console.WriteLine("2. AiAgentBase (Simple OpenAI Assistant wrapper with JSON config)");
        Console.WriteLine();
        Console.Write("Enter choice (1 or 2): ");
        
        var choice = Console.ReadLine();
        
        if (choice == "2")
        {
            await AiAgentBaseTest.RunTest();
        }
        else
        {
            Console.WriteLine("Starting AiAgent Test Program...");
            await AiAgentTestProgram.RunInteractiveTest(args);
        }
    }
}
