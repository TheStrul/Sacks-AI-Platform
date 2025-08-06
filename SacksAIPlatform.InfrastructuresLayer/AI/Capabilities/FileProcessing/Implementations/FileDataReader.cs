using ExcelDataReader;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models;
using System.Data;
using System.Text;

namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing.Implementations;

/// <summary>
/// Unified file data reader for CSV, XLS, and XLSX files using ExcelDataReader
/// Provides consistent access to file data regardless of format and serves as an agent capability
/// </summary>
public class FileDataReader : AgentCapability, IFileDataReader
{
    private static readonly string[] SupportedExtensions = { ".csv", ".xls", ".xlsx", ".xlsb" };

    public FileDataReader()
    {
        Name = "File Data Reader";
        Description = "Reads and processes data from Excel files (CSV, XLS, XLSX, XLSB) and converts them to structured data tables for analysis and processing.";
        Examples = new List<string>
        {
            "Read product data from Excel file",
            "Import supplier information from CSV",
            "Process inventory data from XLSX file",
            "Load customer data from XLS spreadsheet"
        };
        Available = true;
    }

    static FileDataReader()
    {
        // Register encoding provider for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<FileData> ReadFileAsync(string filePath)
    {
        var dataTable = await ReadFileAsDataTableAsync(filePath);
        return new FileData(dataTable);
    }

    public async Task<DataTable> ReadFileAsDataTableAsync(string filePath)
    {
        if (!await FileExistsAsync(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        if (!IsSupportedFile(filePath))
        {
            throw new ArgumentException($"Unsupported file format. Supported extensions: {string.Join(", ", SupportedExtensions)}");
        }

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return await ReadStreamAsDataTableAsync(stream, extension);
    }

    public async Task<FileData> ReadStreamAsync(Stream stream, string fileExtension)
    {
        var dataTable = await ReadStreamAsDataTableAsync(stream, fileExtension);
        return new FileData(dataTable);
    }

    public async Task<DataTable> ReadStreamAsDataTableAsync(Stream stream, string fileExtension)
    {
        return await Task.Run(() =>
        {
            fileExtension = fileExtension.ToLowerInvariant();
            
            IExcelDataReader reader = fileExtension switch
            {
                ".csv" => ExcelReaderFactory.CreateCsvReader(stream),
                ".xls" or ".xlsx" or ".xlsb" => ExcelReaderFactory.CreateReader(stream),
                _ => throw new ArgumentException($"Unsupported file extension: {fileExtension}")
            };

            using (reader)
            {
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true // Treat first row as headers
                    }
                });

                // Return the first table (worksheet)
                if (dataSet.Tables.Count == 0)
                {
                    throw new InvalidOperationException("File contains no data tables");
                }

                return dataSet.Tables[0];
            }
        });
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.FromResult(File.Exists(filePath));
    }

    public string[] GetSupportedExtensions()
    {
        return SupportedExtensions.ToArray();
    }

    public bool IsSupportedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }
}
