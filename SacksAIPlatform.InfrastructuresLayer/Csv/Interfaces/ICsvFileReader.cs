using SacksAIPlatform.InfrastructuresLayer.Csv.Models;

namespace SacksAIPlatform.InfrastructuresLayer.Csv.Interfaces;

/// <summary>
/// General CSV file reading and parsing interface
/// </summary>
public interface ICsvFileReader
{
    /// <summary>
    /// Reads all lines from a CSV file
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <returns>Array of CSV lines</returns>
    Task<string[]> ReadCsvFileAsync(string filePath);

    /// <summary>
    /// Reads CSV content from a stream
    /// </summary>
    /// <param name="stream">Stream containing CSV data</param>
    /// <returns>Array of CSV lines</returns>
    Task<string[]> ReadCsvStreamAsync(Stream stream);

    /// <summary>
    /// Parses a single CSV line handling quoted fields
    /// </summary>
    /// <param name="csvLine">CSV line to parse</param>
    /// <returns>Array of field values</returns>
    string[] ParseCsvLine(string csvLine);

    /// <summary>
    /// Parses multiple CSV lines
    /// </summary>
    /// <param name="lines">Array of CSV lines</param>
    /// <param name="skipHeader">Whether to skip the first line</param>
    /// <returns>Parsed CSV data</returns>
    CsvParseResult ParseCsvLines(string[] lines, bool skipHeader = true);

    /// <summary>
    /// Validates that a CSV file exists and is readable
    /// </summary>
    /// <param name="filePath">Path to validate</param>
    /// <returns>Validation result</returns>
    CsvValidationResult ValidateCsvFile(string filePath);

    /// <summary>
    /// Gets the field count for a CSV line
    /// </summary>
    /// <param name="csvLine">CSV line to analyze</param>
    /// <returns>Number of fields</returns>
    int GetFieldCount(string csvLine);

    /// <summary>
    /// Validates that all lines have consistent field counts
    /// </summary>
    /// <param name="lines">CSV lines to validate</param>
    /// <param name="expectedFields">Expected number of fields</param>
    /// <returns>Validation result</returns>
    CsvValidationResult ValidateFieldCount(string[] lines, int expectedFields);

    /// <summary>
    /// Extracts header row from CSV file
    /// </summary>
    /// <param name="filePath">Path to CSV file</param>
    /// <returns>Header fields</returns>
    Task<string[]> GetHeadersAsync(string filePath);

    /// <summary>
    /// Cleans a CSV field by removing quotes and trimming whitespace
    /// </summary>
    /// <param name="field">Field to clean</param>
    /// <returns>Cleaned field value</returns>
    string CleanField(string field);
}
