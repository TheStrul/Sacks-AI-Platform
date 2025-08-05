using SacksAIPlatform.Tests.Manual;

Console.WriteLine("Starting FileDataReader test with real files...\n");

try
{
    await FileDataReaderManualTest.RunTestAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Test failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to continue...");
Console.ReadKey();
