using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Implementations;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;
using System.Text;

namespace SacksAIPlatform.Tests.Infrastructures.FileProcessing;

/// <summary>
/// Tests for the unified FileDataReader that supports CSV, XLS, and XLSX files
/// </summary>
public class FileDataReaderTests
{
    private readonly IFileDataReader _fileDataReader;

    public FileDataReaderTests()
    {
        _fileDataReader = new FileDataReader();
    }

    [Fact]
    public void GetSupportedExtensions_ShouldReturnExpectedExtensions()
    {
        // Act
        var extensions = _fileDataReader.GetSupportedExtensions();

        // Assert
        Assert.NotNull(extensions);
        Assert.Contains(".csv", extensions);
        Assert.Contains(".xls", extensions);
        Assert.Contains(".xlsx", extensions);
        Assert.Contains(".xlsb", extensions);
    }

    [Theory]
    [InlineData("test.csv", true)]
    [InlineData("test.xls", true)]
    [InlineData("test.xlsx", true)]
    [InlineData("test.xlsb", true)]
    [InlineData("test.CSV", true)]  // Case insensitive
    [InlineData("test.XLS", true)]
    [InlineData("test.XLSX", true)]
    [InlineData("test.txt", false)]
    [InlineData("test.pdf", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSupportedFile_ShouldReturnCorrectValue(string? filePath, bool expected)
    {
        // Act
        var result = _fileDataReader.IsSupportedFile(filePath!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Act
        var result = await _fileDataReader.FileExistsAsync("non_existent_file.csv");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReadFileAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _fileDataReader.ReadFileAsync("non_existent_file.csv"));
    }

    [Fact]
    public async Task ReadFileAsync_WithUnsupportedExtension_ShouldThrowArgumentException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content");
        
        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _fileDataReader.ReadFileAsync(tempFile)); // .tmp extension is unsupported
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadStreamAsDataTableAsync_WithCsvData_ShouldReturnCorrectDataTable()
    {
        // Arrange
        var csvContent = "Name,Age,City\nJohn,25,New York\nJane,30,Los Angeles";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var dataTable = await _fileDataReader.ReadStreamAsDataTableAsync(stream, ".csv");

        // Assert
        Assert.NotNull(dataTable);
        Assert.Equal(3, dataTable.Columns.Count);
        Assert.Equal("Name", dataTable.Columns[0].ColumnName);
        Assert.Equal("Age", dataTable.Columns[1].ColumnName);
        Assert.Equal("City", dataTable.Columns[2].ColumnName);
        
        Assert.Equal(2, dataTable.Rows.Count);
        Assert.Equal("John", dataTable.Rows[0]["Name"]);
        Assert.Equal("25", dataTable.Rows[0]["Age"]);
        Assert.Equal("New York", dataTable.Rows[0]["City"]);
        
        Assert.Equal("Jane", dataTable.Rows[1]["Name"]);
        Assert.Equal("30", dataTable.Rows[1]["Age"]);
        Assert.Equal("Los Angeles", dataTable.Rows[1]["City"]);
    }

    [Fact]
    public async Task ReadStreamAsync_WithCsvData_ShouldReturnFileDataWrapper()
    {
        // Arrange
        var csvContent = "Product,Price,Stock\nLaptop,999.99,50\nMouse,19.99,100";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var fileData = await _fileDataReader.ReadStreamAsync(stream, ".csv");

        // Assert
        Assert.NotNull(fileData);
        Assert.Equal(3, fileData.ColumnCount);
        Assert.Equal(2, fileData.RowCount);
        
        var headers = fileData.Headers;
        Assert.Equal(3, headers.Length);
        Assert.Equal("Product", headers[0]);
        Assert.Equal("Price", headers[1]);
        Assert.Equal("Stock", headers[2]);
        
        Assert.Equal("Laptop", fileData.GetValue(0, "Product"));
        Assert.Equal("999.99", fileData.GetValue(0, "Price"));
        Assert.Equal("50", fileData.GetValue(0, "Stock"));
        
        Assert.Equal("Mouse", fileData.GetValue(1, 0));
        Assert.Equal("19.99", fileData.GetValue(1, 1));
        Assert.Equal("100", fileData.GetValue(1, 2));
    }

    [Fact]
    public async Task ReadStreamAsync_WithEmptyStream_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _fileDataReader.ReadStreamAsync(stream, ".csv"));
    }

    [Fact]
    public async Task ReadStreamAsDataTableAsync_WithUnsupportedExtension_ShouldThrowArgumentException()
    {
        // Arrange
        var content = "test content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _fileDataReader.ReadStreamAsDataTableAsync(stream, ".txt"));
    }

    [Fact]
    public async Task ReadStreamAsync_WithQuotedCsvData_ShouldHandleQuotesCorrectly()
    {
        // Arrange
        var csvContent = "Name,Description,Notes\n\"John Doe\",\"A person with \"\"quotes\"\"\",\"Simple note\"";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var fileData = await _fileDataReader.ReadStreamAsync(stream, ".csv");

        // Assert
        Assert.NotNull(fileData);
        Assert.Equal("John Doe", fileData.GetValue(0, "Name"));
        Assert.Equal("A person with \"quotes\"", fileData.GetValue(0, "Description"));
        Assert.Equal("Simple note", fileData.GetValue(0, "Notes"));
    }
}
