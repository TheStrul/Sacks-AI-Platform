using Microsoft.EntityFrameworkCore.Storage;
using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;

namespace SacksAIPlatform.DataLayer.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly PerfumeDbContext _context;
    private IDbContextTransaction? _transaction;
    
    private IManufacturerRepository? _manufacturerRepository;
    private IBrandRepository? _brandRepository;
    private ISupplierRepository? _supplierRepository;
    private IPerfumeRepository? _perfumeRepository;

    public UnitOfWork(PerfumeDbContext context)
    {
        _context = context;
    }

    public IManufacturerRepository Manufacturers =>
        _manufacturerRepository ??= new ManufacturerRepository(_context);

    public IBrandRepository Brands =>
        _brandRepository ??= new BrandRepository(_context);

    public ISupplierRepository Suppliers =>
        _supplierRepository ??= new SupplierRepository(_context);

    public IPerfumeRepository Perfumes =>
        _perfumeRepository ??= new PerfumeRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
