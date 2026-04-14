namespace WealthTracker.FileIntegration;

public class ColumnDefinition
{
    public required string PropertyName { get; set; }
    public required string TargetType { get; set; }
    public string? Format { get; set; }
    public string? Expression { get; set; }
}