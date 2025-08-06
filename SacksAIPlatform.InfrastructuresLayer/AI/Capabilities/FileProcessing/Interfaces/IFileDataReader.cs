using System.Data;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models;

namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;

/// <summary>
/// Unified interface for reading data from CSV, XLS, and XLSX files
/// Uses ExcelDataReader internally to provide consistent access to file data
/// </summary>
public interface IFileDataReader
{
    /// <summary>
    /// Reads the first worksheet/table from any supported file format (CSV, XLS, XLSX)
    /// Auto-detects file format based on extension
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>FileData containing the first worksheet data</returns>
    Task<FileData> ReadFileAsync(string filePath);

    /// <summary>
    /// Reads the first worksheet/table from any supported file format (CSV, XLS, XLSX)
    /// Auto-detects file format based on extension - returns raw DataTable
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>DataTable containing the first worksheet data</returns>
    Task<DataTable> ReadFileAsDataTableAsync(string filePath);

    /// <summary>
    /// Reads the first worksheet/table from a stream
    /// </summary>
    /// <param name="stream">Stream containing file data</param>
    /// <param name="fileExtension">File extension to determine format (.csv, .xls, .xlsx)</param>
    /// <returns>FileData containing the first worksheet data</returns>
    Task<FileData> ReadStreamAsync(Stream stream, string fileExtension);

    /// <summary>
    /// Reads the first worksheet/table from a stream - returns raw DataTable
    /// </summary>
    /// <param name="stream">Stream containing file data</param>
    /// <param name="fileExtension">File extension to determine format (.csv, .xls, .xlsx)</param>
    /// <returns>DataTable containing the first worksheet data</returns>
    Task<DataTable> ReadStreamAsDataTableAsync(Stream stream, string fileExtension);

    /// <summary>
    /// Checks if a file exists and is readable
    /// </summary>
    /// <param name="filePath">Path to validate</param>
    /// <returns>True if file exists and is accessible</returns>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// Gets the supported file extensions
    /// </summary>
    /// <returns>Array of supported extensions</returns>
    string[] GetSupportedExtensions();

    /// <summary>
    /// Validates that a file has a supported extension
    /// </summary>
    /// <param name="filePath">Path to validate</param>
    /// <returns>True if extension is supported</returns>
    bool IsSupportedFile(string filePath);
}
