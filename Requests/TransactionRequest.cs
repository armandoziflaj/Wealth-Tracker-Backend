using WealthTracker.Models;

namespace WealthTracker.Requests;

public class TransactionFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TransactionType? Type { get; set; }
    public long? CategoryId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? SearchTerm { get; set; }
}