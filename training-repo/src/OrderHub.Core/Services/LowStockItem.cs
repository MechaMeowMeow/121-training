namespace OrderHub.Core.Services;

/// <summary>
/// 低庫存警示頁的單筆結果：商品基本資訊加上「近 30 天售出數量（排除 Cancelled）」。
/// </summary>
public record LowStockItem(string Sku, string Name, int StockQuantity, int SoldLast30Days);
