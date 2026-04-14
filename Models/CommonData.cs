using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
public abstract class CommonDataWithUser : CommonData
{
    [Required]
    public long UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}