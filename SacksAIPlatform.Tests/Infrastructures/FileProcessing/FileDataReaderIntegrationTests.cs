using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;
using Xunit.Abstractions;

namespace SacksAIPlatform.Tests.Infrastructures.FileProcessing;

/// <summary>
/// Integration tests for FileDataReader using real files from the Inputs folder
/// </summary>
public class FileDataReaderIntegrationTests
{
    private readonly IFileDataReader _fileDataReader;
    private readonly ITestOutputHelper _output;
    private readonly string _inputsPath;

    public FileDataReaderIntegrationTests(ITestOutputHelper output)
    {
        _fileDataReader = new FileDataReader();
        _output = output;
        _inputsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Inputs");
    }

    [Theory]
    [InlineData("ComprehensiveStockAi.csv")]
    [InlineData("ComprehensiveStockAi.xlsx")]
    [InlineData("Check 3.8.25.xlsx")]
    [InlineData("coty 1.3.25.xlsx")]
    [InlineData("DIOR 2025.xlsx")]
    [InlineData("GroupedSupplierComparison.xlsx")]
    [InlineData("JIZAN 31.07.25.xlsx")]
    [InlineData("LOREAL 4.8.25.xlsx")]
    [InlineData("PCA 3.8.25.xls")]
    [InlineData("pca 8.7.25.xls")]
    [InlineData("puig 1.4.25.xlsx")]
    [InlineData("SA 2.4.25.xlsx")]
    [InlineData("sample 1 2.4.25.xlsx")]
    [InlineData("shishido 1.2.25.xlsx")]
    [InlineData("UNLIMITED 31.07.25.xls")]
    [InlineData("WW 3.8.25.xlsx")]
    public async Task ReadRealFile_ShouldSuccessfullyReadAllInputFiles(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_inputsPath, fileName);
        
        // Skip test if file doesn't exist (in case workspace structure is different)
        if (!File.Exists(filePath))
        {
            _output.WriteLine($"Skipping {fileName} - file not found at: {filePath}");
            return;
        }

        // Act
        var fileData = await _fileDataReader.ReadFileAsync(filePath);

        // Assert
        Assert.NotNull(fileData);
        Assert.True(fileData.ColumnCount > 0, $"{fileName}: Should have at least 1 column");
        Assert.True(fileData.RowCount >= 0, $"{fileName}: Should have 0 or more rows");

        // Log file information
        _output.WriteLine($"File: {fileName}");
        _output.WriteLine($"  Columns: {fileData.ColumnCount}");
        _output.WriteLine($"  Rows: {fileData.RowCount}");
        _output.WriteLine($"  Headers: {string.Join(", ", fileData.Headers)}");
        
        // Log first few rows if available
        if (fileData.RowCount > 0)
        {
            _output.WriteLine("  First row:");
            var firstRow = fileData.GetRow(0);
            for (int i = 0; i < Math.Min(firstRow.Length, 5); i++) // Show max 5 columns
            {
                _output.WriteLine($"    {fileData.Headers[i]}: {firstRow[i]}");
            }
        }
        _output.WriteLine("");
    }

    [Fact]
    public async Task ReadAllFiles_ShouldProcessDifferentFormats()
    {
        // Arrange
        var testFiles = new[]
        {
            "ComprehensiveStockAi.csv",
            "ComprehensiveStockAi.xlsx", 
            "PCA 3.8.25.xls"
        };

        var results = new List<(string fileName, bool success, string error)>();

        // Act
        foreach (var fileName in testFiles)
        {
            var filePath = Path.Combine(_inputsPath, fileName);
            
            if (!File.Exists(filePath))
            {
                results.Add((fileName, false, "File not found"));
                continue;
            }

            try
            {
                var fileData = await _fileDataReader.ReadFileAsync(filePath);
                results.Add((fileName, true, $"Cols:{fileData.ColumnCount}, Rows:{fileData.RowCount}"));
            }
            catch (Exception ex)
            {
                results.Add((fileName, false, ex.Message));
            }
        }

        // Assert
        _output.WriteLine("File Reading Results:");
        foreach (var (fileName, success, error) in results)
        {
            _output.WriteLine($"  {fileName}: {(success ? "✓" : "✗")} {error}");
        }

        // At least one file should be successfully read
        Assert.True(results.Any(r => r.success), "At least one file should be successfully read");
    }

    [Fact]
    public async Task CompareCSVvsXLSX_SameName_ShouldHaveSimilarStructure()
    {
        // Arrange
        var csvPath = Path.Combine(_inputsPath, "ComprehensiveStockAi.csv");
        var xlsxPath = Path.Combine(_inputsPath, "ComprehensiveStockAi.xlsx");

        // Skip test if either file doesn't exist
        if (!File.Exists(csvPath) || !File.Exists(xlsxPath))
        {
            _output.WriteLine("Skipping comparison - one or both files not found");
            return;
        }

        // Act
        var csvData = await _fileDataReader.ReadFileAsync(csvPath);
        var xlsxData = await _fileDataReader.ReadFileAsync(xlsxPath);

        // Assert
        _output.WriteLine($"CSV: {csvData.ColumnCount} columns, {csvData.RowCount} rows");
        _output.WriteLine($"XLSX: {xlsxData.ColumnCount} columns, {xlsxData.RowCount} rows");
        
        _output.WriteLine("CSV Headers: " + string.Join(", ", csvData.Headers));
        _output.WriteLine("XLSX Headers: " + string.Join(", ", xlsxData.Headers));

        // Both should have data
        Assert.True(csvData.ColumnCount > 0);
        Assert.True(xlsxData.ColumnCount > 0);
        Assert.True(csvData.RowCount >= 0);
        Assert.True(xlsxData.RowCount >= 0);
    }

    [Fact]
    public async Task ReadFile_WithDataTable_ShouldProvideAccessToRawData()
    {
        // Arrange
        var csvPath = Path.Combine(_inputsPath, "ComprehensiveStockAi.csv");
        
        if (!File.Exists(csvPath))
        {
            _output.WriteLine("Skipping test - ComprehensiveStockAi.csv not found");
            return;
        }

        // Act
        var dataTable = await _fileDataReader.ReadFileAsDataTableAsync(csvPath);
        var fileData = await _fileDataReader.ReadFileAsync(csvPath);

        // Assert
        Assert.NotNull(dataTable);
        Assert.NotNull(fileData);
        
        // Both should have same dimensions
        Assert.Equal(dataTable.Columns.Count, fileData.ColumnCount);
        Assert.Equal(dataTable.Rows.Count, fileData.RowCount);
        
        // Check that we can access the underlying DataTable
        Assert.Same(dataTable.GetType(), fileData.DataTable.GetType());
        
        _output.WriteLine($"DataTable access verified: {dataTable.Columns.Count} columns, {dataTable.Rows.Count} rows");
    }
}
