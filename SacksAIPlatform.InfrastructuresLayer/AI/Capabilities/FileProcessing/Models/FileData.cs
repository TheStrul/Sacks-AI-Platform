using System.Data;

namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models;

/// <summary>
/// Container for file data read from any supported format (CSV, XLS, XLSX)
/// Provides convenient access to the underlying DataTable
/// </summary>
public class FileData
{
    private readonly DataTable _dataTable;

    public FileData(DataTable dataTable)
    {
        _dataTable = dataTable ?? throw new ArgumentNullException(nameof(dataTable));
    }

    /// <summary>
    /// Gets the underlying DataTable
    /// </summary>
    public DataTable DataTable => _dataTable;

    /// <summary>
    /// Gets the column headers
    /// </summary>
    public string[] Headers => _dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

    /// <summary>
    /// Gets the number of data rows (excluding header)
    /// </summary>
    public int RowCount => _dataTable.Rows.Count;

    /// <summary>
    /// Gets the number of columns
    /// </summary>
    public int ColumnCount => _dataTable.Columns.Count;

    /// <summary>
    /// Gets a cell value by row and column index
    /// </summary>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <param name="columnIndex">Zero-based column index</param>
    /// <returns>Cell value as string</returns>
    public string? GetValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _dataTable.Rows.Count)
            return null;
        if (columnIndex < 0 || columnIndex >= _dataTable.Columns.Count)
            return null;

        return _dataTable.Rows[rowIndex][columnIndex]?.ToString();
    }

    /// <summary>
    /// Gets a cell value by row index and column name
    /// </summary>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <param name="columnName">Column name</param>
    /// <returns>Cell value as string</returns>
    public string? GetValue(int rowIndex, string columnName)
    {
        if (rowIndex < 0 || rowIndex >= _dataTable.Rows.Count)
            return null;
        if (!_dataTable.Columns.Contains(columnName))
            return null;

        return _dataTable.Rows[rowIndex][columnName]?.ToString();
    }

    /// <summary>
    /// Gets all values in a specific row
    /// </summary>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <returns>Array of cell values</returns>
    public string?[] GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _dataTable.Rows.Count)
            return Array.Empty<string>();

        return _dataTable.Rows[rowIndex].ItemArray.Select(v => v?.ToString()).ToArray();
    }

    /// <summary>
    /// Gets all values in a specific column
    /// </summary>
    /// <param name="columnIndex">Zero-based column index</param>
    /// <returns>Array of cell values</returns>
    public string?[] GetColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _dataTable.Columns.Count)
            return Array.Empty<string>();

        return _dataTable.Rows.Cast<DataRow>().Select(row => row[columnIndex]?.ToString()).ToArray();
    }

    /// <summary>
    /// Gets all values in a specific column by name
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <returns>Array of cell values</returns>
    public string?[] GetColumn(string columnName)
    {
        if (!_dataTable.Columns.Contains(columnName))
            return Array.Empty<string>();

        return _dataTable.Rows.Cast<DataRow>().Select(row => row[columnName]?.ToString()).ToArray();
    }

    /// <summary>
    /// Converts the data to a FileCell enumerable for easier processing
    /// </summary>
    /// <returns>Enumerable of FileCell objects</returns>
    public IEnumerable<FileCell> ToCells()
    {
        for (int rowIndex = 0; rowIndex < _dataTable.Rows.Count; rowIndex++)
        {
            var row = _dataTable.Rows[rowIndex];
            for (int columnIndex = 0; columnIndex < _dataTable.Columns.Count; columnIndex++)
            {
                var column = _dataTable.Columns[columnIndex];
                yield return new FileCell
                {
                    RowIndex = rowIndex,
                    ColumnIndex = columnIndex,
                    ColumnName = column.ColumnName,
                    Value = row[columnIndex]?.ToString(),
                    DataType = column.DataType
                };
            }
        }
    }
}
