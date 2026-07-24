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

    // 一次撈回多個商品（追蹤實體，供扣庫存／加回庫存後統一 SaveChanges）。避免逐列查詢造成 N+1。
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return Array.Empty<Product>();

        return await _db.Products.Where(p => idList.Contains(p.Id)).ToListAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
