using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using System.Xml.Linq;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using WealthTracker.Models;
using WealthTracker.Requests;

namespace WealthTracker.Services;

public class FileService : IFileService
{
    private const string MetadataPath = "Metadata/Transactions.xml";
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;
    private readonly ApplicationDbContext _context;
    public FileService(HttpClient httpClient, IConfiguration config, ApplicationDbContext context)
    {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            _httpClient = httpClient;
            _context = context;
            var modelUri = config["AiSettings:ModelUri"];
            var apiKey = config["AiSettings:ApiKey"];

            _apiEndpoint = $"{modelUri}?key={apiKey}";
        }

        public async Task<object> ParseExcelFile(IFormFile file, long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileStream = file.OpenReadStream();

            var config = ResolveProvider(fileStream, file.FileName);

            if (config == null)
                throw new Exception(
                    "Protocol mismatch: Could not identify the statement provider signature or filename pattern.");

            if (fileStream.CanSeek) fileStream.Position = 0;

            var transactions = await ParseExcel<Transaction>(fileStream, config, file.FileName);
            var transactionList = transactions.ToList();

            foreach (var transaction in transactionList)
            {
                transaction.Amount = Math.Abs(transaction.Amount);
                transaction.UserId = userId;
            }

            _context.Transactions.AddRange(transactionList);
            await _context.SaveChangesAsync(cancellationToken);

            return new
            {
                count = transactionList.Count,
                provider = config.Table
            };
        }
        catch
        {
            throw new Exception("Failed to parse excel file.");
        }
        
    }

    public async Task<TransactionCreateDto> AiService(IFormFile file, long userId, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var fileBytes = ms.ToArray();
        var base64Image = Convert.ToBase64String(fileBytes);

        var requestBody = new 
        {
            contents = new[] {
                new {
                    parts = new object[] {
                        new { text = "Analyze this receipt. Return ONLY a JSON object with these EXACT keys: " +
                                     "amount (number), description (string, the merchant name), " +
                                     "transactionDate (string, ISO 8601 format)." },
                        new { inline_data = new { mime_type = file.ContentType, data = base64Image } }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, requestBody, cancellationToken);
        if ((int)response.StatusCode == 503)
        {
            await Task.Delay(2000, cancellationToken);
            response = await _httpClient.PostAsJsonAsync(_apiEndpoint, requestBody, cancellationToken);
        }
        response.EnsureSuccessStatusCode();
        var resultText = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(resultText);
        var aiText = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrEmpty(aiText)) throw new Exception("AI returned empty text");

        var cleanedJson = ExtractJson(aiText);
        return JsonSerializer.Deserialize<TransactionCreateDto>(cleanedJson, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        }) ?? throw new InvalidOperationException();
    }    
    private static string ExtractJson(string input)
    {
        var startIndex = input.IndexOf('{');
        var endIndex = input.LastIndexOf('}');

        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            return input.Substring(startIndex, endIndex - startIndex + 1);
        }

        throw new Exception("The AI failed to return a valid JSON structure.");
    }

    private HeaderDefinition? ResolveProvider(Stream fileStream, string fileName)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var allConfigs = LoadAllProviderConfigs(); 
        
        var byName = allConfigs.FirstOrDefault(c => 
            !string.IsNullOrEmpty(c.FileNamePattern) && 
            fileName.Contains(c.FileNamePattern, StringComparison.OrdinalIgnoreCase));
        if (byName != null) return byName;

        var csvConfig = new ExcelReaderConfiguration { 
            LeaveOpen = true, 
            AutodetectSeparators = [';', ',', '\t'] 
        };

        using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ExcelReaderFactory.CreateCsvReader(fileStream, csvConfig)
            : ExcelReaderFactory.CreateReader(fileStream, new ExcelReaderConfiguration { LeaveOpen = true });

        if (!reader.Read() || !reader.Read()) return null;

        var raw = reader.GetValue(0)?.ToString()?.Split(';')[0].Trim();
        return allConfigs.FirstOrDefault(c => c.Identification == raw);
    }
    
    private async Task<IEnumerable<T>> ParseExcel<T>(Stream stream,HeaderDefinition config,string fileName) where T : new()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        
        var list = new List<T>();
        var mappings = LoadColumnMappings(config.Table);

        var csvConfig = new ExcelReaderConfiguration { 
            LeaveOpen = true, 
            AutodetectSeparators = [config.Separator] 
        };

        using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ExcelReaderFactory.CreateCsvReader(stream, csvConfig)
            : ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration { LeaveOpen = true });
        
        if (config.HasHeaderRow)
        {
            for (var i = 1; i < config.HeaderRow; i++) reader.Read();
            reader.Read(); 
        }
        
        var headerMap = new Dictionary<string, int>();
        if (config.HasHeaderRow)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i)?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val)) headerMap[val] = i;
            }
        }
        else
        {
            for (var i = 0; i < Math.Min(mappings.Count, reader.FieldCount); i++)
            {
                if (!string.IsNullOrEmpty(mappings[i].PropertyName))
                    headerMap[mappings[i].PropertyName] = i;
            }
        }

        var rowsToSkip = config.HasHeaderRow
                                  ? (config.DataRow - config.HeaderRow - 1) 
                                  : (config.DataRow - 1);
        
        for (var i = 0; i < rowsToSkip; i++) reader.Read();
        
        while (reader.Read())
        {
            var entity = new T();
            var rowHasAnyData = false;
            var rowData = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i)?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val)) rowHasAnyData = true;
                
                var propName = headerMap.FirstOrDefault(x => x.Value == i).Key;
                if (propName != null) rowData[propName] = val ?? "0";
            }
            
            if (!rowHasAnyData) break;
            foreach (var mapping in mappings)
            {
                if (string.IsNullOrEmpty(mapping.PropertyName) && string.IsNullOrEmpty(mapping.Expression)) 
                    continue;
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
                    if (property == null) continue;
                    
                    if (!string.IsNullOrEmpty(mapping.LookUpEntity))
                    {
                        var resolvedId = await ResolveIdAsync(
                            mapping.LookUpEntity,
                            mapping.LookUpKey!,
                            mapping.LookUpValue ?? "Id",
                            valueToConvert
                        );

                        if (resolvedId != null)
                        {
                            // Set the foreign key (e.g., CategoryId)
                            property.SetValue(entity, resolvedId);
                        }
                    }
                    else
                    {
                        // Standard conversion for amounts, dates, etc.
                        var convertedValue = ConvertValue(valueToConvert, mapping.TargetType, mapping.Format);
                        property.SetValue(entity, convertedValue);
                    }
                }
            }
            if (!rowHasAnyData) break;
            list.Add(entity);
        }

        return list;
    }

    
    private static XDocument GetMetadataDocument()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MetadataPath);
        return XDocument.Load(filePath);
    }

    private List<HeaderDefinition> LoadAllProviderConfigs()
    {
        var metadata = GetMetadataDocument();
        return metadata.Descendants("Provider").Select(p => new HeaderDefinition {
            Table = p.Attribute("name")?.Value ?? "Unknown",
            Identification = p.Element("Identification")?.Value,
            FileNamePattern = p.Element("FileNamePattern")?.Value,
            HasHeaderRow = bool.Parse(p.Element("HasHeaderRow")?.Value ?? "true"),
            HeaderRow = short.Parse(p.Element("HeaderRow")?.Value ?? "1"),
            DataRow = short.Parse(p.Element("DataRow")?.Value ?? "2"),
            Separator = (p.Element("Separator")?.Value ?? ";")[0]
        }).ToList();
    }

    private List<ColumnDefinition> LoadColumnMappings(string providerName)
    {
        var metadata = GetMetadataDocument();
        var provider = metadata.Descendants("Provider").FirstOrDefault(p => p.Attribute("name")?.Value == providerName);
        
        return provider?.Descendants("Column").Select(c => new ColumnDefinition {
            PropertyName = c.Element("PropertyName")?.Value ?? "",
            TargetType = c.Element("TargetType")?.Value ?? "string",
            Format = c.Element("Format")?.Value,
            Expression = c.Element("Expression")?.Value,
            LookUpEntity = c.Element("LookUpEntity")?.Value,
            LookUpKey = c.Element("LookUpKey")?.Value,
            LookUpValue =  c.Element("LookUpValue")?.Value
        }).ToList() ?? new List<ColumnDefinition>();
    }


    private object? ConvertValue(string? value, string targetType, string? format)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            return targetType.ToLower() switch
            {
                "datetime" => ParseDateTimeSafe(value, format),

                "decimal" => ParseDecimalSafe(value),
                "bool" => Convert.ToBoolean(value),
                "int" => Convert.ToInt32(value),
                "long" => long.Parse(value),
                _ => value
            };
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ParseDateTimeSafe(string value, string? format)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        DateTime parsedDate;
        try
        {
            var dateTimeFormat = string.IsNullOrEmpty(format) ? "yyyy-MM-dd HH:mm:ss" : format;
            parsedDate = DateTime.ParseExact(value, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }
        catch
        {
            if (!DateTime.TryParse(value, out parsedDate)) 
                return null;
        }
        return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
    }
    
    private static decimal ParseDecimalSafe(string value)
    {
        string normalized = value.Trim();
        
        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            normalized = normalized.Replace(',', '.');
        }
        else if (normalized.Contains(',') && normalized.Contains('.'))
        {
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.') 
                       ? normalized.Replace(".", "").Replace(',', '.') 
                       : normalized.Replace(",", "");
        }

        return decimal.Parse(normalized, CultureInfo.InvariantCulture);
    }
    private string? EvaluateSimpleExpression(string expression, Dictionary<string, string> rowData)
    {
        foreach (var kvp in rowData)
        {
            expression = expression.Replace($"[{kvp.Key}]", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        if (!expression.Contains('?')) return null; //EvaluateMath(expression);
        
        var parts = expression.Split(['?', ':'], StringSplitOptions.TrimEntries);
        var condition = parts[0];
        
        var isTrue = EvaluateCondition(condition);
        return isTrue ? parts[1] : parts[2];

    }

    private static bool EvaluateCondition(string cond)
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

    private async Task<object?> ResolveIdAsync(
        string entityName,
        string key,
        string valueField,
        string? inputValue)
    {
        if (string.IsNullOrWhiteSpace(inputValue))
            return null;

        var entityType = Type.GetType($"WealthTracker.Models.{entityName}");
        if (entityType == null)
            throw new ArgumentException($"Entity '{entityName}' not found.");

        var efEntity = _context.Model.FindEntityType(entityType)
                       ?? throw new ArgumentException($"Entity '{entityName}' not in DbContext.");

        var property = efEntity.FindProperty(key)
                       ?? throw new ArgumentException($"Property '{key}' not found.");

        var propertyType = property.ClrType;

        object typedValue = propertyType == typeof(Guid)
            ? Guid.Parse(inputValue)
            : Convert.ChangeType(inputValue, propertyType);

        var set = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType)
            .Invoke(_context, null)!;

        var query = EntityFrameworkQueryableExtensions.AsNoTracking((dynamic)set);

        var parameter = Expression.Parameter(entityType, "e");

        var propertyAccess = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [propertyType],
            parameter,
            Expression.Constant(key)
        );

        var body = Expression.Equal(
            propertyAccess,
            Expression.Constant(typedValue, propertyType)
        );

        var lambda = Expression.Lambda(body, parameter);

        var entity = await EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(query, (dynamic)lambda);

        return entity?.GetType()
            .GetProperty(valueField)?
            .GetValue(entity);
    }
}