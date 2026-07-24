using OrderHub.Infrastructure.Repositories;

namespace OrderHub.Tests;

public class ProductRepositoryTests
{
    [Fact]
    public async Task GetByIdsAsync_ReturnsOnlyRequestedExistingProducts()
    {
        using var db = TestSetup.CreateContext();
        var repo = new ProductRepository(db);
        var p1 = TestSetup.AddProduct(db, sku: "SKU-1");
        var p2 = TestSetup.AddProduct(db, sku: "SKU-2");
        TestSetup.AddProduct(db, sku: "SKU-3"); // 未被要求，不應回傳

        var result = await repo.GetByIdsAsync(new[] { p1.Id, p2.Id, 99999 }); // 99999 不存在，忽略

        Assert.Equal(new[] { p1.Id, p2.Id }, result.Select(p => p.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task GetByIdsAsync_EmptyIds_ReturnsEmpty()
    {
        using var db = TestSetup.CreateContext();
        var repo = new ProductRepository(db);
        TestSetup.AddProduct(db);

        var result = await repo.GetByIdsAsync(Array.Empty<int>());

        Assert.Empty(result);
    }
}
