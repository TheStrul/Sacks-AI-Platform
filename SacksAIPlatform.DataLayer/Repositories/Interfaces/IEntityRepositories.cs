using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.DataLayer.Repositories.Interfaces;

public interface IManufacturerRepository : IRepository<Manufacturer>
{
    Task<Manufacturer?> GetByNameAsync(string name);
    Task<IEnumerable<Manufacturer>> GetByCountryAsync(string country);
}

public interface IBrandRepository : IRepository<Brand>
{
    Task<Brand?> GetByNameAsync(string name);
    Task<IEnumerable<Brand>> GetByManufacturerIdAsync(int manufacturerId);
}

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<Supplier?> GetByNameAsync(string name);
    Task<IEnumerable<Supplier>> GetByTypeAsync(string type);
    Task<IEnumerable<Supplier>> GetByCountryAsync(string country);
}

public interface IPerfumeRepository : IRepository<Perfume>
{
    Task<Perfume?> GetByCodeAsync(string perfumeCode);
    Task<IEnumerable<Perfume>> GetByBrandIdAsync(int brandId);
    Task<IEnumerable<Perfume>> GetByNameAsync(string name);
    Task<IEnumerable<Perfume>> GetByConcentrationAsync(Concentration concentration);
    Task<IEnumerable<Perfume>> GetByGenderAsync(Gender gender);
}
