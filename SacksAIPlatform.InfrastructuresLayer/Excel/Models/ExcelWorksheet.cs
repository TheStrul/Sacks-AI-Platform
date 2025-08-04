using SacksAIPlatform.InfrastructuresLayer.Excel.Models;

namespace SacksAIPlatform.InfrastructuresLayer.Excel.Models;

public class ExcelWorksheet
{
    public string Name { get; set; } = string.Empty;
    public List<ExcelRow> Rows { get; set; } = new List<ExcelRow>();
    public List<string> Headers { get; set; } = new List<string>();
    
    public ExcelRow? GetRow(int rowNumber)
    {
        return Rows.FirstOrDefault(r => r.RowNumber == rowNumber);
    }
    
    public List<ExcelRow> GetDataRows()
    {
        return Rows.Where(r => r.RowNumber > 1).ToList(); // Exclude header row
    }
}
