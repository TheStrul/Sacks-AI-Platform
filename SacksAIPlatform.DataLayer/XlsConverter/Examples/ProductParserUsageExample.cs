using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.DataLayer.XlsConverter.Helpers;
using SacksAIPlatform.DataLayer.XlsConverter.Models;

namespace SacksAIPlatform.DataLayer.XlsConverter.Examples;

/// <summary>
/// Example usage of the Product Parser system
/// Demonstrates how to configure and use the dictionary helper and parser
/// </summary>
public static class ProductParserUsageExample
{
    /// <summary>
    /// Demonstrates basic usage of the parser system
    /// </summary>
    public static void BasicUsageExample()
    {
        Console.WriteLine("=== Product Parser Usage Example ===\n");

        // 1. Create a parser with default configuration
        var parser = ProductDescriptionParser.CreateDefault();

        // 2. Test with your example
        var testDescription = "ADP BLU MEDITERRANEO MIRTO DI PANAREA 30ML EDT SPRAY 29.6ml";
        Console.WriteLine($"Testing: {testDescription}");

        var result = parser.ParseDescription(testDescription);
        Console.WriteLine($"Parsed: {result.GetSummary()}");
        Console.WriteLine($"Matched rules: {string.Join(", ", result.MatchedRules)}\n");

        // 3. Create a product and update it with parsed information
        var product = new Product
        {
            Code = "TEST001",
            Name = "Test Product"
        };

        Console.WriteLine("Before parsing:");
        Console.WriteLine($"  Concentration: {product.Concentration}");
        Console.WriteLine($"  Type: {product.Type}");
        Console.WriteLine($"  Size: {product.Size}");
        Console.WriteLine($"  Units: {product.Units}");

        parser.ParseAndUpdateProduct(product, testDescription);

        Console.WriteLine("\nAfter parsing:");
        Console.WriteLine($"  Concentration: {product.Concentration}");
        Console.WriteLine($"  Type: {product.Type}");
        Console.WriteLine($"  Size: {product.Size}");
        Console.WriteLine($"  Units: {product.Units}");
    }

    /// <summary>
    /// Demonstrates runtime configuration management
    /// </summary>
    public static void RuntimeConfigurationExample()
    {
        Console.WriteLine("\n=== Runtime Configuration Example ===\n");

        // 1. Create configuration manager
        var configManager = new ProductParserConfigurationManager("example-parser-config.json");
        var runtimeManager = new ProductParserRuntimeManager(configManager);

        // 2. Add custom mappings at runtime
        Console.WriteLine("Adding custom mappings...");
        
        // Add a custom concentration mapping
        runtimeManager.AddConcentrationMapping("AQUA", Concentration.EDC);
        
        // Add a custom type mapping
        runtimeManager.AddTypeMapping("ATOMIZER", PerfumeType.Spray);
        
        // Add brand mappings
        runtimeManager.AddBrandMapping("CHANEL", 1);
        runtimeManager.AddBrandMapping("DIOR", 2);
        runtimeManager.AddBrandMapping("TOM FORD", 3);

        Console.WriteLine($"Configuration statistics: {runtimeManager.GetStatistics().TotalMappings} total mappings");

        // 3. Test with new mappings
        var testDescriptions = new[]
        {
            "CHANEL NO 5 100ML AQUA ATOMIZER",
            "DIOR SAUVAGE 50ML EDT SPRAY",
            "TOM FORD OUD WOOD 30ML EDP SPRAY"
        };

        foreach (var description in testDescriptions)
        {
            Console.WriteLine($"\nTesting: {description}");
            var result = runtimeManager.TestParsing(description);
            Console.WriteLine($"  Result: {result.GetSummary()}");
        }
    }

    /// <summary>
    /// Demonstrates adding custom parsing rules
    /// </summary>
    public static void CustomParsingRulesExample()
    {
        Console.WriteLine("\n=== Custom Parsing Rules Example ===\n");

        var configManager = new ProductParserConfigurationManager();
        var runtimeManager = new ProductParserRuntimeManager(configManager);

        // Add a custom rule to extract limited edition information
        runtimeManager.AddParsingRule(
            name: "ExtractLimitedEdition",
            pattern: @"\b(LIMITED|EDITION|LE)\b",
            propertyType: PropertyType.Remarks,
            priority: 5,
            extractGroups: new List<int> { 1 }
        );

        // Add a rule to extract tester information
        runtimeManager.AddParsingRule(
            name: "ExtractTester",
            pattern: @"\b(TESTER|TST)\b",
            propertyType: PropertyType.Remarks,
            priority: 6,
            extractGroups: new List<int> { 1 }
        );

        // Test with descriptions containing these patterns
        var testDescriptions = new[]
        {
            "CHANEL NO 5 LIMITED EDITION 100ML EDP SPRAY",
            "DIOR SAUVAGE TESTER 100ML EDT SPRAY",
            "TOM FORD NOIR LE 50ML EDP"
        };

        foreach (var description in testDescriptions)
        {
            Console.WriteLine($"Testing: {description}");
            var result = runtimeManager.TestParsing(description);
            Console.WriteLine($"  Matched rules: {string.Join(", ", result.MatchedRules)}");
        }
    }

