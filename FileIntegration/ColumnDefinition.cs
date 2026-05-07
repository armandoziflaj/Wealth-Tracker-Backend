public class ColumnDefinition
{
    public required string PropertyName { get; set; }
    public required string TargetType { get; set; }
    public string? Format { get; set; }
    public string? Expression { get; set; }
    public string? LookUpEntity { get; set; }
    public string? LookUpKey { get; set; }
    public string? LookUpValue { get; set; } = "Id";
}

public class HeaderDefinition
{
    public required string Table { get; set; } 
    public string? Identification { get; set; }
    public string? FileNamePattern { get; set; }
    public bool HasHeaderRow { get; set; } = true;
    public short HeaderRow { get; set; }
    public short DataRow { get; set; }
    public char Separator { get; set; } = ';'; 
}