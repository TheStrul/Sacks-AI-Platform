using SacksAIPlatform.InfrastructuresLayer.Excel.Models;

namespace SacksAIPlatform.InfrastructuresLayer.Excel.Models;

public class ExcelWorkbook
{
    public string FilePath { get; set; } = string.Empty;
    public List<ExcelWorksheet> Worksheets { get; set; } = new List<ExcelWorksheet>();
    
    public ExcelWorksheet? GetWorksheet(string name)
    {
        return Worksheets.FirstOrDefault(w => w.Name == name);
    }
    
    public ExcelWorksheet? GetWorksheet(int index)
    {
        return index >= 0 && index < Worksheets.Count ? Worksheets[index] : null;
    }
}