    /// <summary>
    /// Demonstrates using the parser with the FileToProductConverter
    /// </summary>
    public static void FileConverterIntegrationExample()
    {
        Console.WriteLine("\n=== File Converter Integration Example ===\n");

        // 1. Create a converter with custom parser configuration
        var configManager = new ProductParserConfigurationManager();
        
        // Add some brand mappings for demonstration
        configManager.AddBrandMapping("ARMANI", 10);
        configManager.AddBrandMapping("VERSACE", 11);
        configManager.AddBrandMapping("BULGARI", 12);

        var converter = new FiletoProductConverter(configManager);

        Console.WriteLine("FileToProductConverter created with custom parser configuration");
        Console.WriteLine($"Parser has {converter.ConfigurationManager.GetBrandMappings().Count} brand mappings");

        // 2. You can access the runtime manager for further configuration
        var runtimeManager = new ProductParserRuntimeManager(converter.ConfigurationManager);
        
        // Add more mappings at runtime
        runtimeManager.AddConcentrationMapping("INTENSE", Concentration.Parfum);
        runtimeManager.AddTypeMapping("SPLASH", PerfumeType.Splash);

        // 3. Test parsing
        var testProduct = new Product { Code = "TEST001" };
        converter.Parser.ParseAndUpdateProduct(testProduct, "ARMANI CODE INTENSE 75ML EDT SPLASH");

        Console.WriteLine($"Parsed product - Brand ID: {testProduct.BrandID}, Concentration: {testProduct.Concentration}, Type: {testProduct.Type}");
    }

    /// <summary>
    /// Demonstrates learning from examples
    /// </summary>
    public static void LearningExample()
    {
        Console.WriteLine("\n=== Learning Example ===\n");

        var configManager = new ProductParserConfigurationManager();
        var runtimeManager = new ProductParserRuntimeManager(configManager);

        // Create learning examples where the parser might fail initially
        var learningExamples = new[]
        {
            new LearningExample
            {
                Description = "VERSACE POUR FEMME ELIXIR 100ML VAPORISATEUR",
                ExpectedConcentration = Concentration.Parfum,
                ExpectedType = PerfumeType.Spray
            },
            new LearningExample
            {
                Description = "ARMANI ACQUA DI GIO PROFUMO 125ML SPLASH",
                ExpectedConcentration = Concentration.EDP,
                ExpectedType = PerfumeType.Splash
            }
        };

        Console.WriteLine("Before learning:");
        foreach (var example in learningExamples)
        {
            var result = runtimeManager.TestParsing(example.Description);
            Console.WriteLine($"  {example.Description} -> {result.GetSummary()}");
        }

        // Learn from examples
        runtimeManager.LearnFromExamples(learningExamples);

        Console.WriteLine("\nAfter learning:");
        foreach (var example in learningExamples)
        {
            var result = runtimeManager.TestParsing(example.Description);
            Console.WriteLine($"  {example.Description} -> {result.GetSummary()}");
        }
    }

    /// <summary>
    /// Demonstrates testing and validation
    /// </summary>
    public static void TestingAndValidationExample()
    {
        Console.WriteLine("\n=== Testing and Validation Example ===\n");

        var configManager = new ProductParserConfigurationManager();
        var runtimeManager = new ProductParserRuntimeManager(configManager);

        // Test parsing with detailed results
        var testResult = runtimeManager.TestParsingWithComparison("CHANEL NO 5 100ML EDP SPRAY");
        Console.WriteLine($"Test description: {testResult.OriginalDescription}");
        Console.WriteLine($"Changes made: {testResult.GetSummary()}");
        Console.WriteLine($"Found matches: {testResult.FoundMatches}");

        // Validate configuration
        var validationErrors = runtimeManager.ValidateConfiguration();
        if (validationErrors.Count > 0)
        {
            Console.WriteLine("\nValidation errors:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
        else
        {
            Console.WriteLine("\nConfiguration is valid!");
        }

        // Show statistics
        var stats = runtimeManager.GetStatistics();
        Console.WriteLine($"\nConfiguration statistics:");
        Console.WriteLine($"  Total mappings: {stats.TotalMappings}");
        Console.WriteLine($"  Parsing rules: {stats.ParsingRules}");
        Console.WriteLine($"  Ignore patterns: {stats.IgnorePatterns}");
    }

    /// <summary>
    /// Runs all examples
    /// </summary>
    public static void RunAllExamples()
    {
        try
        {
            BasicUsageExample();
            RuntimeConfigurationExample();
            CustomParsingRulesExample();
            FileConverterIntegrationExample();
            LearningExample();
            TestingAndValidationExample();

            Console.WriteLine("\n=== All examples completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError running examples: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}