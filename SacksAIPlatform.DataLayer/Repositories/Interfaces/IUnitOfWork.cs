namespace SacksAIPlatform.DataLayer.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IManufacturerRepository Manufacturers { get; }
    IBrandRepository Brands { get; }
    ISupplierRepository Suppliers { get; }
    IPerfumeRepository Perfumes { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
