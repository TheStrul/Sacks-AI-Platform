namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models;

/// <summary>
/// Represents a single cell from any supported file format (CSV, XLS, XLSX)
/// Simplified model for unified file processing
/// </summary>
public class FileCell
{
    /// <summary>
    /// Zero-based column index
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Column name/header (if available)
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Cell value as string
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Original data type from the file
    /// </summary>
    public Type DataType { get; set; } = typeof(string);

    /// <summary>
    /// Zero-based row index
    /// </summary>
    public int RowIndex { get; set; }

    public override string ToString()
    {
        return $"[{RowIndex},{ColumnIndex}] {ColumnName}: {Value}";
    }
}
