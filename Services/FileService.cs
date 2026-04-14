using System.Globalization;
using System.Xml.Linq;
using ExcelDataReader;
using WealthTracker.FileIntegration;
using WealthTracker.Models;

namespace WealthTracker.Services;

public class FileService(ApplicationDbContext context) : IFileService
{
    private const string MetadataPath = "Metadata/Transactions.xml";

    public async Task<object> ParseExcelFile(IFormFile file, long userId, CancellationToken cancellationToken = default)
    {
        var fileStream = file.OpenReadStream();
        var provider = ResolveProviderName(fileStream);

        if (provider == "Unknown")
        {
            throw new Exception("Protocol mismatch: Could not identify the statement provider signature.");
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var transactions = await ParseExcel<Transaction>(fileStream, provider);
        var transactionList = transactions.ToList();
        
        foreach (var transaction in transactionList)
        {
            transaction.UserId = userId; 
        }
        
        context.Transactions.AddRange(transactionList);
        await context.SaveChangesAsync(cancellationToken);
        return new 
        { 
            count = transactionList.Count, 
            provider = provider ??  "Unknown"
        };
    }

    private string ResolveProviderName(Stream fileStream)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        
        using var reader = ExcelReaderFactory.CreateReader(fileStream, new ExcelReaderConfiguration() 
        { 
            LeaveOpen = true 
        });
        
        reader.Read(); 
        reader.Read();
        
        var indicatorValue = reader.GetValue(0)?.ToString()?.Trim(); 
        if (string.IsNullOrEmpty(indicatorValue)) return "Unknown";

        var metadata = GetMetadataDocument();
        var provider = metadata.Descendants("Provider")
            .FirstOrDefault(p => p.Element("Identification")?.Value == indicatorValue);

        return provider?.Attribute("name")?.Value ?? "Unknown";
    }

    private Task<IEnumerable<T>> ParseExcel<T>(Stream fileStream, string providerName) where T : new()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        
        var list = new List<T>();
        var metadata = GetMetadataDocument();
        var providerElement = metadata.Descendants("Provider")
            .FirstOrDefault(p => p.Attribute("name")?.Value == providerName);

        if (providerElement == null) throw new Exception($"Provider protocol '{providerName}' not found in metadata.");
        
        var dataRowIndex = int.Parse(providerElement.Element("DataRow")?.Value ?? "1");
        var headerRowIndex = int.Parse(providerElement.Element("HeaderRow")?.Value ?? (dataRowIndex - 1).ToString());
        
        var columnMappings = providerElement.Descendants("Column").Select(c => new ColumnDefinition {
            PropertyName = c.Element("PropertyName")?.Value ?? string.Empty,
            TargetType = c.Element("TargetType")?.Value ?? string.Empty,
            Format = c.Element("Format")?.Value,
            Expression = c.Element("Expression")?.Value
        }).ToList();

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        
        for (var i = 1; i < headerRowIndex; i++) reader.Read();
        reader.Read();
        
        var headerMap = new Dictionary<string, int>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var val = reader.GetValue(i).ToString()?.Trim();
            if (!string.IsNullOrEmpty(val)) headerMap[val] = i;
        }
        var rowsToSkip = dataRowIndex - headerRowIndex - 1;
        for (var i = 0; i < rowsToSkip; i++) reader.Read();
        
        while (reader.Read())
        {
            var entity = new T();
            var rowHasAnyData = false;
            var rowData = new Dictionary<string, string>();
            foreach (var header in headerMap)
            {
                var rawVal = reader.GetValue(header.Value)?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(rawVal))
                {
                    rowHasAnyData = true;
                }
                rowData[header.Key] = rawVal ?? "0";
            }
            if (!rowHasAnyData) break;
            foreach (var mapping in columnMappings)
            {
                string? valueToConvert = null;

                if (!string.IsNullOrEmpty(mapping.Expression))
                {
                    valueToConvert = EvaluateSimpleExpression(mapping.Expression, rowData);
                }
                else if (headerMap.TryGetValue(mapping.PropertyName, out int colIndex))
                {
                    valueToConvert = reader.GetValue(colIndex)?.ToString()?.Trim(); 
                }

                if (!string.IsNullOrWhiteSpace(valueToConvert))
                {
                    rowHasAnyData = true;
                    var property = typeof(T).GetProperty(mapping.PropertyName);
                    if (property == null) 
                        continue;
                    var convertedValue = ConvertValue(valueToConvert, mapping.TargetType, mapping.Format);
                    property.SetValue(entity, convertedValue);
                }
            }
            if (!rowHasAnyData) break;
            list.Add(entity);
        }

        return Task.FromResult<IEnumerable<T>>(list);
    }

    private XDocument GetMetadataDocument()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MetadataPath);
        return XDocument.Load(filePath);
    }

    private object? ConvertValue(string? value, string targetType, string? format)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            return targetType.ToLower() switch
            {
                "datetime" => DateTime.SpecifyKind(
                    !string.IsNullOrEmpty(format) 
                        ? DateTime.ParseExact(value, format, CultureInfo.InvariantCulture) 
                        : Convert.ToDateTime(value), 
                    DateTimeKind.Utc),

                "decimal" => ParseDecimalSafe(value),
                "bool" => Convert.ToBoolean(value),
                "int" => Convert.ToInt32(value),
                _ => value
            };
        }
        catch
        {
            return null;
        }
    }
    private decimal ParseDecimalSafe(string value)
    {
        string normalized = value.Trim();
        
        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            normalized = normalized.Replace(',', '.');
        }
        else if (normalized.Contains(',') && normalized.Contains('.'))
        {
            if (normalized.LastIndexOf(',') > normalized.LastIndexOf('.'))
            {
                normalized = normalized.Replace(".", "").Replace(',', '.');
            }
            else
            {
                normalized = normalized.Replace(",", "");
            }
        }

        return decimal.Parse(normalized, CultureInfo.InvariantCulture);
    }
    private string? EvaluateSimpleExpression(string expression, Dictionary<string, string> rowData)
    {
        foreach (var kvp in rowData)
        {
            expression = expression.Replace($"[{kvp.Key}]", kvp.Value ?? "0", StringComparison.OrdinalIgnoreCase);
        }

        if (!expression.Contains('?')) return null; //EvaluateMath(expression);
        
        var parts = expression.Split(['?', ':'], StringSplitOptions.TrimEntries);
        var condition = parts[0];
        
        var isTrue = EvaluateCondition(condition);
        return isTrue ? parts[1] : parts[2];

    }

    private bool EvaluateCondition(string cond)
    {
        if (cond.Contains('<')) {
            var p = cond.Split('<');
            return decimal.Parse(p[0]) < decimal.Parse(p[1]);
        }
        if (cond.Contains('>')) {
            var p = cond.Split('>');
            return decimal.Parse(p[0]) > decimal.Parse(p[1]);
        }
        return false;
    }
}