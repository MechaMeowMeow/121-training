using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

/// <summary>
/// 建立訂單的驗證規則，自 OrderService 抽出以集中維護、可單獨測試。
/// 每個方法回傳第一個不通過的訊息，全部通過則回 null。
/// </summary>
public static class OrderValidator
{
    // 請求層級：客戶存在、明細非空、數量 > 0、商品不重複（依序檢查，短路回傳）。
    public static string? ValidateRequest(Customer? customer, IReadOnlyList<NewOrderLine> lines)
    {
        if (customer is null)
            return "找不到指定的客戶";

        if (lines is null || lines.Count == 0)
            return "訂單至少需要一項商品";

        if (lines.Any(l => l.Quantity <= 0))
            return "商品數量必須大於 0";

        if (lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
            return "同一商品請勿重複加入，請調整數量即可";

        return null;
    }

    // 單列：商品需存在且販售中、庫存足夠。
    public static string? ValidateLine(Product? product, NewOrderLine line)
    {
        if (product is null || !product.IsActive)
            return $"商品（Id={line.ProductId}）不存在或已停售";

        if (product.StockQuantity < line.Quantity)
            return $"商品「{product.Name}」庫存不足（現有 {product.StockQuantity}，需求 {line.Quantity}）";

        return null;
    }
}
