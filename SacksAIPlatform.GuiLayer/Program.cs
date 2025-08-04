using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Implementations;

namespace SacksAIPlatform.GuiLayer;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting Sacks AI Platform");

            var host = CreateHostBuilder(args).Build();

            // Run the application
            await RunApplication(host);

            Log.Information("Application completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Configure Entity Framework
                services.AddDbContext<PerfumeDbContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("DefaultConnection") 
                        ?? "Server=(localdb)\\mssqllocaldb;Database=SacksAIPlatformDb;Trusted_Connection=true;MultipleActiveResultSets=true"));

                // Register repositories
                services.AddScoped<IUnitOfWork, UnitOfWork>();
                services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
                services.AddScoped<IBrandRepository, BrandRepository>();
                services.AddScoped<ISupplierRepository, SupplierRepository>();
                services.AddScoped<IPerfumeRepository, PerfumeRepository>();
            });

    static async Task RunApplication(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Log.Information("=== Sacks AI Platform - Perfume Database Management ===");
        Log.Information("Architecture: 5-Layer Clean Architecture");
        Log.Information("- OsKLayer: OS-specific operations");
        Log.Information("- InfrastructuresLayer: External services");
        Log.Information("- DataLayer: EF Core with Repository Pattern ✓");
        Log.Information("- LogicLayer: Business logic");
        Log.Information("- GuiLayer: Console interface ✓");
        
        Log.Information("Database: Entity Framework Core with SQL Server");
        Log.Information("Entities: Manufacturer, Brand, Supplier, Perfume");
        
        // Test database connection
        try
        {
            var manufacturers = await unitOfWork.Manufacturers.GetAllAsync();
            Log.Information($"Database connection successful. Found {manufacturers.Count()} manufacturers.");
        }
        catch (Exception ex)
        {
            Log.Warning($"Database connection issue: {ex.Message}");
            Log.Information("This is normal on first run - database needs to be created.");
        }

        Log.Information("First stage implementation complete!");
        Log.Information("Ready for next development phase...");
    }
}
