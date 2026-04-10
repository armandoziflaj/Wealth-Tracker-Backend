using WealthTracker.Models;

namespace WealthTracker.Responses;

public class CategoriesResponses
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; } 
    public string? Color { get; set; } 
    public decimal TransactionTotal { get; set; } = decimal.MinValue;
    public long UserId { get; set; }
}