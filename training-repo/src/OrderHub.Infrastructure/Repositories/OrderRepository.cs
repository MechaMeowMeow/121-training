using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Ai;
using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderHubDbContext _db;

    public OrderRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Order>> GetPagedAsync(int page, int pageSize, OrderStatus? status)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            // page 為 1-based（controller 預設 1、View 分頁列從 1 起算），故第 1 頁不可略過任何資料。
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<Order>> SearchAsync(OrderSearchQuery query)
    {
        var q = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(o => o.Status == query.Status.Value);
        if (query.MemberTier.HasValue)
            q = q.Where(o => o.Customer != null && o.Customer.Tier == query.MemberTier.Value);
        if (query.DateFrom.HasValue)
            q = q.Where(o => o.CreatedAt >= query.DateFrom.Value.Date);
        if (query.DateTo.HasValue)
        {
            var endExclusive = query.DateTo.Value.Date.AddDays(1);   // 含當日
            q = q.Where(o => o.CreatedAt < endExclusive);
        }

        // 上限保險：就算條件很寬，也不把整張表倒出來
        return await q.OrderByDescending(o => o.CreatedAt).Take(100).ToListAsync();
    }

    public Task<Order?> GetWithDetailsAsync(int id) =>
        _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId) =>
        await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    // 近 N 天（since 之後）各商品售出數量，排除 Cancelled 訂單。單一 GROUP BY 聚合，無 N+1。
    public async Task<IReadOnlyDictionary<int, int>> GetSoldQuantitiesAsync(DateTime since, IEnumerable<int> productIds)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        var rows = await _db.OrderItems
            .Where(i => ids.Contains(i.ProductId)
                && i.Order!.Status != OrderStatus.Cancelled
                && i.Order.CreatedAt >= since)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
            .ToListAsync();

        return rows.ToDictionary(r => r.ProductId, r => r.Sold);
    }

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
