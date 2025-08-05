using SacksAIPlatform.InfrastructuresLayer.Csv.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Csv.Implementations;
using SacksAIPlatform.InfrastructuresLayer.Csv.Models;
using Xunit;

namespace SacksAIPlatform.Tests.Infrastructures.Csv;

/// <summary>
/// Tests for CsvFileReader using real CSV data files
/// </summary>
public class CsvDataInputTests
{
    private readonly ICsvFileReader _csvFileReader;
    private readonly string _dataInputsPath;

    public CsvDataInputTests()
    {
        _csvFileReader = new CsvFileReader();
        _dataInputsPath = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructures", "DataInputs");
    }

    [Fact]
    public async Task ReadCsvFileAsync_Test1Csv_ShouldReadSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test1.csv");

        // Act
        var result = await _csvFileReader.ReadCsvFileAsync(filePath);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Length); // Two data rows
        
        // Verify content structure
        Assert.Contains("085715169969", result[0]);
        Assert.Contains("Abercrombie & Fitch", result[0]);
        Assert.Contains("ABERCROMBIE AND FITCH AWAY WEEKEND WOMEN EDP SPRAY", result[0]);
    }

    [Fact]
    public async Task ReadCsvFileAsync_Test2Csv_ShouldHandleQuotesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test2.csv");

        // Act
        var result = await _csvFileReader.ReadCsvFileAsync(filePath);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Length); // Two data rows
        
        // Verify that quoted content is present
        Assert.Contains("\"AWAY\"", result[0]);
        Assert.Contains("'WOMEN'", result[0]);
    }

    [Fact]
    public void ParseCsvLine_Test1FirstRow_ShouldParseAllFields()
    {
        // Arrange
        var csvLine = "FALSE,085715169969,Abercrombie & Fitch,ABERCROMBIE AND FITCH AWAY WEEKEND WOMEN EDP SPRAY,50ml,SP,Eau de Parfum,W,,Contains,,";

        // Act
        var fields = _csvFileReader.ParseCsvLine(csvLine);

        // Assert
        Assert.Equal(12, fields.Length);
        Assert.Equal("FALSE", fields[0]);
        Assert.Equal("085715169969", fields[1]);
        Assert.Equal("Abercrombie & Fitch", fields[2]);
        Assert.Equal("ABERCROMBIE AND FITCH AWAY WEEKEND WOMEN EDP SPRAY", fields[3]);
        Assert.Equal("50ml", fields[4]);
        Assert.Equal("SP", fields[5]);
        Assert.Equal("Eau de Parfum", fields[6]);
        Assert.Equal("W", fields[7]);
        Assert.Equal("", fields[8]); // Empty field
        Assert.Equal("Contains", fields[9]);
        Assert.Equal("", fields[10]); // Empty field
        Assert.Equal("", fields[11]); // Empty field
    }

    [Fact]
    public void ParseCsvLine_Test2FirstRowWithQuotes_ShouldHandleQuotesCorrectly()
    {
        // Arrange
        var csvLine = "FALSE,085715169969,Abercrombie & Fitch,ABERCROMBIE AND FITCH \"AWAY\" WEEKEND 'WOMEN' EDP SPRAY,50ml,SP,Eau de Parfum,W,,Contains,,";

        // Act
        var fields = _csvFileReader.ParseCsvLine(csvLine);

        // Assert
        Assert.Equal(12, fields.Length);
        Assert.Equal("FALSE", fields[0]);
        Assert.Equal("085715169969", fields[1]);
        Assert.Equal("Abercrombie & Fitch", fields[2]);
        Assert.Equal("ABERCROMBIE AND FITCH AWAY WEEKEND 'WOMEN' EDP SPRAY", fields[3]);
        Assert.Equal("50ml", fields[4]);
        Assert.Equal("SP", fields[5]);
        Assert.Equal("Eau de Parfum", fields[6]);
        Assert.Equal("W", fields[7]);
    }

    [Fact]
    public async Task ParseCsvLines_Test1Complete_ShouldParseAllRows()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test1.csv");
        var lines = await _csvFileReader.ReadCsvFileAsync(filePath);

        // Act
        var result = _csvFileReader.ParseCsvLines(lines, skipHeader: false);

        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.Rows.Length);
        Assert.False(result.HasHeaders);
        Assert.Empty(result.Errors);

        // Verify first row data
        Assert.Equal("FALSE", result.Rows[0][0]);
        Assert.Equal("085715169969", result.Rows[0][1]);
        Assert.Equal("Abercrombie & Fitch", result.Rows[0][2]);

        // Verify second row data
        Assert.Equal("FALSE", result.Rows[1][0]);
        Assert.Equal("085715163134", result.Rows[1][1]);
        Assert.Equal("ABERCROMBIE AND FITCH FIRST INSTINCT MEN EDT SPRAY", result.Rows[1][3]);
    }

    [Fact]
    public async Task ParseCsvLines_Test2Complete_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test2.csv");
        var lines = await _csvFileReader.ReadCsvFileAsync(filePath);

        // Act
        var result = _csvFileReader.ParseCsvLines(lines, skipHeader: false);

        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.Rows.Length);
        Assert.Empty(result.Errors);

        // Verify special characters are preserved/handled correctly
        var productName = result.Rows[0][3];
        Assert.Contains("AWAY", productName);      // Quotes should be removed
        Assert.Contains("'WOMEN'", productName);   // Single quotes should be preserved
    }

    [Fact]
    public async Task ValidateCsvFile_BothTestFiles_ShouldBeValid()
    {
        // Arrange
        var test1Path = Path.Combine(_dataInputsPath, "Test1.csv");
        var test2Path = Path.Combine(_dataInputsPath, "Test2.csv");

        // Act
        var result1 = _csvFileReader.ValidateCsvFile(test1Path);
        var result2 = _csvFileReader.ValidateCsvFile(test2Path);

        // Assert
        Assert.True(result1.IsValid, $"Test1.csv should be valid: {result1.ErrorMessage}");
        Assert.True(result2.IsValid, $"Test2.csv should be valid: {result2.ErrorMessage}");
    }

    [Fact]
    public async Task GetFieldCount_BothTestFiles_ShouldHaveConsistentFieldCount()
    {
        // Arrange
        var test1Path = Path.Combine(_dataInputsPath, "Test1.csv");
        var test2Path = Path.Combine(_dataInputsPath, "Test2.csv");
        
        var lines1 = await _csvFileReader.ReadCsvFileAsync(test1Path);
        var lines2 = await _csvFileReader.ReadCsvFileAsync(test2Path);

        // Act
        var fieldCount1Row1 = _csvFileReader.GetFieldCount(lines1[0]);
        var fieldCount1Row2 = _csvFileReader.GetFieldCount(lines1[1]);
        var fieldCount2Row1 = _csvFileReader.GetFieldCount(lines2[0]);
        var fieldCount2Row2 = _csvFileReader.GetFieldCount(lines2[1]);

        // Assert
        Assert.Equal(12, fieldCount1Row1);
        Assert.Equal(12, fieldCount1Row2);
        Assert.Equal(12, fieldCount2Row1);
        Assert.Equal(12, fieldCount2Row2);

        // All files should have consistent field counts
        Assert.Equal(fieldCount1Row1, fieldCount1Row2);
        Assert.Equal(fieldCount2Row1, fieldCount2Row2);
        Assert.Equal(fieldCount1Row1, fieldCount2Row1);
    }

    [Fact]
    public async Task ValidateFieldCount_BothFiles_ShouldPassValidation()
    {
        // Arrange
        var test1Path = Path.Combine(_dataInputsPath, "Test1.csv");
        var test2Path = Path.Combine(_dataInputsPath, "Test2.csv");
        
        var lines1 = await _csvFileReader.ReadCsvFileAsync(test1Path);
        var lines2 = await _csvFileReader.ReadCsvFileAsync(test2Path);

        // Act
        var validation1 = _csvFileReader.ValidateFieldCount(lines1, 12);
        var validation2 = _csvFileReader.ValidateFieldCount(lines2, 12);

        // Assert
        Assert.True(validation1.IsValid);
        Assert.Empty(validation1.Warnings);
        
        Assert.True(validation2.IsValid);
        Assert.Empty(validation2.Warnings);
    }

    [Fact]
    public void CleanField_RealDataSamples_ShouldCleanCorrectly()
    {
        // Test cleaning real field values from our test data
        
        // Test normal field
        var result1 = _csvFileReader.CleanField("Abercrombie & Fitch");
        Assert.Equal("Abercrombie & Fitch", result1);

        // Test field with extra spaces
        var result2 = _csvFileReader.CleanField("  50ml  ");
        Assert.Equal("50ml", result2);

        // Test empty field
        var result3 = _csvFileReader.CleanField("");
        Assert.Equal("", result3);

        // Test field that might have quotes
        var result4 = _csvFileReader.CleanField("\"SP\"");
        Assert.Equal("SP", result4);
    }

    [Fact]
    public async Task EndToEndTest_ProcessBothFiles_ShouldWorkCompletelyE2E()
    {
        // This is an end-to-end test that simulates real usage
        
        // Arrange
        var test1Path = Path.Combine(_dataInputsPath, "Test1.csv");
        var test2Path = Path.Combine(_dataInputsPath, "Test2.csv");

        // Act & Assert - Test1.csv
        Assert.True(_csvFileReader.ValidateCsvFile(test1Path).IsValid);
        var lines1 = await _csvFileReader.ReadCsvFileAsync(test1Path);
        var parsed1 = _csvFileReader.ParseCsvLines(lines1, skipHeader: false);
        
        Assert.Equal(2, parsed1.Rows.Length);
        Assert.Empty(parsed1.Errors);

        // Act & Assert - Test2.csv
        Assert.True(_csvFileReader.ValidateCsvFile(test2Path).IsValid);
        var lines2 = await _csvFileReader.ReadCsvFileAsync(test2Path);
        var parsed2 = _csvFileReader.ParseCsvLines(lines2, skipHeader: false);
        
        Assert.Equal(2, parsed2.Rows.Length);
        Assert.Empty(parsed2.Errors);

        // Verify we can extract meaningful data
        foreach (var row in parsed1.Rows)
        {
            Assert.Equal(12, row.Length);
            Assert.NotEmpty(_csvFileReader.CleanField(row[1])); // UPC should not be empty
            Assert.NotEmpty(_csvFileReader.CleanField(row[2])); // Brand should not be empty
            Assert.NotEmpty(_csvFileReader.CleanField(row[3])); // Product name should not be empty
        }

        foreach (var row in parsed2.Rows)
        {
            Assert.Equal(12, row.Length);
            Assert.NotEmpty(_csvFileReader.CleanField(row[1])); // UPC should not be empty
            Assert.NotEmpty(_csvFileReader.CleanField(row[2])); // Brand should not be empty
            Assert.NotEmpty(_csvFileReader.CleanField(row[3])); // Product name should not be empty
        }
    }
}
