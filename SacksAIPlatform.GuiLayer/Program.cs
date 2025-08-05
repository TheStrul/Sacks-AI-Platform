using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Implementations;
using SacksAIPlatform.LogicLayer.Services;
using SacksAIPlatform.LogicLayer.MachineLearning.Pipeline;
using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.GuiLayer.Chat;
using SacksAIPlatform.DataLayer.Csv.Interfaces;
using SacksAIPlatform.DataLayer.Csv.Implementations;
using SacksAIPlatform.InfrastructuresLayer.Excel.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Excel.Implementations;
using SacksAIPlatform.InfrastructuresLayer.Csv.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Csv.Implementations;

namespace SacksAIPlatform.GuiLayer;

class Program
{
    private static string? _selectedCsvFilePath = null;
    
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Add Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Add configuration
        builder.Services.AddSingleton<IConfiguration>(serviceProvider =>
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            return configuration;
        });

        // Add DbContext
        builder.Services.AddDbContext<PerfumeDbContext>(options =>
        {
            var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
        });

        // Register repositories
        builder.Services.AddScoped<IBrandRepository, BrandRepository>();
        builder.Services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
        builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
        builder.Services.AddScoped<IPerfumeRepository, PerfumeRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        builder.Services.AddScoped<PerfumeBusinessService>();
        builder.Services.AddScoped<ProductMLPipeline>();
        builder.Services.AddScoped<PerfumeImportService>();
        builder.Services.AddScoped<ICsvFileReader, CsvFileReader>();
        builder.Services.AddScoped<ICsvPerfumeConverter, CsvPerfumeConverter>();

        // Register AI services - Infrastructure layer AI with business service wrapper
        builder.Services.AddScoped<IIntentRecognitionService, OpenAIIntentRecognitionService>();
        builder.Services.AddScoped<IConversationalAgent, GenericLLMAgent>();
        builder.Services.AddScoped<PerfumeInventoryAIService>();
        builder.Services.AddScoped<ChatInterface>();
        
        // Register Excel file handler
        builder.Services.AddScoped<IExcelFileHandler, ExcelFileHandler>();

        var app = builder.Build();

        // Ensure database is created
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<PerfumeDbContext>();
                context.Database.EnsureCreated();
                Log.Information("Database initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize database: {ErrorMessage}", ex.Message);
                return;
            }
        }

        // Start the conversational AI interface
        await StartChatInterfaceAsync(app);
    }

    static async Task StartChatInterfaceAsync(IHost app)
    {
        using var scope = app.Services.CreateScope();
        
        try
        {
            Log.Information("🤖 Starting Sacks AI Platform - Conversational Agent");
            Log.Information("🎯 Ready for natural language interaction");
            Log.Information("");
            
            var chatInterface = scope.ServiceProvider.GetRequiredService<ChatInterface>();
            await chatInterface.StartAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start chat interface: {ErrorMessage}", ex.Message);
        }
    }

    static async Task<string> SelectCsvFileAsync()
    {
        try
        {
            Log.Information("🔍 Scanning for CSV files...");
            
            // Get current directory
            var currentDirectory = Directory.GetCurrentDirectory();
            
            // Search for CSV files in current directory and subdirectories
            var csvFiles = Directory.GetFiles(currentDirectory, "*.csv", SearchOption.AllDirectories)
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime) // Sort by date, newest first
                .Take(10) // Limit to 10 most recent files
                .ToList();
                
            if (!csvFiles.Any())
            {
                Log.Warning("⚠️ No CSV files found in the current directory or subdirectories.");
                Log.Information("Current directory: {CurrentDirectory}", currentDirectory);
                
                // Return empty string to indicate no file found
                return string.Empty;
            }
            
            Log.Information("📁 Found {Count} CSV file(s):", csvFiles.Count);
            Log.Information("");
            
            // Display files with details
            for (int i = 0; i < csvFiles.Count; i++)
            {
                var file = csvFiles[i];
                var sizeKB = file.Length / 1024.0;
                var relativePath = Path.GetRelativePath(currentDirectory, file.FullName);
                
                Log.Information($"  {i + 1}. {file.Name}");
                Log.Information($"     📍 Path: {relativePath}");
                Log.Information($"     📊 Size: {sizeKB:F1} KB");
                Log.Information($"     📅 Modified: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                
                // Try to get record count
                try
                {
                    var lineCount = File.ReadAllLines(file.FullName).Length - 1; // -1 for header
                    Log.Information($"     📈 Records: ~{lineCount:N0}");
                }
                catch
                {
                    Log.Information($"     📈 Records: Unable to determine");
                }
                
                Log.Information("");
            }
            
            // Get user selection
            while (true)
            {
                Console.Write($"Please select a file (1-{csvFiles.Count}) or 0 to exit: ");
                var input = Console.ReadLine();
                
                if (int.TryParse(input, out int selection))
                {
                    if (selection == 0)
                    {
                        Log.Information("⚡ Exiting file selection");
                        Environment.Exit(0);
                    }
                    
                    if (selection >= 1 && selection <= csvFiles.Count)
                    {
                        var selectedFile = csvFiles[selection - 1];
                        Log.Information("✅ Selected: {FileName}", selectedFile.Name);
                        Log.Information("📍 Full path: {FullPath}", selectedFile.FullName);
                        Log.Information("");
                        
                        return selectedFile.FullName;
                    }
                }
                
                Log.Warning("❌ Invalid selection. Please enter a number between 1 and {Count}, or 0 to exit.", csvFiles.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to scan for CSV files: {ErrorMessage}", ex.Message);
            return string.Empty;
        }
    }
}
