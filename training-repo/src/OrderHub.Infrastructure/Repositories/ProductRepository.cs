using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    // 只含販售中、庫存嚴格小於 threshold 者，依庫存量升冪（採購最急的排最前）。
    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold) =>
        await _db.Products
            .Where(p => p.IsActive && p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
