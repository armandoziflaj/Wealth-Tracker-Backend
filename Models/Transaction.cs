using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WealthTracker.Models;

public class Transaction : CommonData
{
    [Required]
    [Column(TypeName = "decimal(18,4)")] 
    public decimal Amount { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime TransactionDate { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public long CategoryId { get; set; }
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = null!;
    
    [Required]
    public long UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public bool IsRecurring  { get; set; }
}


public enum TransactionType
{
    Expense = 0,
    Income = 1
}
