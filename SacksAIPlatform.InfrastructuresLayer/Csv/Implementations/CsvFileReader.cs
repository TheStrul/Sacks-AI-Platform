using SacksAIPlatform.InfrastructuresLayer.Csv.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Csv.Models;
using System.Text;

namespace SacksAIPlatform.InfrastructuresLayer.Csv.Implementations;

/// <summary>
/// General CSV file reading and parsing implementation
/// </summary>
public class CsvFileReader : ICsvFileReader
{
    public async Task<string[]> ReadCsvFileAsync(string filePath)
    {
        var validation = ValidateCsvFile(filePath);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"CSV file validation failed: {validation.ErrorMessage}");
        }

        return await File.ReadAllLinesAsync(filePath);
    }

    public async Task<string[]> ReadCsvStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                lines.Add(line);
            }
        }
        
        return lines.ToArray();
    }

    public string[] ParseCsvLine(string csvLine)
    {
        if (string.IsNullOrEmpty(csvLine))
        {
            return Array.Empty<string>();
        }

        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < csvLine.Length; i++)
        {
            char c = csvLine[i];
            
            if (c == '"')
            {
                // Handle escaped quotes (double quotes)
                if (i + 1 < csvLine.Length && csvLine[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip the next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    public CsvParseResult ParseCsvLines(string[] lines, bool skipHeader = true)
    {
        var result = new CsvParseResult
        {
            HasHeaders = skipHeader,
            TotalRows = lines.Length
        };

        var rows = new List<string[]>();
        var errors = new List<CsvParseError>();

        // Extract headers if needed
        if (skipHeader && lines.Length > 0)
        {
            try
            {
                result.Headers = ParseCsvLine(lines[0]);
            }
            catch (Exception ex)
            {
                errors.Add(new CsvParseError
                {
                    LineNumber = 1,
                    ErrorMessage = $"Failed to parse header: {ex.Message}",
                    RawLine = lines[0]
                });
            }
        }

        // Parse data rows
        var startIndex = skipHeader ? 1 : 0;
        for (int i = startIndex; i < lines.Length; i++)
        {
            try
            {
                var fields = ParseCsvLine(lines[i]);
                rows.Add(fields);
            }
            catch (Exception ex)
            {
                errors.Add(new CsvParseError
                {
                    LineNumber = i + 1,
                    ErrorMessage = $"Failed to parse line: {ex.Message}",
                    RawLine = lines[i]
                });
            }
        }

        result.Rows = rows.ToArray();
        result.Errors = errors;
        
        return result;
    }

    public CsvValidationResult ValidateCsvFile(string filePath)
    {
        var result = new CsvValidationResult();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            result.IsValid = false;
            result.ErrorMessage = "File path cannot be null or empty";
            return result;
        }

        if (!File.Exists(filePath))
        {
            result.IsValid = false;
            result.ErrorMessage = $"File does not exist: {filePath}";
            return result;
        }

        try
        {
            // Check if file is readable
            using var fs = File.OpenRead(filePath);
            result.IsValid = true;
        }
        catch (UnauthorizedAccessException)
        {
            result.IsValid = false;
            result.ErrorMessage = "Access denied to the file";
        }
        catch (IOException ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"IO error accessing file: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Unexpected error accessing file: {ex.Message}";
        }

        return result;
    }

    public int GetFieldCount(string csvLine)
    {
        return ParseCsvLine(csvLine).Length;
    }

    public CsvValidationResult ValidateFieldCount(string[] lines, int expectedFields)
    {
        var result = new CsvValidationResult { IsValid = true };
        var warnings = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var fieldCount = GetFieldCount(lines[i]);
            if (fieldCount != expectedFields)
            {
                warnings.Add($"Line {i + 1}: Expected {expectedFields} fields, found {fieldCount}");
            }
        }

        if (warnings.Count > 0)
        {
            result.Warnings = warnings;
            // Consider it valid but with warnings
        }

        return result;
    }

    public async Task<string[]> GetHeadersAsync(string filePath)
    {
        var lines = await ReadCsvFileAsync(filePath);
        if (lines.Length == 0)
        {
            return Array.Empty<string>();
        }

        return ParseCsvLine(lines[0]);
    }

    public string CleanField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // First trim whitespace, then remove surrounding quotes, then handle escaped quotes
        return field
            .Trim() // Remove leading/trailing whitespace
            .Trim('"') // Remove surrounding quotes
            .Replace("\"\"", "\"") // Handle escaped quotes (convert "" to ")
            .Trim(); // Final trim after processing
    }
}
