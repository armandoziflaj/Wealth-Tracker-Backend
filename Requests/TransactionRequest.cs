using System.ComponentModel.DataAnnotations;
using WealthTracker.Models;

namespace WealthTracker.Requests;

public class TransactionFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TransactionType? Type { get; set; }
    public long? CategoryId { get; set; }
    public string? SearchTerm { get; set; }
}

public class TransactionCreateDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public long CategoryId { get; set; }
    public string Notes {get; set;} = string.Empty;
    public bool IsRecurring { get; set; }
}

public class TransactionUpdateDto : TransactionCreateDto
{
    public long Id { get; set; }
}
