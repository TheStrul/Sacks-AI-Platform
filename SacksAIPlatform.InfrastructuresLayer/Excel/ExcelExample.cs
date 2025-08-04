using SacksAIPlatform.InfrastructuresLayer.Excel.Implementations;
using SacksAIPlatform.InfrastructuresLayer.Excel.Interfaces;

namespace SacksAIPlatform.InfrastructuresLayer.Excel;

// Example usage of ExcelFileHandler
public static class ExcelExample
{
    public static async Task DemonstrateExcelReading(string filePath, Action<string>? logAction = null)
    {
        try
        {
            var excelHandler = new ExcelFileHandler();
            
            // Check if file exists
            if (!await excelHandler.FileExistsAsync(filePath))
            {
                logAction?.Invoke($"Excel file not found: {filePath}");
                return;
            }

            // Get worksheet names
            var worksheetNames = await excelHandler.GetWorksheetNamesAsync(filePath);
            logAction?.Invoke($"Found {worksheetNames.Count} worksheets: {string.Join(", ", worksheetNames)}");

            // Read the entire workbook
            var workbook = await excelHandler.ReadFileAsync(filePath);
            
            foreach (var worksheet in workbook.Worksheets)
            {
                logAction?.Invoke($"\n=== Worksheet: {worksheet.Name} ===");
                logAction?.Invoke($"Headers: {string.Join(", ", worksheet.Headers)}");
                logAction?.Invoke($"Total Rows: {worksheet.Rows.Count}");
                logAction?.Invoke($"Data Rows: {worksheet.GetDataRows().Count}");

                // Display first few data rows
                var dataRows = worksheet.GetDataRows().Take(3);
                foreach (var row in dataRows)
                {
                    var cellValues = row.Cells.Select(c => $"{c.ColumnName}='{c.Value}'");
                    logAction?.Invoke($"Row {row.RowNumber}: {string.Join(", ", cellValues)}");
                }
            }
        }
        catch (Exception ex)
        {
            logAction?.Invoke($"Error reading Excel file: {filePath} - {ex.Message}");
        }
    }
}
