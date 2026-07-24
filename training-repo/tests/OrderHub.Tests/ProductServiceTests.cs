using OrderHub.Core.Domain;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndActive_SortedByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var low1 = TestSetup.AddProduct(db, stock: 3, sku: "SKU-LOW1");
        var low2 = TestSetup.AddProduct(db, stock: 9, sku: "SKU-LOW2");
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-EQ");          // 剛好等於門檻 → 排除（驗證是 < 非 <=）
        TestSetup.AddProduct(db, stock: 15, sku: "SKU-HIGH");        // 高於門檻 → 排除
        TestSetup.AddProduct(db, stock: 2, isActive: false, sku: "SKU-INACT"); // 低庫存但停售 → 排除

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { low1.Sku, low2.Sku }, result.Select(r => r.Sku)); // 升冪：3, 9
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var product = TestSetup.AddProduct(db, stock: 3);
        AddOrderWithItem(db, product.Id, quantity: 5, status: OrderStatus.Shipped, createdAt: DateTime.UtcNow.AddDays(-1));
        AddOrderWithItem(db, product.Id, quantity: 4, status: OrderStatus.Cancelled, createdAt: DateTime.UtcNow.AddDays(-1));

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(5, Assert.Single(result).SoldLast30Days); // 只計非取消的 5，不含取消的 4
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesOrdersOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var product = TestSetup.AddProduct(db, stock: 3);
        AddOrderWithItem(db, product.Id, quantity: 7, status: OrderStatus.Shipped, createdAt: DateTime.UtcNow.AddDays(-10));
        AddOrderWithItem(db, product.Id, quantity: 100, status: OrderStatus.Shipped, createdAt: DateTime.UtcNow.AddDays(-31));

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(7, Assert.Single(result).SoldLast30Days); // 只計 30 天內的 7，不含 31 天前的 100
    }

    private static void AddOrderWithItem(OrderHubDbContext db, int productId, int quantity, OrderStatus status, DateTime createdAt)
    {
        var customer = TestSetup.AddCustomer(db);
        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = status,
            CreatedAt = createdAt,
            Items = { new OrderItem { ProductId = productId, Quantity = quantity, UnitPriceSnapshot = 100m } }
        });
        db.SaveChanges();
    }
}
