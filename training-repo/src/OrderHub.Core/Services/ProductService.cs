using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(int threshold)
    {
        var products = await _productRepository.GetLowStockAsync(threshold);
        if (products.Count == 0)
            return Array.Empty<LowStockItem>();

        // 近 30 天售出數量：只用一條聚合查詢取回這批商品的銷量，避免逐一商品查（N+1）。
        var since = DateTime.UtcNow.AddDays(-30);
        var productIds = products.Select(p => p.Id).ToList();
        var soldQuantities = await _orderRepository.GetSoldQuantitiesAsync(since, productIds);

        return products
            .Select(p => new LowStockItem(
                p.Sku,
                p.Name,
                p.StockQuantity,
                soldQuantities.TryGetValue(p.Id, out var sold) ? sold : 0))
            .ToList();
    }
}
