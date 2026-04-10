using WealthTracker.Models;

namespace WealthTracker.Requests;

public class CategoryCreateDto
{
    public string Name { get; set; } = null!;
    public TransactionType Type { get; set; }
    public string? Color { get; set; }
}

public class CategoryUpdateDto : CategoryCreateDto 
{
    public long Id { get; set; }
}
public class CategoryDeleteDto
{
    public long Id { get; set; }
}