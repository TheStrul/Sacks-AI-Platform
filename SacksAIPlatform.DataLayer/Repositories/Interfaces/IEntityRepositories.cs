using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.DataLayer.Repositories.Interfaces;

public interface IManufacturerRepository : IRepository<Manufacturer>
{
    Task<Manufacturer?> GetByNameAsync(string name);
}

public interface IBrandRepository : IRepository<Brand>
{
    Task<Brand?> GetByNameAsync(string name);
    Task<Brand?> FindByNameAsync(string name);
    Task<IEnumerable<Brand>> GetByManufacturerIdAsync(int manufacturerId);
}

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<Supplier?> GetByNameAsync(string name);
    Task<IEnumerable<Supplier>> GetByTypeAsync(string type);
    Task<IEnumerable<Supplier>> GetByCountryAsync(string country);
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByCodeAsync(string perfumeCode);
    Task<IEnumerable<Product>> GetByBrandIdAsync(int brandId);
    Task<IEnumerable<Product>> GetByNameAsync(string name);
    Task<IEnumerable<Product>> GetByConcentrationAsync(Concentration concentration);
    Task<IEnumerable<Product>> GetByGenderAsync(Gender gender);
}
