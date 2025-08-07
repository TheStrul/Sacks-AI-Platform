using SacksAIPlatform.DataLayer.XlsConverter.Examples;

namespace SacksAIPlatform.FileConverterTest;

/// <summary>
/// Test runner that combines the console app tests with the example usage
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// Runs the example usage scenarios in addition to the comprehensive tests
    /// </summary>
    public static void RunExampleUsageTests()
    {
        Console.WriteLine("\n?? Running Example Usage Tests from ProductParserUsageExample");
        Console.WriteLine("=" + new string('=', 70));

        try
        {
            // Run all the example scenarios
            ProductParserUsageExample.RunAllExamples();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Example usage tests failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Shows a summary of the test application capabilities
    /// </summary>
    public static void ShowTestCapabilities()
    {
        Console.WriteLine("\n?? FileToProductConverter Test Application Capabilities");
        Console.WriteLine("=" + new string('=', 60));
        Console.WriteLine();
        Console.WriteLine("? Basic Parser Configuration Testing");
        Console.WriteLine("   - Default configuration loading");
        Console.WriteLine("   - Your example: 'ADP BLU MEDITERRANEO MIRTO DI PANAREA 30ML EDT SPRAY 29.6ml'");
        Console.WriteLine("   - Individual component extraction verification");
        Console.WriteLine();
        Console.WriteLine("? Runtime Dictionary Management");
        Console.WriteLine("   - Brand name to ID mappings");
        Console.WriteLine("   - Concentration mappings (ADP, AQUA, INTENSE, etc.)");
        Console.WriteLine("   - Type mappings (ATOMIZER, VAPORISATEUR, etc.)");
        Console.WriteLine("   - Dynamic addition and testing");
        Console.WriteLine();
        Console.WriteLine("? Complex Description Parsing");
        Console.WriteLine("   - Real-world product descriptions");
        Console.WriteLine("   - Multiple property extraction from single string");
        Console.WriteLine("   - Gender, size, concentration, type detection");
        Console.WriteLine();
        Console.WriteLine("? File Conversion Integration");
        Console.WriteLine("   - CSV file processing with parser integration");
        Console.WriteLine("   - Product object creation and population");
        Console.WriteLine("   - Validation error reporting");
        Console.WriteLine();
        Console.WriteLine("? Custom Parsing Rules");
        Console.WriteLine("   - Regex-based rule creation");
        Console.WriteLine("   - Priority-based rule execution");
        Console.WriteLine("   - Special pattern detection (TESTER, LIMITED EDITION)");
        Console.WriteLine();
        Console.WriteLine("? Configuration Persistence");
        Console.WriteLine("   - JSON configuration save/load");
        Console.WriteLine("   - Configuration validation");
        Console.WriteLine("   - Backup and export functionality");
        Console.WriteLine();
        Console.WriteLine("?? All tests demonstrate the configurable dictionary helper and parser");
        Console.WriteLine("   as requested for the FileToProductConverter system.");
    }

    /// <summary>
    /// Shows next steps and recommendations
    /// </summary>
    public static void ShowNextSteps()
    {
        Console.WriteLine("\n?? Next Steps and Recommendations");
        Console.WriteLine("=" + new string('=', 50));
        Console.WriteLine();
        Console.WriteLine("1. ??? Load Brand Mappings:");
        Console.WriteLine("   - Connect to your brand database");
        Console.WriteLine("   - Use runtimeManager.AddBrandMappingsFromEntities(brands)");
        Console.WriteLine("   - This will enable brand name recognition in descriptions");
        Console.WriteLine();
        Console.WriteLine("2. ?? Customize Configuration:");
        Console.WriteLine("   - Review the generated configuration files");
        Console.WriteLine("   - Add industry-specific terms to dictionaries");
        Console.WriteLine("   - Create custom parsing rules for your data patterns");
        Console.WriteLine();
        Console.WriteLine("3. ?? Test with Real Data:");
        Console.WriteLine("   - Replace test-data.csv with your actual files");
        Console.WriteLine("   - Monitor parsing accuracy and adjust mappings");
        Console.WriteLine("   - Use the learning functionality to improve results");
        Console.WriteLine();
        Console.WriteLine("4. ?? Production Integration:");
        Console.WriteLine("   - Configure parser in your main application");
        Console.WriteLine("   - Set up configuration file paths");
        Console.WriteLine("   - Implement backup and monitoring strategies");
        Console.WriteLine();
        Console.WriteLine("?? See README.md for comprehensive documentation");
        Console.WriteLine("?? The system is ready for production use!");
    }
}