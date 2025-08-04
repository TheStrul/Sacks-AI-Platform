using ExcelDataReader;
using SacksAIPlatform.InfrastructuresLayer.Excel.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Excel.Models;
using System.Data;
using System.Text;

namespace SacksAIPlatform.InfrastructuresLayer.Excel.Implementations;

public class ExcelFileHandler : IExcelFileHandler
{
    static ExcelFileHandler()
    {
        // Register encoding provider for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ExcelFileHandler()
    {
        // Constructor intentionally empty - encoding registered in static constructor
    }

    public async Task<Models.ExcelWorkbook> ReadFileAsync(string filePath)
    {
        if (!await FileExistsAsync(filePath))
        {
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        }

        var workbook = new Models.ExcelWorkbook { FilePath = filePath };

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration()
            {
                UseHeaderRow = true
            }
        });

        foreach (DataTable table in dataSet.Tables)
        {
            var worksheet = ConvertDataTableToWorksheet(table);
            workbook.Worksheets.Add(worksheet);
        }

        return workbook;
    }

    public async Task<Models.ExcelWorksheet> ReadWorksheetAsync(string filePath, string worksheetName)
    {
        var workbook = await ReadFileAsync(filePath);
        var worksheet = workbook.GetWorksheet(worksheetName);
        
        if (worksheet == null)
        {
            throw new ArgumentException($"Worksheet '{worksheetName}' not found in file: {filePath}");
        }

        return worksheet;
    }

    public async Task<Models.ExcelWorksheet> ReadWorksheetAsync(string filePath, int worksheetIndex = 0)
    {
        var workbook = await ReadFileAsync(filePath);
        var worksheet = workbook.GetWorksheet(worksheetIndex);
        
        if (worksheet == null)
        {
            throw new ArgumentException($"Worksheet index {worksheetIndex} is out of range. File has {workbook.Worksheets.Count} worksheets.");
        }

        return worksheet;
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.FromResult(File.Exists(filePath));
    }

    public async Task<List<string>> GetWorksheetNamesAsync(string filePath)
    {
        if (!await FileExistsAsync(filePath))
        {
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        }

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        
        var worksheetNames = new List<string>();
        do
        {
            worksheetNames.Add(reader.Name);
        } while (reader.NextResult());

        return worksheetNames;
    }

    private Models.ExcelWorksheet ConvertDataTableToWorksheet(DataTable dataTable)
    {
        var worksheet = new Models.ExcelWorksheet
        {
            Name = dataTable.TableName
        };

        // Add headers
        foreach (DataColumn column in dataTable.Columns)
        {
            worksheet.Headers.Add(column.ColumnName);
        }

        // Add header row
        var headerRow = new Models.ExcelRow { RowNumber = 1 };
        for (int col = 0; col < dataTable.Columns.Count; col++)
        {
            var headerCell = new ExcelCell
            {
                ColumnIndex = col,
                ColumnName = dataTable.Columns[col].ColumnName,
                Value = dataTable.Columns[col].ColumnName,
                DataType = typeof(string)
            };
            headerRow.Cells.Add(headerCell);
        }
        worksheet.Rows.Add(headerRow);

        // Add data rows
        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            var excelRow = new Models.ExcelRow { RowNumber = row + 2 }; // +2 because we start from 1 and skip header

            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var cellValue = dataTable.Rows[row][col];
                var excelCell = new ExcelCell
                {
                    ColumnIndex = col,
                    ColumnName = dataTable.Columns[col].ColumnName,
                    Value = cellValue?.ToString(),
                    DataType = GetCellDataType(cellValue)
                };
                excelRow.Cells.Add(excelCell);
            }

            worksheet.Rows.Add(excelRow);
        }

        return worksheet;
    }

    private Type GetCellDataType(object? value)
    {
        if (value == null || value == DBNull.Value) return typeof(string);

        return value.GetType() switch
        {
            Type t when t == typeof(int) => typeof(int),
            Type t when t == typeof(long) => typeof(long),
            Type t when t == typeof(double) => typeof(double),
            Type t when t == typeof(decimal) => typeof(decimal),
            Type t when t == typeof(DateTime) => typeof(DateTime),
            Type t when t == typeof(bool) => typeof(bool),
            _ => typeof(string)
        };
    }
}
