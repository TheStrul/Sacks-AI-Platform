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
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksAIPlatform.DataLayer.XlsConverter;
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
        builder.Services.AddScoped<FileDataReader, FileDataReader>();
        builder.Services.AddScoped<IFiletoProductConverter, FiletoProductConverter>();

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

        
        await StartChatInterfaceAsync(app);
    }

    private static async Task StartChatInterfaceAsync(IHost app)
    {
        throw new NotImplementedException();
    }
}
