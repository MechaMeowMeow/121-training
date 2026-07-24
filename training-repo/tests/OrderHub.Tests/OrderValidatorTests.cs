using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderValidatorTests
{
    private static readonly Customer AnyCustomer = new() { Name = "客戶", Email = "c@example.com.tw" };

    [Fact]
    public void ValidateRequest_NullCustomer_ReturnsCustomerError()
    {
        var error = OrderValidator.ValidateRequest(null, new[] { new NewOrderLine(1, 1) });

        Assert.Equal("找不到指定的客戶", error);
    }

    [Fact]
    public void ValidateRequest_NullOrEmptyLines_ReturnsEmptyError()
    {
        Assert.Equal("訂單至少需要一項商品", OrderValidator.ValidateRequest(AnyCustomer, Array.Empty<NewOrderLine>()));
        Assert.Equal("訂單至少需要一項商品", OrderValidator.ValidateRequest(AnyCustomer, null!));
    }

    [Fact]
    public void ValidateRequest_NonPositiveQuantity_ReturnsQuantityError()
    {
        var error = OrderValidator.ValidateRequest(AnyCustomer, new[] { new NewOrderLine(1, 0) });

        Assert.Equal("商品數量必須大於 0", error);
    }

    [Fact]
    public void ValidateRequest_DuplicateProduct_ReturnsDuplicateError()
    {
        var error = OrderValidator.ValidateRequest(AnyCustomer, new[]
        {
            new NewOrderLine(1, 1),
            new NewOrderLine(1, 2)
        });

        Assert.Equal("同一商品請勿重複加入，請調整數量即可", error);
    }

    [Fact]
    public void ValidateRequest_ChecksCustomerBeforeLines()
    {
        // customer 為 null 且 lines 也為空時，應先回客戶錯誤（驗證有固定順序、短路）。
        var error = OrderValidator.ValidateRequest(null, Array.Empty<NewOrderLine>());

        Assert.Equal("找不到指定的客戶", error);
    }

    [Fact]
    public void ValidateRequest_Valid_ReturnsNull()
    {
        var error = OrderValidator.ValidateRequest(AnyCustomer, new[]
        {
            new NewOrderLine(1, 1),
            new NewOrderLine(2, 3)
        });

        Assert.Null(error);
    }

    [Fact]
    public void ValidateLine_MissingOrInactiveProduct_ReturnsNotFoundError()
    {
        var line = new NewOrderLine(7, 1);
        var inactive = new Product { Id = 7, Name = "停售品", StockQuantity = 100, IsActive = false };

        Assert.Equal("商品（Id=7）不存在或已停售", OrderValidator.ValidateLine(null, line));
        Assert.Equal("商品（Id=7）不存在或已停售", OrderValidator.ValidateLine(inactive, line));
    }

    [Fact]
    public void ValidateLine_InsufficientStock_ReturnsStockError()
    {
        var product = new Product { Id = 3, Name = "滑鼠", StockQuantity = 2, IsActive = true };

        var error = OrderValidator.ValidateLine(product, new NewOrderLine(3, 5));

        Assert.Equal("商品「滑鼠」庫存不足（現有 2，需求 5）", error);
    }

    [Fact]
    public void ValidateLine_Valid_ReturnsNull()
    {
        var product = new Product { Id = 3, Name = "滑鼠", StockQuantity = 10, IsActive = true };

        var error = OrderValidator.ValidateLine(product, new NewOrderLine(3, 10));

        Assert.Null(error);
    }
}
