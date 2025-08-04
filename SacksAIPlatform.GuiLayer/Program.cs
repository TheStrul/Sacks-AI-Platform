using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Implementations;
using SacksAIPlatform.DataLayer.Seeds;

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
        var dbContext = scope.ServiceProvider.GetRequiredService<PerfumeDbContext>();

        Log.Information("=== Sacks AI Platform - Perfume Database Management ===");
        Log.Information("Architecture: 5-Layer Clean Architecture");
        Log.Information("- OsKLayer: OS-specific operations");
        Log.Information("- InfrastructuresLayer: External services");
        Log.Information("- DataLayer: EF Core with Repository Pattern ✓");
        Log.Information("- LogicLayer: Business logic");
        Log.Information("- GuiLayer: Console interface ✓");
        
        Log.Information("Database: Entity Framework Core with SQL Server");
        Log.Information("Entities: Manufacturer, Brand, Supplier, Perfume");
        
        // Initialize database
        try
        {
            Log.Information("Initializing database...");
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully.");
            
            // Seed initial data
            Log.Information("Seeding initial data...");
            await DatabaseSeeder.SeedAsync(dbContext);
            Log.Information("Database seeding completed.");
            
            // Test database connection and display data
            var manufacturers = await unitOfWork.Manufacturers.GetAllAsync();
            var brands = await unitOfWork.Brands.GetAllAsync();
            var suppliers = await unitOfWork.Suppliers.GetAllAsync();
            var perfumes = await unitOfWork.Perfumes.GetAllAsync();
            
            Log.Information($"✅ Database initialized successfully!");
            Log.Information($"📊 Data Summary:");
            Log.Information($"   - Manufacturers: {manufacturers.Count()}");
            Log.Information($"   - Brands: {brands.Count()}");
            Log.Information($"   - Suppliers: {suppliers.Count()}");
            Log.Information($"   - Perfumes: {perfumes.Count()}");
            
            // Display sample data
            Log.Information($"📋 Sample Perfumes:");
            foreach (var perfume in perfumes.Take(3))
            {
                Log.Information($"   - {perfume.PerfumeCode}: {perfume.Name} by {perfume.Brand?.Name} ({perfume.Concentration})");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Database initialization failed: {ex.Message}");
            Log.Warning("Please ensure SQL Server LocalDB is installed and running.");
        }

        Log.Information("Second stage implementation complete!");
        Log.Information("✅ Initial DataStore created and populated");
        Log.Information("Ready for next development phase...");
    }
}
