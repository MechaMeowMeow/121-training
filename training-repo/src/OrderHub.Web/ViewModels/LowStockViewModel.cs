using System.ComponentModel.DataAnnotations;

namespace OrderHub.Web.ViewModels;

public class LowStockViewModel
{
    // 未帶查詢字串時維持預設 10；?threshold=0／負數會被 Range 擋下，顯示表單錯誤而非 500。
    [Range(1, 9999, ErrorMessage = "門檻必須大於 0")]
    [Display(Name = "庫存門檻")]
    public int Threshold { get; set; } = 10;

    public IReadOnlyList<LowStockRowViewModel> Items { get; set; } = Array.Empty<LowStockRowViewModel>();
}

public class LowStockRowViewModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int SoldLast30Days { get; set; }
}
