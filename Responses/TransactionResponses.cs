using WealthTracker.Models;

namespace WealthTracker.Responses;

public class TransactionResponseDto 
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public long? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public RecursionType? RecursionTime { get; set; }
}

public class MonthlySummaryDto
{
    public string Name { get; set; } = null!;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
}

public class CategorySummaryDto
{
    public string Name { get; set; } = null!;
    public decimal Value { get; set; }
    public string Fill { get; set; } = null!;
}
public class DashboardDataDto 
{
    public required IEnumerable<CategorySummaryDto> ProtocolFlow { get; set; } 
    public required IEnumerable<CategorySummaryDto> CategoryExpenseAllocation { get; set; } 
    public required IEnumerable<CategorySummaryDto> CategoryIncomeAllocation { get; set; } 
}