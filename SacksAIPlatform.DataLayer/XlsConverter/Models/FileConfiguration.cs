namespace SacksAIPlatform.DataLayer.XlsConverter
{

    using System.Collections.ObjectModel;

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


        public Collection<int> DescriptionColumns { get; set; } = new();
        /// <summary>
        /// Collection of column indices that should be ignored during processing
        /// Useful for columns that contain irrelevant data
        /// </summary>
        public Collection<int> IgnoredColumns { get; set; } = new();

        /// <summary>
        /// Optional: Expected minimum number of columns
        /// Used for validation
        /// </summary>
        public int ValidNumOfColumns { get; set; } = 0;

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
                FormatName = "SimpleConfig",
                ValidNumOfColumns = 13,
                ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Code },
                { 1, PropertyType.Name },
                { 2, PropertyType.Brand },
                { 3, PropertyType.Concentration },
                { 4, PropertyType.Type },
                { 5, PropertyType.Gender },
                { 6, PropertyType.Size },
                { 7, PropertyType.LilFree },
                { 8, PropertyType.CountryOfOrigin },
            },

                DescriptionColumns = new Collection<int> { 10 }, // 3 Empty column at the end

                // 3 Empty column at the end
                IgnoredColumns = new Collection<int> { 11, 12 },
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
            if (ColumnMapping.ContainsValue(PropertyType.Code) == false)
            {
                throw new InvalidOperationException("ColumnMapping must include a mapping for PropertyType.Code");
            }



            // Check for duplicate property mappings (except Ignore)
            var duplicates = ColumnMapping.Values.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key);

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
            return ColumnMapping.TryGetValue(columnIndex, out var propertyType) ? propertyType : PropertyType.None;
        }

        /// <summary>
        /// Checks if a column should be ignored
        /// </summary>
        public bool IsColumnIgnored(int columnIndex)
        {
            return IgnoredColumns.Contains(columnIndex);
        }
    }



    /// <summary>
    /// Enum representing all mappable properties from CSV to Perfume entity
    /// Used for flexible CSV column mapping configuration
    /// </summary>
    public enum PropertyType
    {
        None = -1,
        Code,
        Name,
        Brand,
        Concentration,
        Type,
        Gender,
        Size,
        LilFree,
        CountryOfOrigin,
        Remarks,
        OriginalSource,
        Confirmed,
    }
}