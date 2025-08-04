using SacksAIPlatform.InfrastructuresLayer.Excel.Models;

namespace SacksAIPlatform.InfrastructuresLayer.Excel.Interfaces;

public interface IExcelFileHandler
{
    Task<Models.ExcelWorkbook> ReadFileAsync(string filePath);
    Task<Models.ExcelWorksheet> ReadWorksheetAsync(string filePath, string worksheetName);
    Task<Models.ExcelWorksheet> ReadWorksheetAsync(string filePath, int worksheetIndex = 0);
    Task<bool> FileExistsAsync(string filePath);
    Task<List<string>> GetWorksheetNamesAsync(string filePath);
}
