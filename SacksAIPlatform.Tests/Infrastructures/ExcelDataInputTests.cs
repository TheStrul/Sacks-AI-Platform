using SacksAIPlatform.InfrastructuresLayer.Excel.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Excel.Implementations;
using SacksAIPlatform.InfrastructuresLayer.Excel.Models;
using Xunit;

namespace SacksAIPlatform.Tests.Infrastructures.Excel;

/// <summary>
/// Tests for ExcelFileHandler using real Excel data files
/// </summary>
public class ExcelDataInputTests
{
    private readonly IExcelFileHandler _excelFileHandler;
    private readonly string _dataInputsPath;

    public ExcelDataInputTests()
    {
        _excelFileHandler = new ExcelFileHandler();
        _dataInputsPath = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructures", "DataInputs");
    }

    [Fact]
    public async Task FileExistsAsync_Test3Excel_ShouldReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");

        // Act
        var exists = await _excelFileHandler.FileExistsAsync(filePath);

        // Assert
        Assert.True(exists, $"Excel file should exist: {filePath}");
    }

    [Fact]
    public async Task FileExistsAsync_Test5Excel_ShouldReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");

        // Act
        var exists = await _excelFileHandler.FileExistsAsync(filePath);

        // Assert
        Assert.True(exists, $"Excel file should exist: {filePath}");
    }

    [Fact]
    public async Task FileExistsAsync_NonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "NonExistent.xlsx");

        // Act
        var exists = await _excelFileHandler.FileExistsAsync(filePath);

        // Assert
        Assert.False(exists, "Non-existent file should return false");
    }

    [Fact]
    public async Task GetWorksheetNamesAsync_Test3Excel_ShouldReturnWorksheetNames()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");

        // Act
        var worksheetNames = await _excelFileHandler.GetWorksheetNamesAsync(filePath);

        // Assert
        Assert.NotEmpty(worksheetNames);
        Assert.All(worksheetNames, name => Assert.NotEmpty(name));
        
        // Log worksheet names for debugging
        foreach (var name in worksheetNames)
        {
            Assert.NotNull(name);
            Assert.NotEmpty(name.Trim());
        }
    }

    [Fact]
    public async Task GetWorksheetNamesAsync_Test5Excel_ShouldReturnWorksheetNames()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");

        // Act
        var worksheetNames = await _excelFileHandler.GetWorksheetNamesAsync(filePath);

        // Assert
        Assert.NotEmpty(worksheetNames);
        Assert.All(worksheetNames, name => Assert.NotEmpty(name));
    }

    [Fact]
    public async Task ReadFileAsync_Test3Excel_ShouldReadCompleteWorkbook()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");

        // Act
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);

        // Assert
        Assert.NotNull(workbook);
        Assert.Equal(filePath, workbook.FilePath);
        Assert.NotEmpty(workbook.Worksheets);
        
        // Verify each worksheet has structure
        foreach (var worksheet in workbook.Worksheets)
        {
            Assert.NotNull(worksheet);
            Assert.NotEmpty(worksheet.Name);
            Assert.NotNull(worksheet.Rows);
            Assert.NotNull(worksheet.Headers);
        }
    }

    [Fact]
    public async Task ReadFileAsync_Test5Excel_ShouldReadCompleteWorkbook()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");

        // Act
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);

        // Assert
        Assert.NotNull(workbook);
        Assert.Equal(filePath, workbook.FilePath);
        Assert.NotEmpty(workbook.Worksheets);
        
        // Verify workbook structure
        Assert.True(workbook.Worksheets.Count > 0);
        
        // Check first worksheet structure
        var firstWorksheet = workbook.Worksheets[0];
        Assert.NotNull(firstWorksheet);
        Assert.NotEmpty(firstWorksheet.Name);
    }

    [Fact]
    public async Task ReadWorksheetAsync_ByIndex_ShouldReadFirstWorksheet()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");

        // Act
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, 0);

        // Assert
        Assert.NotNull(worksheet);
        Assert.NotEmpty(worksheet.Name);
        Assert.NotNull(worksheet.Rows);
        Assert.NotNull(worksheet.Headers);
        
        // Verify worksheet has data structure
        if (worksheet.Rows.Count > 0)
        {
            var firstRow = worksheet.Rows[0];
            Assert.NotNull(firstRow);
            Assert.True(firstRow.RowNumber >= 1);
            Assert.NotNull(firstRow.Cells);
        }
    }

    [Fact]
    public async Task ReadWorksheetAsync_ByName_ShouldReadSpecificWorksheet()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var worksheetNames = await _excelFileHandler.GetWorksheetNamesAsync(filePath);
        var firstWorksheetName = worksheetNames[0];

        // Act
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, firstWorksheetName);

        // Assert
        Assert.NotNull(worksheet);
        Assert.Equal(firstWorksheetName, worksheet.Name);
        Assert.NotNull(worksheet.Rows);
        Assert.NotNull(worksheet.Headers);
    }

    [Fact]
    public async Task ReadWorksheetAsync_InvalidIndex_ShouldThrowException()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _excelFileHandler.ReadWorksheetAsync(filePath, 999);
        });
    }

    [Fact]
    public async Task ReadWorksheetAsync_InvalidName_ShouldThrowException()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var invalidWorksheetName = "NonExistentWorksheet";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _excelFileHandler.ReadWorksheetAsync(filePath, invalidWorksheetName);
        });
    }

    [Fact]
    public async Task ReadFileAsync_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "NonExistent.xlsx");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _excelFileHandler.ReadFileAsync(filePath);
        });
    }

    [Fact]
    public async Task GetWorksheetNamesAsync_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "NonExistent.xlsx");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _excelFileHandler.GetWorksheetNamesAsync(filePath);
        });
    }

    [Fact]
    public async Task WorkbookGetWorksheet_ByIndex_ShouldReturnCorrectWorksheet()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);

        // Act
        var worksheet = workbook.GetWorksheet(0);

        // Assert
        Assert.NotNull(worksheet);
        Assert.Equal(workbook.Worksheets[0].Name, worksheet.Name);
    }

    [Fact]
    public async Task WorkbookGetWorksheet_ByName_ShouldReturnCorrectWorksheet()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);
        var firstWorksheetName = workbook.Worksheets[0].Name;

        // Act
        var worksheet = workbook.GetWorksheet(firstWorksheetName);

        // Assert
        Assert.NotNull(worksheet);
        Assert.Equal(firstWorksheetName, worksheet.Name);
    }

    [Fact]
    public async Task WorkbookGetWorksheet_InvalidIndex_ShouldReturnNull()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);

        // Act
        var worksheet = workbook.GetWorksheet(999);

        // Assert
        Assert.Null(worksheet);
    }

    [Fact]
    public async Task WorkbookGetWorksheet_InvalidName_ShouldReturnNull()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);

        // Act
        var worksheet = workbook.GetWorksheet("NonExistentWorksheet");

        // Assert
        Assert.Null(worksheet);
    }

    [Fact]
    public async Task WorksheetGetRow_ValidRowNumber_ShouldReturnRow()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, 0);

        // Act - Try to get first row (should be header row)
        var row = worksheet.GetRow(1);

        // Assert
        if (worksheet.Rows.Count > 0)
        {
            Assert.NotNull(row);
            Assert.Equal(1, row.RowNumber);
        }
        else
        {
            Assert.Null(row); // If worksheet is empty, that's also valid
        }
    }

    [Fact]
    public async Task WorksheetGetDataRows_ShouldExcludeHeaderRow()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, 0);

        // Act
        var dataRows = worksheet.GetDataRows();

        // Assert
        Assert.NotNull(dataRows);
        
        // If there are data rows, they should all have RowNumber > 1
        if (dataRows.Count > 0)
        {
            Assert.All(dataRows, row => Assert.True(row.RowNumber > 1));
        }
    }

    [Fact]
    public async Task ExcelRowGetCellValue_ByColumnIndex_ShouldReturnValue()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, 0);

        // Act & Assert
        if (worksheet.Rows.Count > 0)
        {
            var firstRow = worksheet.Rows[0];
            if (firstRow.Cells.Count > 0)
            {
                var firstCellValue = firstRow.GetCellValue(0);
                // Value might be null or empty, but method should not throw
                // Just verify it doesn't throw an exception and returns a string or null
                Assert.True(firstCellValue == null || firstCellValue is string);
            }
        }
    }

    [Fact]
    public async Task ExcelRowGetCellValue_ByColumnName_ShouldReturnValue()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, 0);

        // Act & Assert
        if (worksheet.Rows.Count > 0)
        {
            var firstRow = worksheet.Rows[0];
            if (firstRow.Cells.Count > 0)
            {
                var firstCell = firstRow.Cells[0];
                var cellValue = firstRow.GetCellValue(firstCell.ColumnName);
                // Value might be null or empty, but method should not throw
                // Just verify it doesn't throw an exception and returns a string or null
                Assert.True(cellValue == null || cellValue is string);
            }
        }
    }

    [Fact]
    public async Task EndToEndTest_ProcessBothExcelFiles_ShouldWorkCompletelyE2E()
    {
        // This is an end-to-end test that simulates real Excel file processing
        
        // Arrange
        var test3Path = Path.Combine(_dataInputsPath, "Test3.xls");
        var test5Path = Path.Combine(_dataInputsPath, "Test5.xlsx");

        // Act & Assert - Test3.xls (legacy .xls format)
        Assert.True(await _excelFileHandler.FileExistsAsync(test3Path));
        var worksheetNames3 = await _excelFileHandler.GetWorksheetNamesAsync(test3Path);
        Assert.NotEmpty(worksheetNames3);
        
        var workbook3 = await _excelFileHandler.ReadFileAsync(test3Path);
        Assert.NotNull(workbook3);
        Assert.NotEmpty(workbook3.Worksheets);

        // Act & Assert - Test5.xlsx (modern .xlsx format)
        Assert.True(await _excelFileHandler.FileExistsAsync(test5Path));
        var worksheetNames5 = await _excelFileHandler.GetWorksheetNamesAsync(test5Path);
        Assert.NotEmpty(worksheetNames5);
        
        var workbook5 = await _excelFileHandler.ReadFileAsync(test5Path);
        Assert.NotNull(workbook5);
        Assert.NotEmpty(workbook5.Worksheets);

        // Verify we can navigate the structure successfully
        foreach (var workbook in new[] { workbook3, workbook5 })
        {
            foreach (var worksheet in workbook.Worksheets)
            {
                Assert.NotNull(worksheet.Name);
                Assert.NotNull(worksheet.Rows);
                Assert.NotNull(worksheet.Headers);
                
                foreach (var row in worksheet.Rows)
                {
                    Assert.True(row.RowNumber >= 1);
                    Assert.NotNull(row.Cells);
                    
                    foreach (var cell in row.Cells)
                    {
                        Assert.True(cell.ColumnIndex >= 0);
                        Assert.NotNull(cell.ColumnName);
                        Assert.NotNull(cell.DataType);
                        // Value can be null - that's valid
                    }
                }
            }
        }
    }

    [Fact]
    public async Task PerformanceTest_ReadLargeExcelFile_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test3.xls");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var workbook = await _excelFileHandler.ReadFileAsync(filePath);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(workbook);
        Assert.True(stopwatch.ElapsedMilliseconds < 30000, $"Excel file reading took too long: {stopwatch.ElapsedMilliseconds}ms");
        
        // Log performance for visibility
        var elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
        Assert.True(elapsedSeconds < 30, $"Reading Excel file should complete within 30 seconds, took {elapsedSeconds:F2} seconds");
    }

    [Fact]
    public async Task DataIntegrityTest_VerifyExcelDataTypes_ShouldMaintainDataTypes()
    {
        // Arrange
        var filePath = Path.Combine(_dataInputsPath, "Test5.xlsx");
        var worksheet = await _excelFileHandler.ReadWorksheetAsync(filePath, 0);

        // Act & Assert
        if (worksheet.Rows.Count > 0)
        {
            foreach (var row in worksheet.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    // Verify data type is properly set
                    Assert.NotNull(cell.DataType);
                    
                    // Verify data type is one of the expected types
                    var validTypes = new[] { typeof(string), typeof(int), typeof(long), typeof(double), typeof(decimal), typeof(DateTime), typeof(bool) };
                    Assert.Contains(cell.DataType, validTypes);
                }
            }
        }
    }
}
