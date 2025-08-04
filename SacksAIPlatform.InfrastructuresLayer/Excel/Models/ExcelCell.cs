namespace SacksAIPlatform.InfrastructuresLayer.Excel.Models;

public class ExcelCell
{
    public int ColumnIndex { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public Type DataType { get; set; } = typeof(string);
}
