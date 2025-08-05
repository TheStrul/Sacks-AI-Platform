namespace SacksAIPlatform.InfrastructuresLayer.Csv.Models;

/// <summary>
/// Result of CSV parsing operation
/// </summary>
public class CsvParseResult
{
    public string[][] Rows { get; set; } = Array.Empty<string[]>();
    public string[] Headers { get; set; } = Array.Empty<string>();
    public int TotalRows { get; set; }
    public bool HasHeaders { get; set; }
    public List<CsvParseError> Errors { get; set; } = new();
}

/// <summary>
/// Error encountered during CSV parsing
/// </summary>
public class CsvParseError
{
    public int LineNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string RawLine { get; set; } = string.Empty;
}

/// <summary>
/// Result of CSV file validation
/// </summary>
public class CsvValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}
