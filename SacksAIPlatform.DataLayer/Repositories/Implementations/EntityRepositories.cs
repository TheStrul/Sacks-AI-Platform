using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;

namespace SacksAIPlatform.DataLayer.Repositories.Implementations;

public class ManufacturerRepository : Repository<Manufacturer>, IManufacturerRepository
{
    public ManufacturerRepository(SacksDbContext context) : base(context)
    {
    }

    public async Task<Manufacturer?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Name == name);
    }

    public override async Task<IEnumerable<Manufacturer>> GetAllAsync()
    {
        return await _dbSet.Include(m => m.Brands).ToListAsync();
    }

    public override async Task<Manufacturer?> GetByIdAsync(object id)
    {
        return await _dbSet.Include(m => m.Brands)
                          .FirstOrDefaultAsync(m => m.ManufacturerID == (int)id);
    }
}

public class BrandRepository : Repository<Brand>, IBrandRepository
{
    public BrandRepository(SacksDbContext context) : base(context)
    {
    }

    public async Task<Brand?> GetByNameAsync(string name)
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .FirstOrDefaultAsync(b => b.Name == name);
    }
    
    public async Task<Brand?> FindByNameAsync(string name)
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .FirstOrDefaultAsync(b => b.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Brand>> GetByManufacturerIdAsync(int manufacturerId)
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .Where(b => b.ManufacturerID == manufacturerId)
                          .ToListAsync();
    }

    public override async Task<IEnumerable<Brand>> GetAllAsync()
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .Include(b => b.Products)
                          .ToListAsync();
    }

    public override async Task<Brand?> GetByIdAsync(object id)
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .Include(b => b.Products)
                          .FirstOrDefaultAsync(b => b.BrandID == (int)id);
    }
}

public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    public SupplierRepository(SacksDbContext context) : base(context)
    {
    }

    public async Task<Supplier?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<IEnumerable<Supplier>> GetByTypeAsync(string type)
    {
        return await _dbSet.Where(s => s.Type == type).ToListAsync();
    }

    public async Task<IEnumerable<Supplier>> GetByCountryAsync(string country)
    {
        return await _dbSet.Where(s => s.Country == country).ToListAsync();
    }
}

public class PerfumeRepository : Repository<Product>, IProductRepository
{
    public PerfumeRepository(SacksDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetByCodeAsync(string perfumeCode)
    {
        return await _dbSet.Include(p => p.Brand)
                          .ThenInclude(b => b.Manufacturer)
                          .FirstOrDefaultAsync(p => p.Code == perfumeCode);
    }

    public async Task<IEnumerable<Product>> GetByBrandIdAsync(int brandId)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.BrandID == brandId)
                          .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByNameAsync(string name)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.Name.Contains(name))
                          .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByConcentrationAsync(Concentration concentration)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.Concentration == concentration)
                          .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByGenderAsync(Gender gender)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.Gender == gender)
                          .ToListAsync();
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet.Include(p => p.Brand)
                          .ThenInclude(b => b.Manufacturer)
                          .ToListAsync();
    }

    public override async Task<Product?> GetByIdAsync(object id)
    {
        return await _dbSet.Include(p => p.Brand)
                          .ThenInclude(b => b.Manufacturer)
                          .FirstOrDefaultAsync(p => p.Code == (string)id);
    }
}
