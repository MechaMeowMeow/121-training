using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
    }

    public Task<PagedResult<Order>> GetOrdersAsync(int page, int pageSize, OrderStatus? status)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        return _orderRepository.GetPagedAsync(page, pageSize, status);
    }

    public Task<Order?> GetOrderAsync(int id) => _orderRepository.GetWithDetailsAsync(id);

    public Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(int customerId) =>
        _orderRepository.GetByCustomerAsync(customerId);

    public async Task<ServiceResult<Order>> CreateOrderAsync(int customerId, IReadOnlyList<NewOrderLine> lines)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);

        var requestError = OrderValidator.ValidateRequest(customer, lines);
        if (requestError is not null)
            return ServiceResult<Order>.Fail(requestError);

        var order = new Order
        {
            CustomerId = customer!.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // 一次撈回所有明細的商品（明細已保證不重複），迴圈內以字典查表，避免逐列查詢的 N+1。
        var products = (await _productRepository.GetByIdsAsync(lines.Select(l => l.ProductId)))
            .ToDictionary(p => p.Id);

        var errors = new List<string>();
        foreach (var line in lines)
        {
            products.TryGetValue(line.ProductId, out var product);

            var lineError = OrderValidator.ValidateLine(product, line);
            if (lineError is not null)
            {
                errors.Add(lineError);
                continue;
            }

            product!.StockQuantity -= line.Quantity;

            // 單價快照存「原價」，會員折扣只在 CalculateTotal 對訂單總額折抵一次；
            // 若在此先打折，Gold 會員會被 CalculateTotal 再折一次（0.9 × 0.9）造成金額偏低。
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPriceSnapshot = product.UnitPrice
            });
        }

        if (errors.Count > 0)
            return ServiceResult<Order>.Fail(errors);

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        return ServiceResult<Order>.Ok(order);
    }

    public async Task<ServiceResult<Order>> CancelOrderAsync(int id)
    {
        var order = await _orderRepository.GetWithDetailsAsync(id);
        if (order is null)
            return ServiceResult<Order>.Fail("找不到指定的訂單");

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            return ServiceResult<Order>.Fail($"狀態為 {order.Status} 的訂單不可取消");

        // 上方已確認只有 Pending / Confirmed 可取消，兩者皆已在建單時扣過庫存，取消時一律加回。
        // 務必在改狀態「之前」加回庫存：若先設成 Cancelled，下方任何以狀態為條件的判斷都會失準。
        var products = (await _productRepository.GetByIdsAsync(order.Items.Select(i => i.ProductId)))
            .ToDictionary(p => p.Id);
        foreach (var item in order.Items)
        {
            if (products.TryGetValue(item.ProductId, out var product))
                product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;

        await _orderRepository.SaveChangesAsync();

        return ServiceResult<Order>.Ok(order);
    }

    public decimal GetDiscountRate(CustomerTier tier) => tier switch
    {
        CustomerTier.Gold => 0.10m,
        CustomerTier.Silver => 0.05m,
        _ => 0m
    };

    public decimal CalculateSubtotal(Order order) =>
        order.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);

    public decimal CalculateTotal(Order order)
    {
        var tier = order.Customer?.Tier ?? CustomerTier.Standard;
        var subtotal = CalculateSubtotal(order);
        return Math.Round(subtotal * (1 - GetDiscountRate(tier)), 2);
    }
}
