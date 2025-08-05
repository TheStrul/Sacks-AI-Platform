using SacksAIPlatform.DataLayer.Entities;

namespace SacksAIPlatform.DataLayer.Csv.Interfaces;

public interface ICsvPerfumeConverter
{
    /// <summary>
    /// Converts CSV file to a list of Perfume entities
    /// </summary>
    /// <param name="csvFilePath">Path to the CSV file</param>
    /// <param name="skipFirstRow">Whether to skip the first row (headers)</param>
    /// <returns>List of converted Perfume entities with validation results</returns>
    Task<CsvConversionResult> ConvertCsvToPerfumesAsync(string csvFilePath, bool skipFirstRow = true);
}

public class CsvConversionResult
{
    public List<Perfume> ValidPerfumes { get; set; } = new();
    public List<CsvValidationError> ValidationErrors { get; set; } = new();
    public int TotalRecordsProcessed { get; set; }
    public int ValidRecordsCount => ValidPerfumes.Count;
    public int ErrorRecordsCount => ValidationErrors.Count;
}

public class CsvValidationError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RawCsvLine { get; set; } = string.Empty;
}
