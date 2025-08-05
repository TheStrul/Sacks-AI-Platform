using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Entities;

namespace SacksAIPlatform.DataLayer.Context;

public class SacksDbContext : DbContext
{
    public SacksDbContext(DbContextOptions<SacksDbContext> options) : base(options)
    {
    }

    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Producs { get; set; }
    public DbSet<FileConfigurationHolder> FileConfigurationHolders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Manufacturer
        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.ManufacturerID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Website).HasMaxLength(500);
        });

        // Configure Brand
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandID);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CountryOfOrigin)
                  .HasConversion<string>()
                  .IsRequired();
            
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

        // Configure FileConfigurationHolder
        modelBuilder.Entity<FileConfigurationHolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FileNamePattern).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ConfigurationJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.FileConfigurations)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
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
                  .WithMany(b => b.Products)
                  .HasForeignKey(e => e.BrandID)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
