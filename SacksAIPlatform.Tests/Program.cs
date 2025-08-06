using SacksAIPlatform.Tests;

/// <summary>
/// Simple entry point to run the AiAgent tests
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting AiAgent Test Program...");
        await AiAgentTestProgram.RunInteractiveTest(args);
    }
}
