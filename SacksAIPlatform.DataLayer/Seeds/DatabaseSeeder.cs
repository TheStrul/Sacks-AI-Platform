using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.DataLayer.Seeds;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PerfumeDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Manufacturers.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Seed Manufacturers
        var manufacturers = GetManufacturers();
        await context.Manufacturers.AddRangeAsync(manufacturers);
        await context.SaveChangesAsync();

        // Seed Brands with proper manufacturer relationships
        var brands = GetBrands(manufacturers);
        await context.Brands.AddRangeAsync(brands);
        await context.SaveChangesAsync();
    }
}
}
