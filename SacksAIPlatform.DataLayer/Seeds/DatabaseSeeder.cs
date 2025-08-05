using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using System.Text.Json;
using System.Reflection;

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

        try
        {
            // Load data from JSON file
            var seedData = await LoadSeedDataFromJsonAsync();
            
            // Seed Manufacturers first
            var manufacturers = seedData.Manufacturers.Select(m => new Manufacturer
            {
                Name = m.Name,
                Website = m.Website
            }).ToList();

            await context.Manufacturers.AddRangeAsync(manufacturers);
            await context.SaveChangesAsync();

            // Seed Brands - create them with proper manufacturer relationships
            var brands = new List<Brand>();
            for (int i = 0; i < seedData.Manufacturers.Count; i++)
            {
                var manufacturerData = seedData.Manufacturers[i];
                var manufacturer = manufacturers[i]; // Same index since we added them in order

                foreach (var brandData in manufacturerData.Brands)
                {
                    brands.Add(new Brand
                    {
                        Name = brandData.Name,
                        ManufacturerID = manufacturer.ManufacturerID,
                        CountryOfOrigin = Enum.Parse<Country>(brandData.CountryOfOrigin)
                    });
                }
            }

            await context.Brands.AddRangeAsync(brands);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to seed database: {ex.Message}", ex);
        }
    }

    private static async Task<SeedData> LoadSeedDataFromJsonAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SacksAIPlatform.DataLayer.Seeds.perfume-brands-data.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            var jsonContent = await reader.ReadToEndAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var seedData = JsonSerializer.Deserialize<SeedData>(jsonContent, options);
            
            if (seedData?.Manufacturers == null)
            {
                throw new InvalidOperationException("Invalid seed data format in JSON file");
            }

            return seedData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load seed data from JSON: {ex.Message}", ex);
        }
    }

    // Data transfer objects for JSON deserialization
    private class SeedData
    {
        public List<ManufacturerDto> Manufacturers { get; set; } = new();
    }

    private class ManufacturerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public List<BrandDto> Brands { get; set; } = new();
    }

    private class BrandDto
    {
        public string Name { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
    }
}
