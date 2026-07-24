using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderServiceCancelTests
{
    private static async Task<Order> CreateOrderWithStatusAsync(
        Core.Services.OrderService service,
        Infrastructure.Data.OrderHubDbContext db,
        OrderStatus status)
    {
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);
        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });
        var order = result.Value!;
        order.Status = status;
        await db.SaveChangesAsync();
        return order;
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public async Task CancelOrder_ActiveOrder_SetsStatusCancelled(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, db.Orders.Single(o => o.Id == order.Id).Status);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CancelOrder_NotCancellableStatus_Fails(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.False(result.Success);
        Assert.Equal(initialStatus, db.Orders.Single(o => o.Id == order.Id).Status);
    }

    [Fact]
    public async Task CancelOrder_RestoresProductStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 50);

        var created = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 8) });
        Assert.True(created.Success);
        Assert.Equal(42, db.Products.Single(p => p.Id == product.Id).StockQuantity); // 建單先扣 8

        var cancel = await service.CancelOrderAsync(created.Value!.Id);

        Assert.True(cancel.Success);
        Assert.Equal(50, db.Products.Single(p => p.Id == product.Id).StockQuantity); // 取消後應加回原庫存
    }

    [Fact]
    public async Task CancelOrder_MultipleProducts_RestoresEachProductStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var p1 = TestSetup.AddProduct(db, stock: 10, sku: "SKU-CC1");
        var p2 = TestSetup.AddProduct(db, stock: 20, sku: "SKU-CC2");
        var created = await service.CreateOrderAsync(customer.Id, new[]
        {
            new NewOrderLine(p1.Id, 3),
            new NewOrderLine(p2.Id, 5)
        });
        Assert.True(created.Success);

        var cancel = await service.CancelOrderAsync(created.Value!.Id);

        Assert.True(cancel.Success);
        Assert.Equal(10, db.Products.Single(p => p.Id == p1.Id).StockQuantity);
        Assert.Equal(20, db.Products.Single(p => p.Id == p2.Id).StockQuantity);
    }

    [Fact]
    public async Task CancelOrder_NotFound_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var result = await service.CancelOrderAsync(12345);

        Assert.False(result.Success);
        Assert.Contains("找不到", result.ErrorMessage);
    }
}
