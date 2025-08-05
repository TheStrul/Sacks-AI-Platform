using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models;
using System.Data;
using Xunit;

namespace SacksAIPlatform.Tests.Infrastructures.FileProcessing;

/// <summary>
/// Tests for the FileData wrapper model
/// </summary>
public class FileDataTests
{
    private FileData CreateTestFileData()
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Age", typeof(int));
        dataTable.Columns.Add("City", typeof(string));
        
        dataTable.Rows.Add("John", 25, "New York");
        dataTable.Rows.Add("Jane", 30, "Los Angeles");
        dataTable.Rows.Add("Bob", 35, "Chicago");
        
        return new FileData(dataTable);
    }

    [Fact]
    public void Constructor_WithNullDataTable_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileData(null!));
    }

    [Fact]
    public void Headers_ShouldReturnCorrectColumnNames()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act
        var headers = fileData.Headers;

        // Assert
        Assert.Equal(3, headers.Length);
        Assert.Equal("Name", headers[0]);
        Assert.Equal("Age", headers[1]);
        Assert.Equal("City", headers[2]);
    }

    [Fact]
    public void RowCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Equal(3, fileData.RowCount);
    }

    [Fact]
    public void ColumnCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Equal(3, fileData.ColumnCount);
    }

    [Fact]
    public void GetValue_WithValidIndices_ShouldReturnCorrectValue()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Equal("John", fileData.GetValue(0, 0));
        Assert.Equal("25", fileData.GetValue(0, 1));
        Assert.Equal("New York", fileData.GetValue(0, 2));
        
        Assert.Equal("Jane", fileData.GetValue(1, 0));
        Assert.Equal("30", fileData.GetValue(1, 1));
        Assert.Equal("Los Angeles", fileData.GetValue(1, 2));
    }

    [Fact]
    public void GetValue_WithColumnName_ShouldReturnCorrectValue()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Equal("John", fileData.GetValue(0, "Name"));
        Assert.Equal("25", fileData.GetValue(0, "Age"));
        Assert.Equal("New York", fileData.GetValue(0, "City"));
        
        Assert.Equal("Jane", fileData.GetValue(1, "Name"));
        Assert.Equal("30", fileData.GetValue(1, "Age"));
        Assert.Equal("Los Angeles", fileData.GetValue(1, "City"));
    }

    [Fact]
    public void GetValue_WithInvalidRowIndex_ShouldReturnNull()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Null(fileData.GetValue(-1, 0));
        Assert.Null(fileData.GetValue(10, 0));
    }

    [Fact]
    public void GetValue_WithInvalidColumnIndex_ShouldReturnNull()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Null(fileData.GetValue(0, -1));
        Assert.Null(fileData.GetValue(0, 10));
    }

    [Fact]
    public void GetValue_WithInvalidColumnName_ShouldReturnNull()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Null(fileData.GetValue(0, "NonExistentColumn"));
    }

    [Fact]
    public void GetRow_WithValidIndex_ShouldReturnCorrectRow()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act
        var row = fileData.GetRow(1);

        // Assert
        Assert.Equal(3, row.Length);
        Assert.Equal("Jane", row[0]);
        Assert.Equal("30", row[1]);
        Assert.Equal("Los Angeles", row[2]);
    }

    [Fact]
    public void GetRow_WithInvalidIndex_ShouldReturnEmptyArray()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Empty(fileData.GetRow(-1));
        Assert.Empty(fileData.GetRow(10));
    }

    [Fact]
    public void GetColumn_WithValidIndex_ShouldReturnCorrectColumn()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act
        var column = fileData.GetColumn(0); // Name column

        // Assert
        Assert.Equal(3, column.Length);
        Assert.Equal("John", column[0]);
        Assert.Equal("Jane", column[1]);
        Assert.Equal("Bob", column[2]);
    }

    [Fact]
    public void GetColumn_WithColumnName_ShouldReturnCorrectColumn()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act
        var column = fileData.GetColumn("Age");

        // Assert
        Assert.Equal(3, column.Length);
        Assert.Equal("25", column[0]);
        Assert.Equal("30", column[1]);
        Assert.Equal("35", column[2]);
    }

    [Fact]
    public void GetColumn_WithInvalidIndex_ShouldReturnEmptyArray()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Empty(fileData.GetColumn(-1));
        Assert.Empty(fileData.GetColumn(10));
    }

    [Fact]
    public void GetColumn_WithInvalidColumnName_ShouldReturnEmptyArray()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act & Assert
        Assert.Empty(fileData.GetColumn("NonExistentColumn"));
    }

    [Fact]
    public void ToCells_ShouldReturnAllCells()
    {
        // Arrange
        var fileData = CreateTestFileData();

        // Act
        var cells = fileData.ToCells().ToList();

        // Assert
        Assert.Equal(9, cells.Count); // 3 rows × 3 columns
        
        // Check first row
        var firstRowCells = cells.Where(c => c.RowIndex == 0).ToList();
        Assert.Equal(3, firstRowCells.Count);
        Assert.Equal("Name", firstRowCells[0].ColumnName);
        Assert.Equal("John", firstRowCells[0].Value);
        Assert.Equal("Age", firstRowCells[1].ColumnName);
        Assert.Equal("25", firstRowCells[1].Value);
        Assert.Equal("City", firstRowCells[2].ColumnName);
        Assert.Equal("New York", firstRowCells[2].Value);
    }

    [Fact]
    public void DataTable_ShouldReturnOriginalDataTable()
    {
        // Arrange
        var originalTable = new DataTable();
        originalTable.Columns.Add("Test", typeof(string));
        originalTable.Rows.Add("Value");
        
        var fileData = new FileData(originalTable);

        // Act
        var retrievedTable = fileData.DataTable;

        // Assert
        Assert.Same(originalTable, retrievedTable);
    }
}
