using Microsoft.EntityFrameworkCore;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;

namespace SacksAIPlatform.DataLayer.Repositories.Implementations;

public class ManufacturerRepository : Repository<Manufacturer>, IManufacturerRepository
{
    public ManufacturerRepository(PerfumeDbContext context) : base(context)
    {
    }

    public async Task<Manufacturer?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Name == name);
    }

    public async Task<IEnumerable<Manufacturer>> GetByCountryAsync(string country)
    {
        return await _dbSet.Where(m => m.Country == country).ToListAsync();
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
    public BrandRepository(PerfumeDbContext context) : base(context)
    {
    }

    public async Task<Brand?> GetByNameAsync(string name)
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .FirstOrDefaultAsync(b => b.Name == name);
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
                          .Include(b => b.Perfumes)
                          .ToListAsync();
    }

    public override async Task<Brand?> GetByIdAsync(object id)
    {
        return await _dbSet.Include(b => b.Manufacturer)
                          .Include(b => b.Perfumes)
                          .FirstOrDefaultAsync(b => b.BrandID == (int)id);
    }
}

public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    public SupplierRepository(PerfumeDbContext context) : base(context)
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

public class PerfumeRepository : Repository<Perfume>, IPerfumeRepository
{
    public PerfumeRepository(PerfumeDbContext context) : base(context)
    {
    }

    public async Task<Perfume?> GetByCodeAsync(string perfumeCode)
    {
        return await _dbSet.Include(p => p.Brand)
                          .ThenInclude(b => b.Manufacturer)
                          .FirstOrDefaultAsync(p => p.PerfumeCode == perfumeCode);
    }

    public async Task<IEnumerable<Perfume>> GetByBrandIdAsync(int brandId)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.BrandID == brandId)
                          .ToListAsync();
    }

    public async Task<IEnumerable<Perfume>> GetByNameAsync(string name)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.Name.Contains(name))
                          .ToListAsync();
    }

    public async Task<IEnumerable<Perfume>> GetByConcentrationAsync(string concentration)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.Concentration == concentration)
                          .ToListAsync();
    }

    public async Task<IEnumerable<Perfume>> GetByGenderAsync(string gender)
    {
        return await _dbSet.Include(p => p.Brand)
                          .Where(p => p.Gender == gender)
                          .ToListAsync();
    }

    public override async Task<IEnumerable<Perfume>> GetAllAsync()
    {
        return await _dbSet.Include(p => p.Brand)
                          .ThenInclude(b => b.Manufacturer)
                          .ToListAsync();
    }

    public override async Task<Perfume?> GetByIdAsync(object id)
    {
        return await _dbSet.Include(p => p.Brand)
                          .ThenInclude(b => b.Manufacturer)
                          .FirstOrDefaultAsync(p => p.PerfumeCode == (string)id);
    }
}
