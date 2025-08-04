namespace SacksAIPlatform.InfrastructuresLayer.Excel.Models;

public class ExcelRow
{
    public int RowNumber { get; set; }
    public List<ExcelCell> Cells { get; set; } = new List<ExcelCell>();
    
    public string? GetCellValue(int columnIndex)
    {
        return Cells.FirstOrDefault(c => c.ColumnIndex == columnIndex)?.Value;
    }
    
    public string? GetCellValue(string columnName)
    {
        return Cells.FirstOrDefault(c => c.ColumnName == columnName)?.Value;
    }
}
