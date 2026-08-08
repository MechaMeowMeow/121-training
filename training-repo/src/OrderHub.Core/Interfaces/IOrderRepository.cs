using OrderHub.Core.Ai;
using OrderHub.Core.Common;
using OrderHub.Core.Domain;

namespace OrderHub.Core.Interfaces;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetPagedAsync(int page, int pageSize, OrderStatus? status);
    Task<IReadOnlyList<Order>> SearchAsync(OrderSearchQuery query);
    Task<Order?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId);
    Task<IReadOnlyDictionary<int, int>> GetSoldQuantitiesAsync(DateTime since, IEnumerable<int> productIds);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}
