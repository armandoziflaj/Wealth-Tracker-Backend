using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WealthTracker.Models;

public class Transaction : CommonDataWithUser
{
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Required] [StringLength(200)] public string Description { get; set; } = string.Empty;

    [Required] public DateTime TransactionDate { get; set; }

    [Required] public TransactionType Type { get; set; }

    public long? CategoryId { get; set; }
    [ForeignKey("CategoryId")] 
    public virtual Category Category { get; set; } = null!;

    [MaxLength(1000)] 
    public string? Notes { get; set; }

    public bool IsRecurring { get; set; }

    public RecursionType? RecursionTime { get; set; }

    public DateTime? NextOccurrence { get; set; } 

    public long? ParentTransactionId { get; set; }

    [ForeignKey(nameof(ParentTransactionId))]
    public virtual Transaction? ParentTransaction { get; set; }
    
    public void SetupNextOccurrence()
    {
        if (IsRecurring && RecursionTime.HasValue)
        {
            NextOccurrence = CalculateNextDate(TransactionDate, RecursionTime.Value);
        }
    }

    private DateTime CalculateNextDate(DateTime start, RecursionType interval) =>
        interval switch
        {
            RecursionType.Daily      => start.AddDays(1),
            RecursionType.Weekly     => start.AddDays(7),
            RecursionType.Monthly    => start.AddMonths(1),
            RecursionType.Quarterly  => start.AddMonths(3),
            RecursionType.SemiAnnual => start.AddMonths(6),
            RecursionType.Yearly     => start.AddYears(1),
            _                        => start
        };
}

public enum TransactionType
{
    Expense = 0,
    Income = 1
}

public enum RecursionType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    SemiAnnual = 4,
    Yearly = 5
}
