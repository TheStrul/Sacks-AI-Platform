using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.DataLayer.XlsConverter;
using SacksAIPlatform.DataLayer.XlsConverter.Helpers;
using SacksAIPlatform.DataLayer.XlsConverter.Models;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Seeds;
using Serilog;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace SacksAIPlatform.FileConverterTest;

/// <summary>
/// Test console application for FileToProductConverter with ProductDescriptionParser
/// Demonstrates configuration, parsing, and runtime dictionary management
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== FileToProductConverter Test Console ===");
        Console.WriteLine("Dear Mr Strul, this application will test the FileToProductConverter with the new parser system.\n");

        // Configure Serilog - Redirect to debug window to avoid interfering with console interaction
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/fileconverter-test-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            // Build host with services
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting FileToProductConverter Test Application");

            // Show main menu
            await ShowMainMenu(host.Services, logger);

            logger.LogInformation("All tests completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Application failed: {ex.Message}");
            Log.Logger.Error(ex, "Application failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.AddSingleton<IConfiguration>(context.Configuration);

                // Register database context (using SQL Server)
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? "Server=(localdb)\\mssqllocaldb;Database=SacksAIPlatform;Trusted_Connection=true;MultipleActiveResultSets=true";

                services.AddDbContext<SacksDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // Register parser services
                services.AddSingleton<ProductParserConfigurationManager>();
                services.AddSingleton<ProductParserRuntimeManager>();
                services.AddTransient<FiletoProductConverter>();

                // Configure logging based on appsettings.json
                var redirectToDebug = context.Configuration.GetValue<bool>("Logging:RedirectToDebug", true);
                var enableConsoleLogging = context.Configuration.GetValue<bool>("Logging:EnableConsoleLogging", false);

                services.AddLogging(logging =>
                {
                    logging.ClearProviders();

                    if (redirectToDebug)
                    {
                        logging.AddDebug();
                    }

                    if (enableConsoleLogging)
                    {
                        logging.AddConsole();
                    }

                    logging.SetMinimumLevel(LogLevel.Information);
                });
            });

    static async Task ShowMainMenu(IServiceProvider services, ILogger<Program> logger)
    {
        bool exit = false;
        while (!exit)
        {
            Console.Clear();
            Console.WriteLine("?? FileToProductConverter Test & Database Integration");
            Console.WriteLine("=" + new string('=', 60));
            Console.WriteLine();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Run Standard Tests (No Database)");
            Console.WriteLine("2. ??? Database Integration - Parse File with Real Data");
            Console.WriteLine("3. ?? Interactive Parsing - Parse with User Decisions");
            Console.WriteLine("4. ?? View Database Statistics");
            Console.WriteLine("5. ?? Database Management Options");
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice (0-5): ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunAllTests(services, logger);
                        break;
                    case "2":
                        await RunDatabaseIntegrationTest(services, logger);
                        break;
                    case "3":
                        await RunInteractiveParsingTest(services, logger);
                        break;
                    case "4":
                        await ShowDatabaseStatistics(services, logger);
                        break;
                    case "5":
                        await ShowDatabaseManagementMenu(services, logger);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("? Invalid choice. Please try again.");
                        break;
                }

                if (!exit && choice != "0")
                {
                    Console.WriteLine("\nPress any key to return to main menu...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
                logger.LogError(ex, "Error in menu option {Choice}", choice);
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    static ProductParserRuntimeManager runtimeManager = null;
    /// <summary>
    /// Main database integration test - connects to real DB and allows file parsing
    /// </summary>
    static async Task RunDatabaseIntegrationTest(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("??? Database Integration Test");
        Console.WriteLine("=" + new string('=', 50));
        Console.WriteLine();

        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();
            var configManager = services.GetRequiredService<ProductParserConfigurationManager>();

            // Step 1: Connect to database and retrieve data
            Console.WriteLine("?? Connecting to database...");

            // Ensure database exists
            await dbContext.Database.EnsureCreatedAsync();

            // Load all data from database
            var products = await dbContext.Products.Include(p => p.Brand).ToListAsync();
            var brands = await dbContext.Brands.Include(b => b.Manufacturer).ToListAsync();
            var fileConfigurations = await dbContext.FileConfigurationHolders
                .Include(f => f.Supplier)
                .ToListAsync();

            Console.WriteLine($"? Database connected successfully!");
            Console.WriteLine($"   ?? Products found: {products.Count}");
            Console.WriteLine($"   ??? Brands found: {brands.Count}");
            Console.WriteLine($"   ?? File configurations found: {fileConfigurations.Count}");

            // Check if database is empty and offer to seed it
            if (brands.Count == 0)
            {
                Console.WriteLine("\n?? Database appears to be empty (no brands found).");
                Console.Write("Would you like to seed the database with comprehensive perfume brand data? (y/N): ");
                var seedChoice = Console.ReadLine()?.ToLowerInvariant();

                if (seedChoice == "y" || seedChoice == "yes")
                {
                    Console.WriteLine("\n?? Seeding database...");
                    await DatabaseSeeder.SeedAsync(dbContext);

                    // Reload data after seeding
                    brands = await dbContext.Brands.Include(b => b.Manufacturer).ToListAsync();
                    Console.WriteLine($"? Database seeded! Now have {brands.Count} brands available.");
                }
                else
                {
                    Console.WriteLine("?? Continuing with empty database. Brand mapping will be limited.");
                }
            }

            // Step 2: Update parser with database brand mappings
            Console.WriteLine("\n?? Configuring parser with database brand mappings...");
            if (runtimeManager == null)
            {
                runtimeManager = new ProductParserRuntimeManager(configManager);
            }
                

            foreach (var brand in brands)
            {
                if (!string.IsNullOrEmpty(brand.Name))
                {
                    runtimeManager.AddBrandMapping(brand.Name.ToUpperInvariant(), brand.BrandID);
                }
            }

            // Also add product name to brand mappings
            foreach (var product in products.Where(p => !string.IsNullOrEmpty(p.Name)))
            {
                runtimeManager.AddProductToBrandMapping(product.Name.ToUpperInvariant(), product.BrandID);
            }

            Console.WriteLine($"? Parser configured with {brands.Count} brand mappings and {products.Count} product mappings");

            // Step 3: Ask user to select a file
            Console.WriteLine("\n?? File Selection");
            Console.WriteLine("-" + new string('-', 20));
            var selectedFile = await SelectFileToProcess();
            if (string.IsNullOrEmpty(selectedFile))
            {
                Console.WriteLine("? No file selected. Returning to menu.");
                return;
            }

            // Step 4: Ask user to select configuration
            Console.WriteLine("\n?? Configuration Selection");
            Console.WriteLine("-" + new string('-', 30));
            var selectedConfig = await SelectFileConfiguration(fileConfigurations);
            if (selectedConfig == null)
            {
                Console.WriteLine("? No configuration selected. Using default configuration.");
                selectedConfig = FileConfiguration.CreateDefaultConfiguration();
            }

            // Step 5: Process the file with FileToProductConverter
            Console.WriteLine("\n?? Processing File");
            Console.WriteLine("-" + new string('-', 20));
            Console.WriteLine($"?? File: {selectedFile}");
            Console.WriteLine($"?? Configuration: {selectedConfig.FormatName}");
            Console.WriteLine();

            var converter = new FiletoProductConverter(configManager, selectedConfig);
            var result = await converter.ConvertFileToProductsAsync(selectedFile, selectedConfig);

            // Step 6: Display results
            Console.WriteLine("?? Processing Results");
            Console.WriteLine("-" + new string('-', 25));
            Console.WriteLine($"?? Lines processed: {result.TotalLinesProcessed}");
            Console.WriteLine($"?? Empty lines: {result.EmptyLines}");
            Console.WriteLine($"? Valid products: {result.ValidProducts.Count}");
            Console.WriteLine($"? Validation errors: {result.ValidationErrors.Count}");

            Collection<Product> newProducts = new Collection<Product>();
            if (result.ValidProducts.Count > 0)
            {
                foreach (var product in result.ValidProducts)
                {
                    // Validate product is new before adding to database

                    if (!await dbContext.Products.AnyAsync(p => p.Code == product.Code))
                    {
                        newProducts.Add(product);
                    }
                }
                Console.WriteLine($"? New products: {newProducts.Count}");
                if (newProducts.Count > 0)
                {
                    Console.WriteLine("\n?? Sample Processed Products (first 5):");
                    foreach (var product in result.ValidProducts.Take(5))
                    {
                        Console.WriteLine($"   ?? {product.Code} - {product.Name}");
                        Console.WriteLine($"      Brand ID: {product.BrandID}, Concentration: {product.Concentration}");
                        Console.WriteLine($"      Type: {product.Type}, Size: {product.Size} {product.Units}");
                        Console.WriteLine();
                    }
                }
            }

            if (result.ValidationErrors.Count > 0)
            {
                Console.WriteLine("\n?? Validation Errors (first 5):");
                foreach (var error in result.ValidationErrors.Take(5))
                {
                    Console.WriteLine($"   Row {error.RowNumber}: {error.ErrorMessage}");
                }
            }

            // Step 7: Ask if user wants to save results to database
            if (newProducts.Count > 0)
            {
                Console.WriteLine("\n?? Save to Database?");
                Console.WriteLine("-" + new string('-', 25));
                Console.Write("Do you want to save the new parsed products to the database? (y/N): ");
                var saveChoice = Console.ReadLine()?.ToLowerInvariant();

                if (saveChoice == "y" || saveChoice == "yes")
                {
                    await SaveProductsToDatabase(dbContext, newProducts.ToList(), logger);
                }
            }
            else
            {
                Console.WriteLine("\n? No new products to save.");
            }
            logger.LogInformation("Database integration test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Database integration test failed: {ex.Message}");
            logger.LogError(ex, "Database integration test failed");
            throw;
        }
    }

    static async Task<string?> SelectFileToProcess()
    {
        Console.WriteLine("Please select a file to process:");
        Console.WriteLine("1. Browse for file");
        Console.WriteLine("2. Use test file (test-data.csv)");
        Console.Write("Choice (1-2): ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                Console.WriteLine("?? Please navigate and select your file...");
                Console.Write("Enter full file path: ");
                return Console.ReadLine();

            case "2":
                // Create test file if it doesn't exist
                if (!File.Exists("test-data.csv"))
                {
                    await CreateTestCsvFile();
                }
                return "test-data.csv";

            default:
                Console.WriteLine("? Invalid choice.");
                return null;
        }
    }

    static async Task<FileConfiguration?> SelectFileConfiguration(List<FileConfigurationHolder> configurations)
    {
        if (configurations.Count == 0)
        {
            Console.WriteLine("?? No file configurations found in database.");
            Console.WriteLine("Using default configuration.");
            return FileConfiguration.CreateDefaultConfiguration();
        }

        Console.WriteLine("Available file configurations:");
        Console.WriteLine("0. Use default configuration");

        for (int i = 0; i < configurations.Count; i++)
        {
            var config = configurations[i];
            Console.WriteLine($"{i + 1}. {config.Name} (Supplier: {config.Supplier.Name}, Pattern: {config.FileNamePattern})");
        }

        Console.Write($"Select configuration (0-{configurations.Count}): ");
        var choice = Console.ReadLine();

        if (int.TryParse(choice, out var index))
        {
            if (index == 0)
            {
                return FileConfiguration.CreateDefaultConfiguration();
            }
            else if (index > 0 && index <= configurations.Count)
            {
                var selectedHolder = configurations[index - 1];
                try
                {
                    var fileConfig = JsonSerializer.Deserialize<FileConfiguration>(selectedHolder.ConfigurationJson);
                    Console.WriteLine($"? Selected: {selectedHolder.Name}");
                    return fileConfig;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error deserializing configuration: {ex.Message}");
                    return null;
                }
            }
        }

        Console.WriteLine("? Invalid selection.");
        return null;
    }

    static async Task SaveProductsToDatabase(SacksDbContext dbContext, List<Product> products, ILogger<Program> logger)
    {
        try
        {
            Console.WriteLine("?? Saving products to database...");

            int added = 0;
            int updated = 0;
            int skipped = 0;

            foreach (var product in products)
            {
                var existing = await dbContext.Products.FirstOrDefaultAsync(p => p.Code == product.Code);
                if (existing == null)
                {
                    dbContext.Products.Add(product);
                    added++;
                }
                else
                {
                    // Update existing product
                    existing.Name = product.Name;
                    existing.BrandID = product.BrandID;
                    existing.Concentration = product.Concentration;
                    existing.Type = product.Type;
                    existing.Gender = product.Gender;
                    existing.Size = product.Size;
                    existing.Units = product.Units;
                    existing.LilFree = product.LilFree;
                    existing.CountryOfOrigin = product.CountryOfOrigin;
                    existing.OriginalSource = product.OriginalSource;
                    updated++;
                }
            }

            var saved = await dbContext.SaveChangesAsync();

            Console.WriteLine($"? Database save completed:");
            Console.WriteLine($"   ? Added: {added} products");
            Console.WriteLine($"   ?? Updated: {updated} products");
            Console.WriteLine($"   ?? Total saved: {saved} changes");

            logger.LogInformation("Saved {Added} new and updated {Updated} products to database", added, updated);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error saving to database: {ex.Message}");
            logger.LogError(ex, "Error saving products to database");
            throw;
        }
    }

    static async Task ShowDatabaseStatistics(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Database Statistics");
        Console.WriteLine("=" + new string('=', 30));
        Console.WriteLine();

        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();

            // Ensure database exists
            await dbContext.Database.EnsureCreatedAsync();

            var productCount = await dbContext.Products.CountAsync();
            var brandCount = await dbContext.Brands.CountAsync();
            var manufacturerCount = await dbContext.Manufacturers.CountAsync();
            var supplierCount = await dbContext.Suppliers.CountAsync();
            var configCount = await dbContext.FileConfigurationHolders.CountAsync();

            Console.WriteLine($"?? Products: {productCount:N0}");
            Console.WriteLine($"??? Brands: {brandCount:N0}");
            Console.WriteLine($"?? Manufacturers: {manufacturerCount:N0}");
            Console.WriteLine($"?? Suppliers: {supplierCount:N0}");
            Console.WriteLine($"?? File Configurations: {configCount:N0}");

            if (brandCount > 0)
            {
                Console.WriteLine("\n??? Top 10 Brands by Product Count:");
                var topBrands = await dbContext.Products
                    .Include(p => p.Brand)
                    .Where(p => p.Brand != null)
                    .GroupBy(p => p.Brand!.Name)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                foreach (var brand in topBrands)
                {
                    Console.WriteLine($"   {brand.Brand}: {brand.Count:N0} products");
                }
            }

            if (configCount > 0)
            {
                Console.WriteLine("\n?? File Configurations:");
                var configs = await dbContext.FileConfigurationHolders
                    .Include(f => f.Supplier)
                    .ToListAsync();

                foreach (var config in configs.Take(10))
                {
                    Console.WriteLine($"   {config.Name} (Supplier: {config.Supplier.Name})");
                    Console.WriteLine($"     Pattern: {config.FileNamePattern}, Extension: {config.FileExtension}");
                }
            }

            logger.LogInformation("Database statistics displayed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error retrieving database statistics: {ex.Message}");
            logger.LogError(ex, "Error retrieving database statistics");
            throw;
        }
    }

    static async Task ShowDatabaseManagementMenu(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Database Management");
        Console.WriteLine("=" + new string('=', 30));
        Console.WriteLine();
        Console.WriteLine("1. Initialize/Seed Database");
        Console.WriteLine("2. Clear All Data");
        Console.WriteLine("3. Add Sample File Configuration");
        Console.WriteLine("4. ?? Test Database Connection Only");
        Console.WriteLine("5. ?? Create New File Configuration (Interactive)");
        Console.WriteLine("0. Return to Main Menu");
        Console.WriteLine();
        Console.Write("Choose option (0-5): ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await InitializeDatabase(services, logger);
                break;
            case "2":
                await ClearDatabase(services, logger);
                break;
            case "3":
                await AddSampleFileConfiguration(services, logger);
                break;
            case "4":
                await TestDatabaseConnectionOnly(services, logger);
                break;
            case "5":
                await CreateNewFileConfigurationInteractive(services, logger);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("? Invalid choice.");
                break;
        }
    }

    static async Task TestDatabaseConnectionOnly(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Testing Database Connection Only");
        Console.WriteLine("=" + new string('=', 40));
        Console.WriteLine();

        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();

            Console.WriteLine("?? Attempting to connect to database...");

            // Test basic connection
            var canConnect = await dbContext.Database.CanConnectAsync();
            Console.WriteLine($"? Can connect to database: {canConnect}");

            if (!canConnect)
            {
                Console.WriteLine("? Cannot connect to database. Check connection string.");
                return;
            }

            Console.WriteLine("?? Ensuring database is created...");
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("? Database creation/verification completed");

            Console.WriteLine("?? Checking table creation...");

            // Test querying each table to verify they exist
            var productCount = await dbContext.Products.CountAsync();
            var brandCount = await dbContext.Brands.CountAsync();
            var manufacturerCount = await dbContext.Manufacturers.CountAsync();
            var supplierCount = await dbContext.Suppliers.CountAsync();
            var configCount = await dbContext.FileConfigurationHolders.CountAsync();

            Console.WriteLine("? All tables accessible:");
            Console.WriteLine($"   ?? Products table: {productCount} records");
            Console.WriteLine($"   ??? Brands table: {brandCount} records");
            Console.WriteLine($"   ?? Manufacturers table: {manufacturerCount} records");
            Console.WriteLine($"   ?? Suppliers table: {supplierCount} records");
            Console.WriteLine($"   ?? FileConfigurationHolders table: {configCount} records");

            // Test FileConfigurationHolder specifically (where the LONGTEXT issue was)
            Console.WriteLine("\n?? Testing FileConfigurationHolder table specifically...");

            var testConfig = new FileConfigurationHolder
            {
                Name = "Connection Test Config",
                SupplierId = 1, // This will fail if no supplier exists, but that's ok for connection test
                FileNamePattern = "*.test",
                FileExtension = ".test",
                ConfigurationJson = JsonSerializer.Serialize(new { test = "configuration" }),
                Remarks = "Test configuration for database connection verification"
            };

            // Try to add and then remove test configuration
            try
            {
                // Create a supplier first if none exists
                if (supplierCount == 0)
                {
                    var testSupplier = new Supplier
                    {
                        Name = "Test Supplier",
                        Type = "Test",
                        Country = "Test",
                        ContactInfo = "test@test.com"
                    };
                    dbContext.Suppliers.Add(testSupplier);
                    await dbContext.SaveChangesAsync();
                    testConfig.SupplierId = testSupplier.SupplierID;
                    Console.WriteLine("? Created test supplier");
                }
                else
                {
                    var firstSupplier = await dbContext.Suppliers.FirstAsync();
                    testConfig.SupplierId = firstSupplier.SupplierID;
                }

                dbContext.FileConfigurationHolders.Add(testConfig);
                await dbContext.SaveChangesAsync();
                Console.WriteLine("? Successfully added test FileConfigurationHolder");

                // Clean up test record
                dbContext.FileConfigurationHolders.Remove(testConfig);
                await dbContext.SaveChangesAsync();
                Console.WriteLine("? Successfully removed test FileConfigurationHolder");

                Console.WriteLine("\n?? Database connection test PASSED!");
                Console.WriteLine("   The LONGTEXT ? NVARCHAR(MAX) fix is working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error testing FileConfigurationHolder: {ex.Message}");
                logger.LogError(ex, "FileConfigurationHolder test failed");
            }

            logger.LogInformation("Database connection test completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Database connection test failed: {ex.Message}");
            logger.LogError(ex, "Database connection test failed");
            Console.WriteLine("\nTroubleshooting tips:");
            Console.WriteLine("1. Check if SQL Server LocalDB is installed");
            Console.WriteLine("2. Check the connection string in appsettings.json");
            Console.WriteLine("3. Try running: sqllocaldb info");
            Console.WriteLine("4. Try running: sqllocaldb start mssqllocaldb");
        }
    }

    static async Task InitializeDatabase(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Initializing Database...");
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();

            // Ensure database is created
            await dbContext.Database.EnsureCreatedAsync();

            // Use DatabaseSeeder to populate with comprehensive brand data
            Console.WriteLine("?? Checking if database seeding is needed...");

            // Check if database is empty (DatabaseSeeder already checks this internally)
            var manufacturerCount = await dbContext.Manufacturers.CountAsync();

            if (manufacturerCount == 0)
            {
                Console.WriteLine("?? Database is empty. Seeding with comprehensive perfume brand data...");
                Console.WriteLine("?? Loading data from embedded JSON resource (perfume-brands-data.json)...");

                await DatabaseSeeder.SeedAsync(dbContext);

                // Show seeding results
                var newManufacturerCount = await dbContext.Manufacturers.CountAsync();
                var newBrandCount = await dbContext.Brands.CountAsync();

                Console.WriteLine("? Database seeding completed successfully!");
                Console.WriteLine($"   ?? Manufacturers added: {newManufacturerCount}");
                Console.WriteLine($"   ??? Brands added: {newBrandCount}");

                // Show some examples of seeded data
                var sampleBrands = await dbContext.Brands
                    .Include(b => b.Manufacturer)
                    .Take(10)
                    .ToListAsync();

                Console.WriteLine("\n?? Sample seeded brands:");
                foreach (var brand in sampleBrands)
                {
                    Console.WriteLine($"   • {brand.Name} ({brand.CountryOfOrigin}) - {brand.Manufacturer.Name}");
                }

                if (newBrandCount > 10)
                {
                    Console.WriteLine($"   ... and {newBrandCount - 10} more brands");
                }
            }
            else
            {
                Console.WriteLine($"?? Database already contains {manufacturerCount} manufacturers");
                Console.WriteLine("   Skipping seeding (use Clear All Data first if you want to re-seed)");
            }

            logger.LogInformation("Database initialization completed with DatabaseSeeder");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error initializing database: {ex.Message}");
            logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    static async Task ClearDatabase(IServiceProvider services, ILogger<Program> logger)
    {
        Console.Write("?? Are you sure you want to clear ALL database data? (yes/no): ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToLowerInvariant() != "yes")
        {
            Console.WriteLine("? Operation cancelled.");
            return;
        }

        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();

            // Clear all tables
            dbContext.Products.RemoveRange(dbContext.Products);
            dbContext.FileConfigurationHolders.RemoveRange(dbContext.FileConfigurationHolders);
            dbContext.Brands.RemoveRange(dbContext.Brands);
            dbContext.Manufacturers.RemoveRange(dbContext.Manufacturers);
            dbContext.Suppliers.RemoveRange(dbContext.Suppliers);

            await dbContext.SaveChangesAsync();
            Console.WriteLine("? Database cleared successfully");

            logger.LogInformation("Database cleared successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error clearing database: {ex.Message}");
            logger.LogError(ex, "Error clearing database");
            throw;
        }
    }

    static async Task AddSampleFileConfiguration(IServiceProvider services, ILogger<Program> logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();

            // Ensure we have a supplier first
            var supplier = await dbContext.Suppliers.FirstOrDefaultAsync();
            if (supplier == null)
            {
                supplier = new Supplier
                {
                    Name = "Sample Supplier",
                    Type = "Distributor",
                    Country = "USA",
                    ContactInfo = "sample@example.com"
                };
                dbContext.Suppliers.Add(supplier);
                await dbContext.SaveChangesAsync();
            }

            // Create sample file configuration
            var defaultConfig = FileConfiguration.CreateDefaultConfiguration();
            var configJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });

            var fileConfig = new FileConfigurationHolder
            {
                Name = "Default CSV Configuration",
                SupplierId = supplier.SupplierID,
                FileNamePattern = "*.csv",
                FileExtension = ".csv",
                ConfigurationJson = configJson,
                Remarks = "Standard CSV configuration for product imports"
            };

            dbContext.FileConfigurationHolders.Add(fileConfig);
            await dbContext.SaveChangesAsync();

            Console.WriteLine("? Sample file configuration added successfully");
            logger.LogInformation("Sample file configuration added");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error adding sample configuration: {ex.Message}");
            logger.LogError(ex, "Error adding sample file configuration");
            throw;
        }
    }

    static async Task CreateTestCsvFile()
    {
        var csvContent = @"Code,Name,Brand,Size
P001,""ADP BLU MEDITERRANEO MIRTO DI PANAREA 30ML EDT SPRAY 29.6ml"",5,""30ml""
P002,""CHANEL NO 5 100ML EDP SPRAY FOR WOMEN"",1,""100ml""
P003,""DIOR SAUVAGE INTENSE 50ML EDT MASCULINE SPRAY"",2,""50ml""
P004,""TOM FORD OUD WOOD PURE 75ML PARFUM UNISEX"",3,""75ml""
P005,""VERSACE BRIGHT CRYSTAL TESTER 90ML EDT FEMME"",4,""90ml""
P006,""ARMANI CODE BLACK LIMITED EDITION 125ML EDP MEN"",6,""125ml""
P007,""HUGO BOSS BOTTLED NIGHT 200ML EDT SPLASH HOMME"",7,""200ml""
P008,""BULGARI OMNIA CRYSTALLINE 65ML EDT WOMEN ATOMIZER"",8,""65ml""
P009,""ISSEY MIYAKE L'EAU D'ISSEY 40ML EDF POUR FEMME VAPORISATEUR"",9,""40ml""
P010,""CREED AVENTUS COLLECTOR 120ML PARFUM SPRAY UNISEX"",10,""120ml""";

        await File.WriteAllTextAsync("test-data.csv", csvContent);
    }

    /// <summary>
    /// Interactive parsing test - demonstrates user-guided parsing decisions
    /// </summary>
    static async Task RunInteractiveParsingTest(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Interactive Parsing Test");
        Console.WriteLine("=" + new string('=', 40));
        Console.WriteLine();
        Console.WriteLine("This test demonstrates how the parser can ask for your help when");
        Console.WriteLine("it encounters uncertain parsing decisions.");
        Console.WriteLine();

        try
        {
            using var scope = services.CreateScope();
            var configManager = services.GetRequiredService<ProductParserConfigurationManager>();

            // Add some basic brand mappings for demonstration
            if (runtimeManager == null)
            {
                runtimeManager = new ProductParserRuntimeManager(configManager);
            }

            Console.WriteLine("?? Setting up interactive parsing test...");
            
            // Add some known brands for comparison
            runtimeManager.AddBrandMapping("CHANEL", 1);
            runtimeManager.AddBrandMapping("DIOR", 2);
            runtimeManager.AddBrandMapping("TOM FORD", 3);
            runtimeManager.AddBrandMapping("VERSACE", 4);

            // Step 1: Configure confidence threshold
            Console.WriteLine("\n?? Configuration");
            Console.WriteLine("-" + new string('-', 20));
            Console.WriteLine("The parser will ask for help when confidence is below a threshold.");
            Console.WriteLine("Lower threshold = more questions, higher accuracy");
            Console.WriteLine("Higher threshold = fewer questions, more automated");
            Console.WriteLine();

            double confidenceThreshold = 0.6; // Default
            Console.Write("Enter confidence threshold (0.1-0.9, default 0.6): ");
            var thresholdInput = Console.ReadLine();
            if (double.TryParse(thresholdInput, out var threshold) && threshold >= 0.1 && threshold <= 0.9)
            {
                confidenceThreshold = threshold;
            }
            Console.WriteLine($"? Using confidence threshold: {confidenceThreshold:P0}");

            // Step 2: Create interactive handler
            var interactiveHandler = new ConsoleInteractiveDecisionHandler(
                enableInteraction: true, 
                confidenceThreshold: confidenceThreshold
            );

            // Step 3: Create test data with ambiguous entries
            Console.WriteLine("\n?? Creating test data with ambiguous parsing challenges...");
            await CreateInteractiveTestCsvFile();

            // Step 4: Configure file converter
            var converter = new FiletoProductConverter(configManager);
            var fileConfig = new FileConfiguration
            {
                TitleIndex = 0,
                StartFromRow = 1,
                EndAtRow = -1,
                HasInnerTitles = false,
                FormatName = "InteractiveTest",
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

            Console.WriteLine("? Test configuration ready");

            // Step 5: Process with interactive mode
            Console.WriteLine("\n?? Processing file with interactive decisions...");
            Console.WriteLine("?? I'll ask for your help when I'm uncertain about parsing decisions.");
            Console.WriteLine();

            var result = await converter.ConvertFileToProductsInteractiveAsync(
                "interactive-test.csv", 
                fileConfig, 
                interactiveHandler
            );

            // Step 6: Display results
            Console.WriteLine("\n?? Interactive Processing Results");
            Console.WriteLine("=" + new string('=', 40));
            Console.WriteLine($"?? Lines processed: {result.TotalLinesProcessed}");
            Console.WriteLine($"?? Empty lines: {result.EmptyLines}");
            Console.WriteLine($"? Valid products: {result.ValidProducts.Count}");
            Console.WriteLine($"? Validation errors: {result.ValidationErrors.Count}");
            Console.WriteLine($"?? Interactive decisions made: {result.InteractiveDecisions}");
            Console.WriteLine($"?? Learning examples captured: {result.LearnedExamples}");
            Console.WriteLine();

            // Step 7: Show processed products
            if (result.ValidProducts.Count > 0)
            {
                Console.WriteLine("?? Successfully Processed Products:");
                foreach (var product in result.ValidProducts)
                {
                    Console.WriteLine($"   ?? {product.Code} - {product.Name}");
                    Console.WriteLine($"      Brand ID: {product.BrandID}, Concentration: {product.Concentration}");
                    Console.WriteLine($"      Type: {product.Type}, Size: {product.Size} {product.Units}");
                    Console.WriteLine($"      Gender: {product.Gender}");
                    Console.WriteLine();
                }
            }

            // Step 8: Show validation errors if any
            if (result.ValidationErrors.Count > 0)
            {
                Console.WriteLine("?? Validation Errors:");
                foreach (var error in result.ValidationErrors)
                {
                    Console.WriteLine($"   Row {error.RowNumber}: {error.ErrorMessage}");
                    Console.WriteLine($"      Field: {error.Field}, Value: '{error.Value}'");
                    Console.WriteLine();
                }
            }

            // Step 9: Offer to run comparison
            Console.WriteLine("?? Comparison Test");
            Console.WriteLine("-" + new string('-', 20));
            Console.Write("Would you like to see how this compares to non-interactive parsing? (y/N): ");
            var compareChoice = Console.ReadLine()?.ToLowerInvariant();

            if (compareChoice == "y" || compareChoice == "yes")
            {
                Console.WriteLine("\n?? Running non-interactive comparison...");
                var nonInteractiveResult = await converter.ConvertFileToProductsAsync("interactive-test.csv", fileConfig);

                Console.WriteLine("\n?? Comparison Results:");
                Console.WriteLine($"                     Interactive  Non-Interactive  Improvement");
                Console.WriteLine($"Valid Products:      {result.ValidProducts.Count,10}  {nonInteractiveResult.ValidProducts.Count,14}  {result.ValidProducts.Count - nonInteractiveResult.ValidProducts.Count,11:+#;-#;0}");
                Console.WriteLine($"Validation Errors:   {result.ValidationErrors.Count,10}  {nonInteractiveResult.ValidationErrors.Count,14}  {nonInteractiveResult.ValidationErrors.Count - result.ValidationErrors.Count,11:+#;-#;0}");
                Console.WriteLine($"User Decisions:      {result.InteractiveDecisions,10}  {0,14}  {result.InteractiveDecisions,11}");

                if (result.InteractiveDecisions > 0)
                {
                    Console.WriteLine($"\n?? The interactive mode required {result.InteractiveDecisions} user decisions");
                    Console.WriteLine("   but potentially improved parsing accuracy.");
                }
            }

            // Step 10: Summary and insights
            Console.WriteLine("\n? Interactive Parsing Test Summary");
            Console.WriteLine("=" + new string('=', 40));
            if (result.InteractiveDecisions > 0)
            {
                Console.WriteLine($"?? The parser asked for help {result.InteractiveDecisions} times");
                Console.WriteLine("?? This helps improve accuracy for ambiguous data");
                Console.WriteLine("?? Each decision can be used to improve future parsing");
            }
            else
            {
                Console.WriteLine("?? No user interaction was needed with the current threshold");
                Console.WriteLine($"?? Try lowering the confidence threshold (below {confidenceThreshold:P0}) to see more interactions");
            }

            Console.WriteLine("\n?? Interactive Features Demonstrated:");
            Console.WriteLine("   ? User-guided brand recognition");
            Console.WriteLine("   ? Concentration disambiguation");
            Console.WriteLine("   ? Size and units clarification");
            Console.WriteLine("   ? Product name extraction decisions");
            Console.WriteLine("   ? Configurable confidence thresholds");
            Console.WriteLine("   ? Learning opportunity detection");

            logger.LogInformation("Interactive parsing test completed with {InteractiveDecisions} user decisions", result.InteractiveDecisions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Interactive parsing test failed: {ex.Message}");
            logger.LogError(ex, "Interactive parsing test failed");
            throw;
        }
    }

    /// <summary>
    /// Creates test CSV file specifically designed to trigger interactive decisions
    /// </summary>
    static async Task CreateInteractiveTestCsvFile()
    {
        var csvContent = @"Code,Name,Brand,Size
""COMPLEX001"",""UNKNOWN BRAND MYSTERIOUS ELIXIR 50ML SPRAY POUR HOMME"",""99"",""50ml""
""AMBIGUOUS002"",""RARE PARFUM INTENSE 100ML VAPORISATEUR FEMME TESTER"",""NEWBRAND"",""100""
""UNCLEAR003"",""EXOTIC AQUA FRESH 75ML COLOGNE SPLASH UNISEX"",""888"",""75 oz""
""CONFUSING004"",""VINTAGE EDT CLASSIC 30ML ATOMIZER MEN LIMITED"",""OLDHOUSE"",""30ml""
""UNCERTAIN005"",""MYSTERIOUS HOUSE PURE ESSENCE 125ML POUR FEMME"",""UNKNOWN"",""125""
""SIMPLE006"",""CHANEL NO 5 100ML EDP SPRAY WOMEN"",""1"",""100ml""";

        await File.WriteAllTextAsync("interactive-test.csv", csvContent);
    }

    /// <summary>
    /// Placeholder for running standard tests without database
    /// </summary>
    static async Task RunAllTests(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Running Standard Tests");
        Console.WriteLine("?? Implementation in progress...");
        await Task.Delay(1000);
    }

    /// <summary>
    /// Placeholder for interactive file configuration creator
    /// </summary>
    static async Task CreateNewFileConfigurationInteractive(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("??? Interactive File Configuration Creator");
        Console.WriteLine("?? Implementation in progress...");
        await Task.Delay(1000);
    }
}