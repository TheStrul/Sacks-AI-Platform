namespace SacksAIPlatform.DataLayer.XlsConverter.Examples
{
    using SacksAIPlatform.DataLayer.XlsConverter;
    using SacksAIPlatform.DataLayer.XlsConverter.Helpers;

    /// <summary>
    /// Example demonstrating interactive FileToProductConverter functionality
    /// Shows how to use the interactive decision handler for uncertain parsing decisions
    /// </summary>
    public static class InteractiveFileConverterExample
    {
        /// <summary>
        /// Demonstrates interactive file conversion with user decision prompts
        /// </summary>
        public static async Task DemoInteractiveConversionAsync()
        {
            Console.WriteLine("=== Interactive FileToProductConverter Demo ===");
            Console.WriteLine("Dear Mr Strul, this demonstrates the interactive parsing capabilities.");
            Console.WriteLine();

            // Create parser configuration
            var configManager = new ProductParserConfigurationManager();
            
            // Add some brand mappings
            configManager.AddBrandMapping("CHANEL", 1);
            configManager.AddBrandMapping("DIOR", 2);
            configManager.AddBrandMapping("TOM FORD", 3);

            // Create interactive decision handler
            var interactiveHandler = new ConsoleInteractiveDecisionHandler(
                enableInteraction: true, 
                confidenceThreshold: 0.6  // Ask for help when confidence < 60%
            );

            // Create converter with interactive support
            var converter = new FiletoProductConverter(configManager);

            // Create file configuration
            var fileConfig = new FileConfiguration
            {
                TitleIndex = 0,
                StartFromRow = 1,
                EndAtRow = -1,
                HasInnerTitles = false,
                FormatName = "InteractiveDemo",
                ValidNumOfColumns = 4,
                ColumnMapping = new Dictionary<int, PropertyType>
                {
                    { 1, PropertyType.Code },
                    { 2, PropertyType.Name },
                    { 3, PropertyType.Brand },
                    { 4, PropertyType.Size }
                },
                DescriptionColumns = new() { 2 }, // Name column for description parsing
                IgnoredColumns = new()
            };

            // Create test data that will trigger interactive decisions
            await CreateInteractiveTestFile();

            Console.WriteLine("?? Processing file with interactive decision support...");
            Console.WriteLine("?? The parser will ask for your help when it's uncertain about parsing decisions.");
            Console.WriteLine();

            // Process file with interactive handler
            var result = await converter.ConvertFileToProductsInteractiveAsync(
                "interactive-test-data.csv", 
                fileConfig, 
                interactiveHandler
            );

            // Display results
            Console.WriteLine();
            Console.WriteLine("?? Interactive Processing Results");
            Console.WriteLine("=" + new string('=', 40));
            Console.WriteLine($"?? Lines processed: {result.TotalLinesProcessed}");
            Console.WriteLine($"? Valid products: {result.ValidProducts.Count}");
            Console.WriteLine($"? Validation errors: {result.ValidationErrors.Count}");
            Console.WriteLine($"?? Interactive decisions made: {result.InteractiveDecisions}");
            Console.WriteLine($"?? Learning examples captured: {result.LearnedExamples}");
            Console.WriteLine();

            if (result.ValidProducts.Count > 0)
            {
                Console.WriteLine("?? Processed Products:");
                foreach (var product in result.ValidProducts)
                {
                    Console.WriteLine($"   ?? {product.Code} - {product.Name}");
                    Console.WriteLine($"      Brand ID: {product.BrandID}, Concentration: {product.Concentration}");
                    Console.WriteLine($"      Type: {product.Type}, Size: {product.Size} {product.Units}");
                    Console.WriteLine();
                }
            }

            if (result.ValidationErrors.Count > 0)
            {
                Console.WriteLine("?? Validation Errors:");
                foreach (var error in result.ValidationErrors)
                {
                    Console.WriteLine($"   Row {error.RowNumber}: {error.ErrorMessage}");
                }
            }

            Console.WriteLine("? Interactive demo completed!");
        }

        /// <summary>
        /// Demonstrates non-interactive vs interactive comparison
        /// </summary>
        public static async Task DemoInteractiveVsNonInteractiveAsync()
        {
            Console.WriteLine("=== Interactive vs Non-Interactive Comparison ===");
            Console.WriteLine();

            var configManager = new ProductParserConfigurationManager();
            var converter = new FiletoProductConverter(configManager);
            
            var fileConfig = FileConfiguration.CreateDefaultConfiguration();
            fileConfig.ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 1, PropertyType.Code },
                { 2, PropertyType.Name },
                { 3, PropertyType.Brand },
                { 4, PropertyType.Size }
            };
            fileConfig.DescriptionColumns = new() { 2 };

            await CreateInteractiveTestFile();

            // Process without interaction
            Console.WriteLine("?? Processing WITHOUT interactive decisions...");
            var nonInteractiveResult = await converter.ConvertFileToProductsAsync(
                "interactive-test-data.csv", 
                fileConfig
            );

            Console.WriteLine($"   Valid products: {nonInteractiveResult.ValidProducts.Count}");
            Console.WriteLine($"   Validation errors: {nonInteractiveResult.ValidationErrors.Count}");
            Console.WriteLine();

            // Process with interaction
            Console.WriteLine("?? Processing WITH interactive decisions...");
            var interactiveHandler = new ConsoleInteractiveDecisionHandler(true, 0.6);
            var interactiveResult = await converter.ConvertFileToProductsInteractiveAsync(
                "interactive-test-data.csv", 
                fileConfig, 
                interactiveHandler
            );

            Console.WriteLine($"   Valid products: {interactiveResult.ValidProducts.Count}");
            Console.WriteLine($"   Validation errors: {interactiveResult.ValidationErrors.Count}");
            Console.WriteLine($"   Interactive decisions: {interactiveResult.InteractiveDecisions}");
            Console.WriteLine($"   Learning examples: {interactiveResult.LearnedExamples}");
            Console.WriteLine();

            Console.WriteLine("?? Comparison Summary:");
            Console.WriteLine($"   Improvement in valid products: {interactiveResult.ValidProducts.Count - nonInteractiveResult.ValidProducts.Count}");
            Console.WriteLine($"   Reduction in errors: {nonInteractiveResult.ValidationErrors.Count - interactiveResult.ValidationErrors.Count}");
            Console.WriteLine($"   User interactions required: {interactiveResult.InteractiveDecisions}");
        }

        /// <summary>
        /// Demonstrates configurable confidence thresholds
        /// </summary>
        public static async Task DemoConfidenceThresholdsAsync()
        {
            Console.WriteLine("=== Confidence Threshold Demo ===");
            Console.WriteLine("Shows how different confidence thresholds affect interaction frequency");
            Console.WriteLine();

            var configManager = new ProductParserConfigurationManager();
            var converter = new FiletoProductConverter(configManager);
            var fileConfig = FileConfiguration.CreateDefaultConfiguration();

            await CreateInteractiveTestFile();

            var thresholds = new[] { 0.3, 0.5, 0.7, 0.9 };

            foreach (var threshold in thresholds)
            {
                Console.WriteLine($"?? Testing with confidence threshold: {threshold:P0}");
                
                var handler = new ConsoleInteractiveDecisionHandler(true, threshold);
                var result = await converter.ConvertFileToProductsInteractiveAsync(
                    "interactive-test-data.csv", 
                    fileConfig, 
                    handler
                );

                Console.WriteLine($"   Interactive decisions: {result.InteractiveDecisions}");
                Console.WriteLine($"   Valid products: {result.ValidProducts.Count}");
                Console.WriteLine();
            }

            Console.WriteLine("?? Lower thresholds = more interactions but potentially better accuracy");
            Console.WriteLine("?? Higher thresholds = fewer interactions but may miss uncertain cases");
        }

        /// <summary>
        /// Creates test data that will trigger interactive decisions
        /// </summary>
        private static async Task CreateInteractiveTestFile()
        {
            var csvContent = @"Code,Name,Brand,Size
""COMPLEX001"",""UNKNOWN BRAND MYSTERIOUS ELIXIR 50ML SPRAY POUR HOMME"",""99"",""50ml""
""AMBIGUOUS002"",""RARE PARFUM INTENSE 100ML VAPORISATEUR FEMME TESTER"",""NEWBRAND"",""100""
""UNCLEAR003"",""EXOTIC AQUA FRESH 75ML COLOGNE SPLASH UNISEX"",""888"",""75 oz""
""CONFUSING004"",""VINTAGE EDT CLASSIC 30ML ATOMIZER MEN LIMITED"",""OLDHOUSE"",""30ml""
""SIMPLE005"",""CHANEL NO 5 100ML EDP SPRAY WOMEN"",""1"",""100ml""";

            await File.WriteAllTextAsync("interactive-test-data.csv", csvContent);
        }

        /// <summary>
        /// Runs all interactive examples
        /// </summary>
        public static async Task RunAllInteractiveExamplesAsync()
        {
            try
            {
                await DemoInteractiveConversionAsync();
                
                Console.WriteLine("\nPress any key to continue to comparison demo...");
                Console.ReadKey();
                Console.Clear();
                
                await DemoInteractiveVsNonInteractiveAsync();
                
                Console.WriteLine("\nPress any key to continue to confidence threshold demo...");
                Console.ReadKey();
                Console.Clear();
                
                await DemoConfidenceThresholdsAsync();

                Console.WriteLine("\n? All interactive examples completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error running interactive examples: {ex.Message}");
                throw;
            }
        }
    }
}