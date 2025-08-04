using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Entities;

namespace SacksAIPlatform.DataLayer.Seeds;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PerfumeDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (context.Manufacturers.Any())
        {
            return; // Database has been seeded
        }

        // Seed Manufacturers (Real Industry Conglomerates)
        var manufacturers = new List<Manufacturer>
        {
            new Manufacturer 
            { 
                Name = "Chanel S.A.", 
                Country = "France", 
                Website = "https://www.chanel.com" 
            },
            new Manufacturer 
            { 
                Name = "LVMH Moët Hennessy Louis Vuitton", 
                Country = "France", 
                Website = "https://www.lvmh.com" 
            },
            new Manufacturer 
            { 
                Name = "Estée Lauder Companies", 
                Country = "USA", 
                Website = "https://www.elcompanies.com" 
            },
            new Manufacturer 
            { 
                Name = "L'Oréal Luxe", 
                Country = "France", 
                Website = "https://www.loreal.com" 
            },
            new Manufacturer 
            { 
                Name = "Creed Boutique Ltd.", 
                Country = "United Kingdom", 
                Website = "https://www.creedboutique.com" 
            }
        };

        context.Manufacturers.AddRange(manufacturers);
        await context.SaveChangesAsync();

        // Seed Brands (Realistic Industry Relationships)
        var brands = new List<Brand>
        {
            // Chanel S.A. owns only Chanel
            new Brand { Name = "Chanel", ManufacturerID = manufacturers[0].ManufacturerID },
            
            // LVMH owns multiple luxury brands
            new Brand { Name = "Christian Dior", ManufacturerID = manufacturers[1].ManufacturerID },
            new Brand { Name = "Givenchy", ManufacturerID = manufacturers[1].ManufacturerID },
            new Brand { Name = "Kenzo", ManufacturerID = manufacturers[1].ManufacturerID },
            
            // Estée Lauder Companies owns Tom Ford
            new Brand { Name = "Tom Ford", ManufacturerID = manufacturers[2].ManufacturerID },
            
            // L'Oréal Luxe owns Armani brands
            new Brand { Name = "Giorgio Armani", ManufacturerID = manufacturers[3].ManufacturerID },
            new Brand { Name = "Emporio Armani", ManufacturerID = manufacturers[3].ManufacturerID },
            
            // Creed Boutique owns only Creed
            new Brand { Name = "Creed", ManufacturerID = manufacturers[4].ManufacturerID }
        };

        context.Brands.AddRange(brands);
        await context.SaveChangesAsync();
    }
}
