using System.ComponentModel.DataAnnotations;

namespace WealthTracker.Models;

public abstract class CommonData
{
    [Key]
    public long Id { get; set; }
    [Required]
    public DateTime CreatedOn { get; set; } =  DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    
    public uint RowVersion { get; set; } 
}