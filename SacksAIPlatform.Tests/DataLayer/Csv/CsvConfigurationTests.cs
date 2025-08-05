using SacksAIPlatform.DataLayer.Csv.Models;
using SacksAIPlatform.DataLayer.Enums;
using Xunit;

namespace SacksAIPlatform.Tests.DataLayer.Csv;

/// <summary>
/// Tests for CsvConfiguration class and its functionality
/// </summary>
public class CsvConfigurationTests
{
    [Fact]
    public void CsvConfiguration_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new CsvConfiguration();

        // Assert
        Assert.Equal(0, config.TitleIndex);
        Assert.Equal(1, config.StartFromRow);
        Assert.Equal(-1, config.EndAtRow);
        Assert.False(config.HasInnerTitles);
        Assert.NotNull(config.ColumnMapping);
        Assert.NotNull(config.IgnoredColumns);
        Assert.Empty(config.ColumnMapping);
        Assert.Empty(config.IgnoredColumns);
        Assert.Equal(0, config.MinimumColumns);
        Assert.Equal("Default", config.FormatName);
    }

    [Fact]
    public void CreateDefaultConfiguration_ShouldReturnValidComprehensiveStockAiConfig()
    {
        // Act
        var config = CsvConfiguration.CreateDefaultConfiguration();

        // Assert
        Assert.Equal("ComprehensiveStockAi", config.FormatName);
        Assert.Equal(0, config.TitleIndex);
        Assert.Equal(1, config.StartFromRow);
        Assert.Equal(-1, config.EndAtRow);
        Assert.False(config.HasInnerTitles);
        Assert.Equal(11, config.MinimumColumns);
        
        // Check column mappings
        Assert.Equal(PropertyType.Confirmed, config.GetPropertyType(0));
        Assert.Equal(PropertyType.UPC, config.GetPropertyType(1));
        Assert.Equal(PropertyType.Brand, config.GetPropertyType(2));
        Assert.Equal(PropertyType.ProductName, config.GetPropertyType(3));
        Assert.Equal(PropertyType.SizeAndUnits, config.GetPropertyType(4));
        Assert.Equal(PropertyType.Type, config.GetPropertyType(5));
        Assert.Equal(PropertyType.Concentration, config.GetPropertyType(6));
        Assert.Equal(PropertyType.Gender, config.GetPropertyType(7));
        Assert.Equal(PropertyType.CountryOfOrigin, config.GetPropertyType(8));
        Assert.Equal(PropertyType.LiFree, config.GetPropertyType(9));
        Assert.Equal(PropertyType.Ignore, config.GetPropertyType(10));
        Assert.Equal(PropertyType.TotalProducts, config.GetPropertyType(11));
        
        // Check ignored columns
        Assert.True(config.IsColumnIgnored(10));
        Assert.False(config.IsColumnIgnored(1));
    }

    [Fact]
    public void CreateSimpleConfiguration_ShouldReturnValidSimpleConfig()
    {
        // Act
        var config = CsvConfiguration.CreateSimpleConfiguration();

        // Assert
        Assert.Equal("Simple", config.FormatName);
        Assert.Equal(5, config.MinimumColumns);
        Assert.Equal(5, config.ColumnMapping.Count);
        
        // Check basic mappings
        Assert.Equal(PropertyType.Code, config.GetPropertyType(0));
        Assert.Equal(PropertyType.Name, config.GetPropertyType(1));
        Assert.Equal(PropertyType.Brand, config.GetPropertyType(2));
        Assert.Equal(PropertyType.Size, config.GetPropertyType(3));
        Assert.Equal(PropertyType.Concentration, config.GetPropertyType(4));
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            TitleIndex = 0,
            StartFromRow = 1,
            EndAtRow = 100,
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Code },
                { 1, PropertyType.Name }
            }
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithInvalidStartRow_ShouldThrowException()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            TitleIndex = 5,
            StartFromRow = 3, // Invalid: StartFromRow <= TitleIndex
            ColumnMapping = new Dictionary<int, PropertyType> { { 0, PropertyType.Name } }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("StartFromRow", exception.Message);
        Assert.Contains("TitleIndex", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidEndRow_ShouldThrowException()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            StartFromRow = 10,
            EndAtRow = 5, // Invalid: EndAtRow <= StartFromRow
            ColumnMapping = new Dictionary<int, PropertyType> { { 0, PropertyType.Name } }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("EndAtRow", exception.Message);
        Assert.Contains("StartFromRow", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyColumnMapping_ShouldThrowException()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            ColumnMapping = new Dictionary<int, PropertyType>() // Empty mapping
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("ColumnMapping cannot be empty", exception.Message);
    }

    [Fact]
    public void Validate_WithDuplicatePropertyMappings_ShouldThrowException()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Name },
                { 1, PropertyType.Name } // Duplicate mapping
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Duplicate property mappings", exception.Message);
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    public void Validate_WithMultipleIgnoreProperties_ShouldNotThrow()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Name },
                { 1, PropertyType.Ignore },
                { 2, PropertyType.Ignore }, // Multiple Ignore is allowed
                { 3, PropertyType.Brand }
            }
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void GetPropertyType_WithValidColumnIndex_ShouldReturnCorrectType()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Code },
                { 5, PropertyType.Brand }
            }
        };

        // Act & Assert
        Assert.Equal(PropertyType.Code, config.GetPropertyType(0));
        Assert.Equal(PropertyType.Brand, config.GetPropertyType(5));
        Assert.Equal(PropertyType.Ignore, config.GetPropertyType(999)); // Non-existent column
    }

    [Fact]
    public void IsColumnIgnored_WithIgnoredColumns_ShouldReturnCorrectResult()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Name },
                { 1, PropertyType.Ignore }
            },
            IgnoredColumns = new HashSet<int> { 5, 10 }
        };

        // Act & Assert
        Assert.False(config.IsColumnIgnored(0)); // Valid column
        Assert.True(config.IsColumnIgnored(1));  // Mapped to Ignore
        Assert.True(config.IsColumnIgnored(5));  // In IgnoredColumns
        Assert.True(config.IsColumnIgnored(10)); // In IgnoredColumns
        Assert.True(config.IsColumnIgnored(999)); // Non-existent column (defaults to Ignore)
    }

    [Fact]
    public void CsvConfiguration_ComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new CsvConfiguration
        {
            TitleIndex = 2,
            StartFromRow = 3,
            EndAtRow = 1000,
            HasInnerTitles = true,
            FormatName = "CustomFormat",
            MinimumColumns = 8,
            ColumnMapping = new Dictionary<int, PropertyType>
            {
                { 0, PropertyType.Confirmed },
                { 1, PropertyType.UPC },
                { 2, PropertyType.Brand },
                { 3, PropertyType.ProductName },
                { 4, PropertyType.SizeAndUnits },
                { 5, PropertyType.Concentration },
                { 6, PropertyType.Gender },
                { 7, PropertyType.CountryOfOrigin }
            },
            IgnoredColumns = new HashSet<int> { 8, 9, 10 }
        };

        // Act & Assert
        config.Validate(); // Should not throw
        
        Assert.Equal("CustomFormat", config.FormatName);
        Assert.True(config.HasInnerTitles);
        Assert.Equal(8, config.MinimumColumns);
        Assert.Equal(8, config.ColumnMapping.Count);
        Assert.Equal(3, config.IgnoredColumns.Count);
        
        // Test specific mappings
        Assert.Equal(PropertyType.UPC, config.GetPropertyType(1));
        Assert.Equal(PropertyType.Brand, config.GetPropertyType(2));
        Assert.True(config.IsColumnIgnored(8));
        Assert.False(config.IsColumnIgnored(1));
    }
}
