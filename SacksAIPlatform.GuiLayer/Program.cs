using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using DotNetEnv;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Implementations;
using SacksAIPlatform.LogicLayer.MachineLearning.Pipeline;
using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.GuiLayer.Chat;
using SacksAIPlatform.DataLayer.Csv.Interfaces;
using SacksAIPlatform.DataLayer.Csv.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Implementations;
using SacksAIPlatform.InfrastructuresLayer.AI.Capabilities;
using SacksAIPlatform.LogicLayer.Services;

namespace SacksAIPlatform.GuiLayer;

class Program
{
    private static string? _selectedCsvFilePath = null;
    
    static async Task Main(string[] args)
    {
        // Load environment variables from .env file
        try
        {
            Env.Load();
            Console.WriteLine("✅ Environment variables loaded from .env file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Could not load .env file: {ex.Message}");
            Console.WriteLine("Continuing with system environment variables and appsettings.json");
        }

        var builder = Host.CreateApplicationBuilder(args);

        // Add configuration first
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Configure Serilog based on ShowInfoLogs setting
        var showInfoLogs = configuration.GetValue<bool>("Chat:ShowInfoLogs", false);
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information();

        if (showInfoLogs)
        {
            loggerConfig = loggerConfig.WriteTo.Console();
        }
        else
        {
            // Only show warnings and errors in console, but still log everything to file if needed
            loggerConfig = loggerConfig
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning);
        }

        Log.Logger = loggerConfig.CreateLogger();

        // Add configuration to services
        builder.Services.AddSingleton<IConfiguration>(configuration);

        // Configure logging levels based on ShowInfoLogs setting
        if (!showInfoLogs)
        {
            // Override logging configuration to hide info logs
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
        }

        // Add DbContext
        builder.Services.AddDbContext<SacksDbContext>(options =>
        {
            var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        // Register repositories
        builder.Services.AddScoped<IBrandRepository, BrandRepository>();
        builder.Services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
        builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
        builder.Services.AddScoped<IProductRepository, PerfumeRepository>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        builder.Services.AddScoped<ProductMLPipeline>();
        builder.Services.AddScoped<IFileDataReader, FileDataReader>();
        builder.Services.AddScoped<IFiletoProductConverter, FiletoProductConverter>();
        builder.Services.AddScoped<FolderAndFileCapability>();

        // Register AI services - Infrastructure layer AI with business service wrapper
        builder.Services.AddScoped<IConversationalAgent, AiAgent>();
        builder.Services.AddScoped<ProductsInventoryAIService>();
        builder.Services.AddScoped<ChatInterface>();

        var app = builder.Build();

        // Ensure database is created
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<SacksDbContext>();
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
