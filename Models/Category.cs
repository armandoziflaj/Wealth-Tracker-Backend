using System.ComponentModel.DataAnnotations.Schema;

namespace WealthTracker.Models;

using System.ComponentModel.DataAnnotations;

[Table("Categories")]
public class Category : CommonData
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TransactionType Type { get; set; } 
    
    [MaxLength(100)]
    public string? Color { get; set; } 

    public long UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    public ICollection<Transaction> Transactions { get; set; } = [];
}