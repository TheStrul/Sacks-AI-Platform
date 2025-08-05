using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Csv.Models;

namespace SacksAIPlatform.DataLayer.Csv.Interfaces;

public interface IFiletoProductConverter
{
    /// <summary>
    /// Converts CSV file to a list of Product entities using flexible configuration
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="configuration">CSV parsing configuration, null uses default</param>
    /// <returns>List of converted Product entities with validation results</returns>
    Task<FileConversionResult> ConvertFileToProductsAsync(string filePath, FileConfiguration? configuration = null);
}

public class FileConversionResult
{
    public List<Product> ValidProducts { get; set; } = new();
    public List<FileValidationError> ValidationErrors { get; set; } = new();
    public int TotalRecordsProcessed { get; set; }
    public int ValidRecordsCount => ValidProducts.Count;
    public int ErrorRecordsCount => ValidationErrors.Count;
}

public class FileValidationError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RawLine { get; set; } = string.Empty;
}
