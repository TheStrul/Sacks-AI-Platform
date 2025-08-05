using SacksAIPlatform.InfrastructuresLayer.Csv.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Csv.Implementations;
using SacksAIPlatform.InfrastructuresLayer.Csv.Models;
using Xunit;

namespace SacksAIPlatform.Tests.InfrastructuresLayer.Csv;

/// <summary>
/// Tests for CsvFileReader functionality
/// </summary>
public class CsvFileReaderTests
{
    private readonly ICsvFileReader _csvFileReader;

    public CsvFileReaderTests()
    {
        _csvFileReader = new CsvFileReader();
    }

    [Fact]
    public void ParseCsvLine_SimpleFields_ShouldParseCorrectly()
    {
        // Arrange
        var csvLine = "Field1,Field2,Field3";

        // Act
        var result = _csvFileReader.ParseCsvLine(csvLine);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Field1", result[0]);
        Assert.Equal("Field2", result[1]);
        Assert.Equal("Field3", result[2]);
    }

    [Fact]
    public void ParseCsvLine_QuotedFields_ShouldParseCorrectly()
    {
        // Arrange
        var csvLine = "\"Field with, comma\",\"Simple Field\",\"Field with \"\"quotes\"\"\"";

        // Act
        var result = _csvFileReader.ParseCsvLine(csvLine);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Field with, comma", result[0]);
        Assert.Equal("Simple Field", result[1]);
        Assert.Equal("Field with \"quotes\"", result[2]);
    }

    [Fact]
    public void ParseCsvLine_EmptyFields_ShouldParseCorrectly()
    {
        // Arrange
        var csvLine = "Field1,,Field3";

        // Act
        var result = _csvFileReader.ParseCsvLine(csvLine);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Field1", result[0]);
        Assert.Equal("", result[1]);
        Assert.Equal("Field3", result[2]);
    }

    [Fact]
    public void ParseCsvLine_EmptyLine_ShouldReturnEmpty()
    {
        // Arrange
        var csvLine = "";

        // Act
        var result = _csvFileReader.ParseCsvLine(csvLine);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CleanField_QuotedField_ShouldRemoveQuotes()
    {
        // Arrange
        var field = "\"  Test Value  \"";

        // Act
        var result = _csvFileReader.CleanField(field);

        // Assert
        Assert.Equal("Test Value", result);
    }


    [Fact]
    public void CleanField_NullOrEmpty_ShouldReturnEmpty()
    {
        // Arrange & Act & Assert
        Assert.Equal("", _csvFileReader.CleanField(null));
        Assert.Equal("", _csvFileReader.CleanField(""));
        Assert.Equal("", _csvFileReader.CleanField("   "));
    }

    [Fact]
    public void GetFieldCount_ValidLine_ShouldReturnCorrectCount()
    {
        // Arrange
        var csvLine = "Field1,Field2,\"Field with, comma\",Field4";

        // Act
        var result = _csvFileReader.GetFieldCount(csvLine);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void ValidateCsvFile_NonExistentFile_ShouldReturnInvalid()
    {
        // Arrange
        var filePath = "non-existent-file.csv";

        // Act
        var result = _csvFileReader.ValidateCsvFile(filePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCsvFile_NullOrEmptyPath_ShouldReturnInvalid()
    {
        // Arrange & Act & Assert
        var result1 = _csvFileReader.ValidateCsvFile(null);
        Assert.False(result1.IsValid);
        Assert.Contains("cannot be null", result1.ErrorMessage);

        var result2 = _csvFileReader.ValidateCsvFile("");
        Assert.False(result2.IsValid);
        Assert.Contains("cannot be null", result2.ErrorMessage);

        var result3 = _csvFileReader.ValidateCsvFile("   ");
        Assert.False(result3.IsValid);
        Assert.Contains("cannot be null", result3.ErrorMessage);
    }

    [Fact]
    public async Task ReadCsvFileAsync_NonExistentFile_ShouldThrowException()
    {
        // Arrange
        var filePath = "non-existent-file.csv";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _csvFileReader.ReadCsvFileAsync(filePath));
    }

    [Fact]
    public async Task ParseCsvLines_WithHeader_ShouldParseCorrectly()
    {
        // Arrange
        var lines = new[]
        {
            "Header1,Header2,Header3",
            "Value1,Value2,Value3",
            "\"Value with, comma\",Value5,Value6"
        };

        // Act
        var result = _csvFileReader.ParseCsvLines(lines, skipHeader: true);

        // Assert
        Assert.True(result.HasHeaders);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(3, result.Headers.Length);
        Assert.Equal("Header1", result.Headers[0]);
        Assert.Equal(2, result.Rows.Length);
        Assert.Equal("Value1", result.Rows[0][0]);
        Assert.Equal("Value with, comma", result.Rows[1][0]);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ParseCsvLines_WithoutHeader_ShouldParseCorrectly()
    {
        // Arrange
        var lines = new[]
        {
            "Value1,Value2,Value3",
            "Value4,Value5,Value6"
        };

        // Act
        var result = _csvFileReader.ParseCsvLines(lines, skipHeader: false);

        // Assert
        Assert.False(result.HasHeaders);
        Assert.Equal(2, result.TotalRows);
        Assert.Empty(result.Headers);
        Assert.Equal(2, result.Rows.Length);
        Assert.Equal("Value1", result.Rows[0][0]);
        Assert.Equal("Value4", result.Rows[1][0]);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateFieldCount_ConsistentFields_ShouldBeValid()
    {
        // Arrange
        var lines = new[]
        {
            "Field1,Field2,Field3",
            "Value1,Value2,Value3",
            "Value4,Value5,Value6"
        };

        // Act
        var result = _csvFileReader.ValidateFieldCount(lines, 3);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ValidateFieldCount_InconsistentFields_ShouldHaveWarnings()
    {
        // Arrange
        var lines = new[]
        {
            "Field1,Field2,Field3",
            "Value1,Value2",
            "Value4,Value5,Value6,Value7"
        };

        // Act
        var result = _csvFileReader.ValidateFieldCount(lines, 3);

        // Assert
        Assert.True(result.IsValid); // Still valid but with warnings
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Line 2: Expected 3 fields, found 2", result.Warnings);
        Assert.Contains("Line 3: Expected 3 fields, found 4", result.Warnings);
    }
}
