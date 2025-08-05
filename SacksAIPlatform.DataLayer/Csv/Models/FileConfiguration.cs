using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.DataLayer.Csv.Models;

/// <summary>
/// Configuration class for flexible CSV parsing and column mapping
/// Allows different CSV formats to be processed by the same converter
/// </summary>
public class FileConfiguration
{
    /// <summary>
    /// Row index where column titles/headers are located (0-based)
    /// Set to -1 if there are no headers
    /// </summary>
    public int TitleIndex { get; set; } = 0;
    
    /// <summary>
    /// Row index where data starts (0-based)
    /// Usually TitleIndex + 1 if headers exist
    /// </summary>
    public int StartFromRow { get; set; } = 1;
    
    /// <summary>
    /// Row index where data ends (0-based)
    /// Set to -1 to process until end of file
    /// </summary>
    public int EndAtRow { get; set; } = -1;
    
    /// <summary>
    /// Whether the CSV has inner/sub-titles within the data
    /// If true, will skip rows that appear to be titles
    /// </summary>
    public bool HasInnerTitles { get; set; } = false;
    
    /// <summary>
    /// Maps CSV column indices to Perfume property types
    /// Key: Column index (0-based)
    /// Value: Property type to map to
    /// </summary>
    public Dictionary<int, PropertyType> ColumnMapping { get; set; } = new();
    
    /// <summary>
    /// Collection of column indices that should be ignored during processing
    /// Useful for columns that contain irrelevant data
    /// </summary>
    public HashSet<int> IgnoredColumns { get; set; } = new();
    
    /// <summary>
    /// Optional: Expected minimum number of columns
    /// Used for validation
    /// </summary>
    public int MinimumColumns { get; set; } = 0;
    
    /// <summary>
    /// Optional: File format identifier for logging/debugging
    /// </summary>
    public string FormatName { get; set; } = "Default";
    
    /// <summary>
    /// Creates a default configuration for ComprehensiveStockAi.csv format
    /// </summary>
    public static FileConfiguration CreateDefaultConfiguration()
    {
        return new FileConfiguration
        {
            TitleIndex = 0,
            StartFromRow = 1,
            EndAtRow = -1,
            HasInnerTitles = false,
            FormatName = "ComprehensiveStockAi",
            MinimumColumns = 11,
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Confirmed },
                { 1, PropertyType.UPC },
                { 2, PropertyType.Brand },
                { 3, PropertyType.ProductName },
                { 4, PropertyType.SizeAndUnits },
                { 5, PropertyType.Type },
                { 6, PropertyType.Concentration },
                { 7, PropertyType.Gender },
                { 8, PropertyType.CountryOfOrigin },
                { 9, PropertyType.LiFree },
                { 10, PropertyType.Ignore }, // Empty column
                { 11, PropertyType.TotalProducts }
            },
            IgnoredColumns = new HashSet<int> { 10 } // Empty column between LiFree and TotalProducts
        };
    }
    
    /// <summary>
    /// Creates a simple configuration for basic perfume CSV files
    /// </summary>
    public static FileConfiguration CreateSimpleConfiguration()
    {
        return new FileConfiguration
        {
            TitleIndex = 0,
            StartFromRow = 1,
            EndAtRow = -1,
            HasInnerTitles = false,
            FormatName = "Simple",
            MinimumColumns = 5,
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Code },
                { 1, PropertyType.Name },
                { 2, PropertyType.Brand },
                { 3, PropertyType.Size },
                { 4, PropertyType.Concentration }
            }
        };
    }
    
    /// <summary>
    /// Validates the configuration for consistency
    /// </summary>
    public void Validate()
    {
        if (TitleIndex >= 0 && StartFromRow <= TitleIndex)
        {
            throw new InvalidOperationException($"StartFromRow ({StartFromRow}) must be greater than TitleIndex ({TitleIndex})");
        }
        
        if (EndAtRow >= 0 && EndAtRow <= StartFromRow)
        {
            throw new InvalidOperationException($"EndAtRow ({EndAtRow}) must be greater than StartFromRow ({StartFromRow})");
        }
        
        if (ColumnMapping.Count == 0)
        {
            throw new InvalidOperationException("ColumnMapping cannot be empty");
        }
        
        // Check for duplicate property mappings (except Ignore)
        var propertyTypes = ColumnMapping.Values.Where(p => p != PropertyType.Ignore).ToList();
        var duplicates = propertyTypes.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key);
        
        if (duplicates.Any())
        {
            throw new InvalidOperationException($"Duplicate property mappings found: {string.Join(", ", duplicates)}");
        }
    }
    
    /// <summary>
    /// Gets the property type for a given column index
    /// </summary>
    public PropertyType GetPropertyType(int columnIndex)
    {
        return ColumnMapping.TryGetValue(columnIndex, out var propertyType) ? propertyType : PropertyType.Ignore;
    }
    
    /// <summary>
    /// Checks if a column should be ignored
    /// </summary>
    public bool IsColumnIgnored(int columnIndex)
    {
        return IgnoredColumns.Contains(columnIndex) || GetPropertyType(columnIndex) == PropertyType.Ignore;
    }
}
