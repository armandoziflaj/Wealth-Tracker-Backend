using System.ComponentModel.DataAnnotations;

namespace WealthTracker.Models;

public abstract class CommonData
{
    [Key]
    public long Id { get; set; }
    [Required]
    public DateTime CreatedOn { get; set; } =  DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public bool IsDeleted { get; set; } = false;
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public uint RowVersion { get; set; } 
}