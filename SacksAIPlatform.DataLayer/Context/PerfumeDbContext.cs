using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Entities;

namespace SacksAIPlatform.DataLayer.Context;

public class PerfumeDbContext : DbContext
{
    public PerfumeDbContext(DbContextOptions<PerfumeDbContext> options) : base(options)
    {
    }

    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Perfume> Perfumes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Manufacturer
        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.ManufacturerID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(500);
        });

        // Configure Brand
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            
            entity.HasOne(e => e.Manufacturer)
                  .WithMany(m => m.Brands)
                  .HasForeignKey(e => e.ManufacturerID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Supplier
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.ContactInfo).HasMaxLength(500);
        });

        // Configure Perfume
        modelBuilder.Entity<Perfume>(entity =>
        {
            entity.HasKey(e => e.PerfumeCode);
            entity.Property(e => e.PerfumeCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Concentration).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.Units).HasMaxLength(20);
            entity.Property(e => e.LilFree).HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.CountryOfOrigin).HasMaxLength(100);
            entity.Property(e => e.OriginalSource).HasMaxLength(500);
            
            entity.HasOne(e => e.Brand)
                  .WithMany(b => b.Perfumes)
                  .HasForeignKey(e => e.BrandID)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
