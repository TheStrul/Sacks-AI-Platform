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
            Console.WriteLine("3. ?? View Database Statistics");
            Console.WriteLine("4. ?? Database Management Options");
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice (0-4): ");

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
                        await ShowDatabaseStatistics(services, logger);
                        break;
                    case "4":
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
            var runtimeManager = new ProductParserRuntimeManager(configManager);

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
                Console.WriteLine("\n?? Sample Processed Products (first 5):");
                foreach (var product in result.ValidProducts.Take(5))
                {
                    Console.WriteLine($"   ?? {product.Code} - {product.Name}");
                    Console.WriteLine($"      Brand ID: {product.BrandID}, Concentration: {product.Concentration}");
                    Console.WriteLine($"      Type: {product.Type}, Size: {product.Size} {product.Units}");
                    Console.WriteLine();
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
        Console.WriteLine("3. Enter file path manually");
        Console.Write("Choice (1-3): ");

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

            case "3":
                Console.Write("Enter file path: ");
                return Console.ReadLine();

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

    // Keep existing RunAllTests method for backwards compatibility
    static async Task RunAllTests(IServiceProvider services, ILogger<Program> logger)
    {
        try
        {
            // Show test capabilities
            TestRunner.ShowTestCapabilities();

            // Test 1: Basic Parser Configuration
            await TestBasicParserConfiguration(services, logger);

            // Test 2: Runtime Dictionary Management
            await TestRuntimeDictionaryManagement(services, logger);

            // Test 3: Complex Description Parsing
            await TestComplexDescriptionParsing(services, logger);

            // Test 4: File Conversion Integration
            await TestFileConversionIntegration(services, logger);

            // Test 5: Custom Parsing Rules
            await TestCustomParsingRules(services, logger);

            // Test 6: Configuration Persistence
            await TestConfigurationPersistence(services, logger);

            // Test 7: Example Usage Tests
            Console.WriteLine("\n?? Test 7: Example Usage Integration");
            Console.WriteLine("=" + new string('=', 50));
            TestRunner.RunExampleUsageTests();

            // Show next steps
            TestRunner.ShowNextSteps();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during testing");
            throw;
        }
    }

    static async Task TestBasicParserConfiguration(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("\n?? Test 1: Basic Parser Configuration");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            var configManager = services.GetRequiredService<ProductParserConfigurationManager>();
            var runtimeManager = services.GetRequiredService<ProductParserRuntimeManager>();

            // Show default configuration statistics
            var stats = runtimeManager.GetStatistics();
            Console.WriteLine($"? Default configuration loaded:");
            Console.WriteLine($"   ?? Total mappings: {stats.TotalMappings}");
            Console.WriteLine($"   ?? Parsing rules: {stats.ParsingRules}");
            Console.WriteLine($"   ?? Ignore patterns: {stats.IgnorePatterns}");

            // Test the example from your use case
            var testDescription = "ADP BLU MEDITERRANEO MIRTO DI PANAREA 30ML EDT SPRAY 29.6ml";
            Console.WriteLine($"\n?? Testing your example: {testDescription}");

            var result = runtimeManager.TestParsing(testDescription);
            Console.WriteLine($"? Parsing result: {result.GetSummary()}");
            Console.WriteLine($"?? Matched rules: {string.Join(", ", result.MatchedRules)}");

            // Test individual components
            Console.WriteLine($"\n?? Individual components detected:");
            Console.WriteLine($"   ?? Concentration: {result.Concentration} (Expected: Parfum)");
            Console.WriteLine($"   ?? Size: {result.Size} (Expected: 30)");
            Console.WriteLine($"   ?? Units: {result.Units} (Expected: ml)");
            Console.WriteLine($"   ?? Type: {result.Type} (Expected: Spray)");

            logger.LogInformation("Basic parser configuration test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed: {ex.Message}");
            logger.LogError(ex, "Basic parser configuration test failed");
            throw;
        }
    }

    static async Task TestRuntimeDictionaryManagement(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("\n?? Test 2: Runtime Dictionary Management");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            var runtimeManager = services.GetRequiredService<ProductParserRuntimeManager>();

            // Add custom brand mappings
            Console.WriteLine("? Adding custom brand mappings...");
            runtimeManager.AddBrandMapping("CHANEL", 1);
            runtimeManager.AddBrandMapping("DIOR", 2);
            runtimeManager.AddBrandMapping("TOM FORD", 3);
            runtimeManager.AddBrandMapping("VERSACE", 4);

            // Add custom concentration mappings
            Console.WriteLine("? Adding custom concentration mappings...");
            runtimeManager.AddConcentrationMapping("AQUA", Concentration.EDC);
            runtimeManager.AddConcentrationMapping("INTENSE", Concentration.Parfum);
            runtimeManager.AddConcentrationMapping("PURE", Concentration.EDP);

            // Add custom type mappings
            Console.WriteLine("? Adding custom type mappings...");
            runtimeManager.AddTypeMapping("ATOMIZER", PerfumeType.Spray);
            runtimeManager.AddTypeMapping("VAPORISATEUR", PerfumeType.Spray);

            // Test with new mappings
            var testCases = new[]
            {
                "CHANEL NO 5 100ML AQUA ATOMIZER",
                "DIOR SAUVAGE INTENSE 50ML EDT SPRAY",
                "TOM FORD OUD WOOD PURE 30ML VAPORISATEUR",
                "VERSACE BRIGHT CRYSTAL 90ML EDT SPRAY"
            };

            Console.WriteLine("\n?? Testing with new mappings:");
            foreach (var testCase in testCases)
            {
                var result = runtimeManager.TestParsing(testCase);
                Console.WriteLine($"   ?? {testCase}");
                Console.WriteLine($"      ? {result.GetSummary()}");
            }

            var newStats = runtimeManager.GetStatistics();
            Console.WriteLine($"\n?? Updated statistics:");
            Console.WriteLine($"   ??? Brand mappings: {newStats.BrandMappings}");
            Console.WriteLine($"   ?? Concentration mappings: {newStats.ConcentrationMappings}");
            Console.WriteLine($"   ?? Type mappings: {newStats.TypeMappings}");

            logger.LogInformation("Runtime dictionary management test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed: {ex.Message}");
            logger.LogError(ex, "Runtime dictionary management test failed");
            throw;
        }
    }

    static async Task TestComplexDescriptionParsing(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("\n?? Test 3: Complex Description Parsing");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            var runtimeManager = services.GetRequiredService<ProductParserRuntimeManager>();

            // Test complex descriptions that might come from real data
            var complexDescriptions = new[]
            {
                "ARMANI CODE BLACK 75ML EDP SPRAY FOR MEN TESTER",
                "BULGARI OMNIA CRYSTALLINE 65ML EDT WOMEN PERFUME",
                "CREED AVENTUS 120ML PARFUM SPRAY UNISEX LIMITED EDITION",
                "HUGO BOSS BOTTLED NIGHT 100ML EDT HOMME SPLASH",
                "ISSEY MIYAKE L'EAU D'ISSEY 125ML EDT POUR FEMME",
                "JEAN PAUL GAULTIER LE MALE 200ML EDT SPRAY MASCULINE"
            };

            Console.WriteLine("?? Testing complex real-world descriptions:");
            foreach (var description in complexDescriptions)
            {
                Console.WriteLine($"\n?? Testing: {description}");

                var testResult = runtimeManager.TestParsingWithComparison(description);
                Console.WriteLine($"   ? Changes: {testResult.GetSummary()}");
                Console.WriteLine($"   ?? Matches: {testResult.FoundMatches}");

                if (testResult.ParsedInfo.MatchedRules.Count > 0)
                {
                    Console.WriteLine($"   ?? Rules: {string.Join(", ", testResult.ParsedInfo.MatchedRules)}");
                }
            }

            logger.LogInformation("Complex description parsing test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed: {ex.Message}");
            logger.LogError(ex, "Complex description parsing test failed");
            throw;
        }
    }

    static async Task TestFileConversionIntegration(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("\n?? Test 4: File Conversion Integration");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            var configManager = services.GetRequiredService<ProductParserConfigurationManager>();

            // Create test CSV data
            await CreateTestCsvFile();

            // Create converter with our configured parser
            var converter = new FiletoProductConverter(configManager);

            Console.WriteLine("?? Created test CSV file with complex product descriptions");
            Console.WriteLine("?? Testing file conversion with parser integration...");

            // Create file configuration for our test data
            var fileConfig = new FileConfiguration
            {
                TitleIndex = 0,
                StartFromRow = 1,
                EndAtRow = -1,
                HasInnerTitles = false,
                FormatName = "TestFormat",
                ValidNumOfColumns = 4,
                ColumnMapping = new Dictionary<int, PropertyType>
                {
                    { 0, PropertyType.Code },
                    { 1, PropertyType.Name },
                    { 2, PropertyType.Brand },
                    { 3, PropertyType.Size }
                },
                DescriptionColumns = new() { 1 }, // Use name column for description parsing
                IgnoredColumns = new()
            };

            // Process the file
            var result = await converter.ConvertFileToProductsAsync("test-data.csv", fileConfig);

            Console.WriteLine($"\n?? Conversion Results:");
            Console.WriteLine($"   ?? Lines processed: {result.TotalLinesProcessed}");
            Console.WriteLine($"   ? Valid products: {result.ValidProducts.Count}");
            Console.WriteLine($"   ? Validation errors: {result.ValidationErrors.Count}");

            // Show some converted products
            Console.WriteLine($"\n?? Sample converted products:");
            foreach (var product in result.ValidProducts.Take(3))
            {
                Console.WriteLine($"   ?? Code: {product.Code}");
                Console.WriteLine($"      Name: {product.Name}");
                Console.WriteLine($"      Concentration: {product.Concentration}");
                Console.WriteLine($"      Type: {product.Type}");
                Console.WriteLine($"      Size: {product.Size} {product.Units}");
                Console.WriteLine($"      Brand ID: {product.BrandID}");
                Console.WriteLine();
            }

            if (result.ValidationErrors.Count > 0)
            {
                Console.WriteLine($"?? Validation errors found:");
                foreach (var error in result.ValidationErrors.Take(3))
                {
                    Console.WriteLine($"   Row {error.RowNumber}: {error.ErrorMessage}");
                }
            }

            logger.LogInformation("File conversion integration test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed: {ex.Message}");
            logger.LogError(ex, "File conversion integration test failed");
            throw;
        }
    }

    static async Task TestCustomParsingRules(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("\n?? Test 5: Custom Parsing Rules");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            var runtimeManager = services.GetRequiredService<ProductParserRuntimeManager>();

            // Add custom parsing rules
            Console.WriteLine("? Adding custom parsing rules...");

            // Rule to extract limited edition information
            runtimeManager.AddParsingRule(
                name: "ExtractLimitedEdition",
                pattern: @"\b(LIMITED|EDITION|LE|COLLECTOR)\b",
                propertyType: PropertyType.Remarks,
                priority: 5
            );

            // Rule to extract tester information
            runtimeManager.AddParsingRule(
                name: "ExtractTester",
                pattern: @"\b(TESTER|TST|DEMO)\b",
                propertyType: PropertyType.Remarks,
                priority: 6
            );

            // Rule to extract gender-specific information
            runtimeManager.AddParsingRule(
                name: "ExtractGenderSpecific",
                pattern: @"\b(POUR HOMME|POUR FEMME|FOR MEN|FOR WOMEN|MASCULINE|FEMININE)\b",
                propertyType: PropertyType.Gender,
                priority: 7
            );

            // Test with descriptions containing these patterns
            var testDescriptions = new[]
            {
                "CHANEL NO 5 LIMITED EDITION 100ML EDP SPRAY",
                "DIOR SAUVAGE TESTER 100ML EDT SPRAY",
                "TOM FORD NOIR POUR HOMME 50ML EDP",
                "VERSACE BRIGHT CRYSTAL FOR WOMEN 90ML EDT",
                "CREED AVENTUS COLLECTOR EDITION 120ML PARFUM"
            };

            Console.WriteLine("\n?? Testing with custom rules:");
            foreach (var description in testDescriptions)
            {
                var result = runtimeManager.TestParsing(description);
                Console.WriteLine($"   ?? {description}");
                Console.WriteLine($"      ?? Rules matched: {string.Join(", ", result.MatchedRules)}");
                Console.WriteLine($"      ? Result: {result.GetSummary()}");
            }

            logger.LogInformation("Custom parsing rules test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed: {ex.Message}");
            logger.LogError(ex, "Custom parsing rules test failed");
            throw;
        }
    }

    static async Task TestConfigurationPersistence(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("\n?? Test 6: Configuration Persistence");
        Console.WriteLine("=" + new string('=', 50));

        try
        {
            var configManager = services.GetRequiredService<ProductParserConfigurationManager>();

            // Create backup
            Console.WriteLine("?? Creating configuration backup...");
            configManager.CreateBackup("test-backup.json");

            // Export current configuration
            Console.WriteLine("?? Exporting current configuration...");
            configManager.ExportConfiguration("exported-config.json");

            // Validate configuration
            Console.WriteLine("? Validating configuration...");
            var errors = configManager.ValidateConfiguration();

            if (errors.Count > 0)
            {
                Console.WriteLine("?? Configuration validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"   - {error}");
                }
            }
            else
            {
                Console.WriteLine("? Configuration is valid!");
            }

            // Show current configuration in JSON format
            var config = configManager.CurrentConfiguration;
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var configJson = JsonSerializer.Serialize(config, jsonOptions);

            Console.WriteLine($"\n?? Current configuration preview (first 500 chars):");
            Console.WriteLine($"```json");
            Console.WriteLine(configJson.Length > 500 ? configJson.Substring(0, 500) + "..." : configJson);
            Console.WriteLine($"```");

            Console.WriteLine($"\n?? Configuration files created:");
            Console.WriteLine($"   ?? test-backup.json");
            Console.WriteLine($"   ?? exported-config.json");
            Console.WriteLine($"   ?? product-parser-config.json (main config)");

            logger.LogInformation("Configuration persistence test completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Test failed: {ex.Message}");
            logger.LogError(ex, "Configuration persistence test failed");
            throw;
        }
    }

    static async Task CreateNewFileConfigurationInteractive(IServiceProvider services, ILogger<Program> logger)
    {
        Console.WriteLine("?? Interactive File Configuration Creator");
        Console.WriteLine("=" + new string('=', 45));
        Console.WriteLine();
        Console.WriteLine("Welcome to the interactive FileConfigurationHolder creator!");
        Console.WriteLine("I'll guide you through creating a new file configuration step by step.");
        Console.WriteLine();

        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SacksDbContext>();

            // Step 1: Basic Information
            Console.WriteLine("?? Step 1: Basic Information");
            Console.WriteLine("-" + new string('-', 30));

            // Configuration Name
            string configName;
            while (true)
            {
                Console.Write("?? What would you like to name this configuration? (e.g., 'Supplier ABC CSV Format'): ");
                configName = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(configName))
                {
                    Console.WriteLine("?? Configuration name cannot be empty. Please try again.");
                    continue;
                }

                if (configName.Length > 200)
                {
                    Console.WriteLine("?? Configuration name is too long (max 200 characters). Please try again.");
                    continue;
                }

                break;
            }
            Console.WriteLine($"? Configuration name: '{configName}'");

            // Step 2: Supplier Selection
            Console.WriteLine("\n?? Step 2: Supplier Selection");
            Console.WriteLine("-" + new string('-', 25));

            var suppliers = await dbContext.Suppliers.ToListAsync();
            Supplier selectedSupplier;

            if (suppliers.Count == 0)
            {
                Console.WriteLine("?? No suppliers found in database. Let's create one first!");
                selectedSupplier = await CreateSupplierInteractive(dbContext);
            }
            else
            {
                Console.WriteLine("Available suppliers:");
                for (int i = 0; i < suppliers.Count; i++)
                {
                    var supplier = suppliers[i];
                    Console.WriteLine($"{i + 1}. {supplier.Name} ({supplier.Type}, {supplier.Country})");
                }
                Console.WriteLine($"{suppliers.Count + 1}. Create new supplier");

                int supplierChoice;
                while (true)
                {
                    Console.Write($"?? Select supplier (1-{suppliers.Count + 1}): ");
                    if (int.TryParse(Console.ReadLine(), out supplierChoice))
                    {
                        if (supplierChoice >= 1 && supplierChoice <= suppliers.Count)
                        {
                            selectedSupplier = suppliers[supplierChoice - 1];
                            break;
                        }
                        else if (supplierChoice == suppliers.Count + 1)
                        {
                            selectedSupplier = await CreateSupplierInteractive(dbContext);
                            break;
                        }
                    }
                    Console.WriteLine("?? Invalid choice. Please try again.");
                }
            }
            Console.WriteLine($"? Selected supplier: {selectedSupplier.Name}");

            // Step 3: File Pattern Information
            Console.WriteLine("\n?? Step 3: File Pattern Information");
            Console.WriteLine("-" + new string('=', 30));

            // File Name Pattern
            string fileNamePattern;
            while (true)
            {
                Console.WriteLine("?? What file name pattern should this configuration match?");
                Console.WriteLine("   Examples: *.csv, *inventory*.xlsx, supplier-data-*.xls, products.csv");
                Console.Write("   Pattern: ");
                fileNamePattern = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(fileNamePattern))
                {
                    Console.WriteLine("?? File name pattern cannot be empty. Please try again.");
                    continue;
                }

                if (fileNamePattern.Length > 100)
                {
                    Console.WriteLine("?? File name pattern is too long (max 100 characters). Please try again.");
                    continue;
                }

                break;
            }
            Console.WriteLine($"? File pattern: '{fileNamePattern}'");

            // File Extension
            string fileExtension;
            while (true)
            {
                Console.WriteLine("?? What file extension does this configuration handle?");
                Console.WriteLine("   Examples: .csv, .xlsx, .xls, .txt");
                Console.Write("   Extension: ");
                fileExtension = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(fileExtension))
                {
                    Console.WriteLine("?? File extension cannot be empty. Please try again.");
                    continue;
                }

                if (!fileExtension.StartsWith("."))
                {
                    fileExtension = "." + fileExtension;
                }

                if (fileExtension.Length > 10)
                {
                    Console.WriteLine("?? File extension is too long (max 10 characters). Please try again.");
                    continue;
                }

                break;
            }
            Console.WriteLine($"? File extension: '{fileExtension}'");

            // Step 4: Configuration Type Choice
            Console.WriteLine("\n?? Step 4: Configuration Setup");
            Console.WriteLine("-" + new string('=', 25));

            FileConfiguration fileConfig;
            Console.WriteLine("?? How would you like to create the file configuration?");
            Console.WriteLine("1. Use default configuration (recommended for most CSV files)");
            Console.WriteLine("2. Create custom configuration (advanced)");
            Console.Write("Choice (1-2): ");

            var configChoice = Console.ReadLine();
            if (configChoice == "2")
            {
                fileConfig = await CreateCustomFileConfiguration();
            }
            else
            {
                fileConfig = FileConfiguration.CreateDefaultConfiguration();
                fileConfig.FormatName = configName;
                Console.WriteLine("? Using default configuration with standard CSV settings");
            }

            // Step 5: Optional Remarks
            Console.WriteLine("\n?? Step 5: Optional Remarks");
            Console.WriteLine("-" + new string('=', 25));

            Console.WriteLine("?? Any additional notes or remarks about this configuration? (optional)");
            Console.WriteLine("   Examples: 'Weekly inventory files', 'Special format for Product X', etc.");
            Console.Write("   Remarks: ");
            var remarks = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(remarks) && remarks.Length > 500)
            {
                remarks = remarks.Substring(0, 500);
                Console.WriteLine("?? Remarks truncated to 500 characters.");
            }

            // Step 6: Summary and Confirmation
            Console.WriteLine("\n?? Step 6: Review and Confirmation");
            Console.WriteLine("-" + new string('=', 35));

            Console.WriteLine("Here's a summary of your new file configuration:");
            Console.WriteLine($"   ?? Name: {configName}");
            Console.WriteLine($"   ?? Supplier: {selectedSupplier.Name}");
            Console.WriteLine($"   ?? File Pattern: {fileNamePattern}");
            Console.WriteLine($"   ?? File Extension: {fileExtension}");
            Console.WriteLine($"   ?? Configuration Type: {fileConfig.FormatName}");
            Console.WriteLine($"   ?? Remarks: {(string.IsNullOrEmpty(remarks) ? "(none)" : remarks)}");

            Console.WriteLine();
            Console.Write("?? Does this look correct? Save to database? (y/N): ");
            var saveConfirmation = Console.ReadLine()?.ToLowerInvariant();

            if (saveConfirmation == "y" || saveConfirmation == "yes")
            {
                // Create and save the FileConfigurationHolder
                var configJson = JsonSerializer.Serialize(fileConfig, new JsonSerializerOptions { WriteIndented = true });

                var fileConfigHolder = new FileConfigurationHolder
                {
                    Name = configName,
                    SupplierId = selectedSupplier.SupplierID,
                    FileNamePattern = fileNamePattern,
                    FileExtension = fileExtension,
                    ConfigurationJson = configJson,
                    Remarks = remarks,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.FileConfigurationHolders.Add(fileConfigHolder);
                await dbContext.SaveChangesAsync();

                Console.WriteLine("\n?? Success!");
                Console.WriteLine($"? File configuration '{configName}' has been created and saved to the database.");
                Console.WriteLine($"?? Configuration ID: {fileConfigHolder.Id}");

                logger.LogInformation("Interactive file configuration created: {ConfigName} for supplier {SupplierName}",
                    configName, selectedSupplier.Name);
            }
            else
            {
                Console.WriteLine("? Configuration creation cancelled.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error creating file configuration: {ex.Message}");
            logger.LogError(ex, "Error in interactive file configuration creation");
            throw;
        }
    }

    static async Task<Supplier> CreateSupplierInteractive(SacksDbContext dbContext)
    {
        Console.WriteLine("\n?? Creating New Supplier");
        Console.WriteLine("-" + new string('-', 25));

        // Supplier Name
        string supplierName;
        while (true)
        {
            Console.Write("?? Supplier name: ");
            supplierName = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(supplierName))
            {
                Console.WriteLine("?? Supplier name cannot be empty. Please try again.");
                continue;
            }

            if (supplierName.Length > 200)
            {
                Console.WriteLine("?? Supplier name is too long (max 200 characters). Please try again.");
                continue;
            }

            break;
        }

        // Supplier Type
        Console.WriteLine("?? Supplier type (e.g., Distributor, Retailer, Manufacturer, Wholesaler): ");
        var supplierType = Console.ReadLine()?.Trim() ?? "Distributor";
        if (supplierType.Length > 100)
        {
            supplierType = supplierType.Substring(0, 100);
        }

        // Country
        Console.WriteLine("?? Country: ");
        var country = Console.ReadLine()?.Trim() ?? string.Empty;
        if (country.Length > 100)
        {
            country = country.Substring(0, 100);
        }

        // Contact Info
        Console.WriteLine("?? Contact information (email, phone, etc.) [optional]: ");
        var contactInfo = Console.ReadLine()?.Trim() ?? string.Empty;
        if (contactInfo.Length > 500)
        {
            contactInfo = contactInfo.Substring(0, 500);
        }

        var supplier = new Supplier
        {
            Name = supplierName,
            Type = supplierType,
            Country = country,
            ContactInfo = contactInfo
        };

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"? Supplier '{supplierName}' created successfully!");
        return supplier;
    }

    static async Task<FileConfiguration> CreateCustomFileConfiguration()
    {
        Console.WriteLine("\n?? Creating Custom File Configuration");
        Console.WriteLine("-" + new string('=', 35));
        Console.WriteLine("Let's set up the file structure and column mappings...");

        // Start with default and modify
        var config = FileConfiguration.CreateDefaultConfiguration();

        // Format Name
        Console.Write("?? Configuration format name: ");
        var formatName = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(formatName))
        {
            config.FormatName = formatName;
        }

        // Row settings
        Console.WriteLine("\n?? Row Settings:");
        Console.WriteLine($"Current settings: Start from row {config.StartFromRow}, End at row {(config.EndAtRow == -1 ? "end of file" : config.EndAtRow.ToString())}");

        Console.Write("?? Start reading from which row? (0-based, current: 1): ");
        if (int.TryParse(Console.ReadLine(), out var startRow))
        {
            config.StartFromRow = startRow;
        }

        Console.Write("?? End at which row? (-1 for end of file, current: -1): ");
        if (int.TryParse(Console.ReadLine(), out var endRow))
        {
            config.EndAtRow = endRow;
        }

        Console.Write("?? Does the file have inner title rows to skip? (y/N): ");
        var hasInnerTitles = Console.ReadLine()?.ToLowerInvariant();
        config.HasInnerTitles = hasInnerTitles == "y" || hasInnerTitles == "yes";

        Console.Write("?? Expected number of columns per row: ");
        if (int.TryParse(Console.ReadLine(), out var numColumns))
        {
            config.ValidNumOfColumns = numColumns;
        }

        // Column mapping
        Console.WriteLine("\n?? Column Mapping:");
        Console.WriteLine("Let's map which columns contain which type of data...");
        Console.WriteLine("Available property types: Code, Name, Brand, Concentration, Type, Gender, Size, LilFree, CountryOfOrigin");

        config.ColumnMapping.Clear();
        Console.Write("?? How many columns do you want to map? ");
        if (int.TryParse(Console.ReadLine(), out var mappingCount))
        {
            for (int i = 0; i < mappingCount; i++)
            {
                Console.Write($"Column {i + 1} - Column number (1-based): ");
                if (int.TryParse(Console.ReadLine(), out var colNum))
                {
                    Console.Write($"Column {i + 1} - Property type (Code/Name/Brand/etc.): ");
                    var propTypeStr = Console.ReadLine()?.Trim();
                    if (Enum.TryParse<PropertyType>(propTypeStr, true, out var propType))
                    {
                        config.ColumnMapping[colNum] = propType;
                        Console.WriteLine($"? Column {colNum} ? {propType}");
                    }
                }
            }
        }

        // Description columns for parsing
        Console.WriteLine("\n?? Description Parsing:");
        Console.Write("?? Which columns contain product descriptions for parsing? (comma-separated, 1-based): ");
        var descCols = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(descCols))
        {
            config.DescriptionColumns.Clear();
            foreach (var col in descCols.Split(','))
            {
                if (int.TryParse(col.Trim(), out var colIndex))
                {
                    config.DescriptionColumns.Add(colIndex);
                }
            }
        }

        Console.WriteLine($"? Custom configuration created with {config.ColumnMapping.Count} mapped columns");
        return config;
    }
}