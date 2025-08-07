using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.DataLayer.XlsConverter.Models;

namespace SacksAIPlatform.DataLayer.XlsConverter.Helpers;

/// <summary>
/// Helper class for managing runtime dictionary updates and parser configuration
/// Provides easy-to-use methods for adding new mappings and rules
/// </summary>
public class ProductParserRuntimeManager
{
    private readonly ProductParserConfigurationManager _configManager;
    private ProductDescriptionParser _parser;

    /// <summary>
    /// Initializes the runtime manager
    /// </summary>
    public ProductParserRuntimeManager(ProductParserConfigurationManager configManager)
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _parser = new ProductDescriptionParser(_configManager.CurrentConfiguration);
    }

    /// <summary>
    /// Gets the current parser instance
    /// </summary>
    public ProductDescriptionParser Parser => _parser;

    /// <summary>
    /// Gets the configuration manager
    /// </summary>
    public ProductParserConfigurationManager ConfigManager => _configManager;

    /// <summary>
    /// Recreates the parser with current configuration
    /// </summary>
    private void RefreshParser()
    {
        _parser = new ProductDescriptionParser(_configManager.CurrentConfiguration);
    }

    /// <summary>
    /// Adds a concentration mapping and refreshes the parser
    /// </summary>
    public void AddConcentrationMapping(string keyword, Concentration concentration)
    {
        _configManager.AddConcentrationMapping(keyword, concentration);
        RefreshParser();
    }

    /// <summary>
    /// Adds a type mapping and refreshes the parser
    /// </summary>
    public void AddTypeMapping(string keyword, PerfumeType type)
    {
        _configManager.AddTypeMapping(keyword, type);
        RefreshParser();
    }

    /// <summary>
    /// Adds a gender mapping and refreshes the parser
    /// </summary>
    public void AddGenderMapping(string keyword, Gender gender)
    {
        _configManager.AddGenderMapping(keyword, gender);
        RefreshParser();
    }

    /// <summary>
    /// Adds a units mapping and refreshes the parser
    /// </summary>
    public void AddUnitsMapping(string keyword, Units units)
    {
        _configManager.AddUnitsMapping(keyword, units);
        RefreshParser();
    }

    /// <summary>
    /// Adds a brand name to ID mapping and refreshes the parser
    /// </summary>
    public void AddBrandMapping(string brandName, int brandId)
    {
        _configManager.AddBrandMapping(brandName, brandId);
        RefreshParser();
    }

    /// <summary>
    /// Adds a product name to brand ID mapping and refreshes the parser
    /// </summary>
    public void AddProductToBrandMapping(string productName, int brandId)
    {
        _configManager.AddProductToBrandMapping(productName, brandId);
        RefreshParser();
    }

    /// <summary>
    /// Adds multiple brand mappings from Brand entities
    /// </summary>
    public void AddBrandMappingsFromEntities(IEnumerable<Brand> brands)
    {
        foreach (var brand in brands)
        {
            if (!string.IsNullOrEmpty(brand.Name))
            {
                _configManager.AddBrandMapping(brand.Name.ToUpperInvariant(), brand.BrandID);
            }
        }
        RefreshParser();
    }

    /// <summary>
    /// Adds multiple product to brand mappings from Product entities
    /// </summary>
    public void AddProductMappingsFromEntities(IEnumerable<Product> products)
    {
        foreach (var product in products)
        {
            if (!string.IsNullOrEmpty(product.Name) && product.BrandID > 0)
            {
                _configManager.AddProductToBrandMapping(product.Name.ToUpperInvariant(), product.BrandID);
            }
        }
        RefreshParser();
    }

    /// <summary>
    /// Adds a new parsing rule and refreshes the parser
    /// </summary>
    public void AddParsingRule(string name, string pattern, PropertyType propertyType, 
        int priority = 10, List<int>? extractGroups = null, bool stopOnMatch = false)
    {
        var rule = new ParsingRule
        {
            Name = name,
            Pattern = pattern,
            PropertyType = propertyType,
            Priority = priority,
            ExtractGroups = extractGroups ?? new List<int> { 1 },
            StopOnMatch = stopOnMatch,
            Description = $"Custom rule: {name}"
        };

        _configManager.AddParsingRule(rule);
        RefreshParser();
    }

    /// <summary>
    /// Adds an ignore pattern and refreshes the parser
    /// </summary>
    public void AddIgnorePattern(string pattern)
    {
        _configManager.AddIgnorePattern(pattern);
        RefreshParser();
    }

    /// <summary>
    /// Tests a description against the current parser and returns results
    /// </summary>
    public ParsedProductInfo TestParsing(string description)
    {
        return _parser.ParseDescription(description);
    }

    /// <summary>
    /// Tests parsing on a sample and shows before/after comparison
    /// </summary>
    public ParsingTestResult TestParsingWithComparison(string description)
    {
        var originalProduct = new Product
        {
            Code = "TEST",
            Name = "Test Product",
            Concentration = Concentration.EDT,
            Type = PerfumeType.Spray,
            Gender = Gender.Unisex,
            Size = "0",
            Units = Units.ml,
            BrandID = 0
        };

        var testProduct = new Product
        {
            Code = originalProduct.Code,
            Name = originalProduct.Name,
            Concentration = originalProduct.Concentration,
            Type = originalProduct.Type,
            Gender = originalProduct.Gender,
            Size = originalProduct.Size,
            Units = originalProduct.Units,
            BrandID = originalProduct.BrandID
        };

        var parsed = _parser.ParseDescription(description);
        _parser.ParseAndUpdateProduct(testProduct, description);

        return new ParsingTestResult
        {
            OriginalDescription = description,
            ParsedInfo = parsed,
            OriginalProduct = originalProduct,
            UpdatedProduct = testProduct,
            FoundMatches = parsed.HasMatches()
        };
    }

    /// <summary>
    /// Validates all mappings and rules in the current configuration
    /// </summary>
    public List<string> ValidateConfiguration()
    {
        return _configManager.ValidateConfiguration();
    }

    /// <summary>
    /// Gets statistics about the current configuration
    /// </summary>
    public ConfigurationStatistics GetStatistics()
    {
        var config = _configManager.CurrentConfiguration;
        return new ConfigurationStatistics
        {
            ConcentrationMappings = config.ConcentrationDictionary.Count,
            TypeMappings = config.TypeDictionary.Count,
            GenderMappings = config.GenderDictionary.Count,
            UnitsMappings = config.UnitsDictionary.Count,
            BrandMappings = config.BrandNameToIdDictionary.Count,
            ProductToBrandMappings = config.ProductNameToBrandIdDictionary.Count,
            ParsingRules = config.ParsingRules.Count,
            IgnorePatterns = config.IgnorePatterns.Count
        };
    }

    /// <summary>
    /// Creates a backup of the current configuration
    /// </summary>
    public void CreateBackup(string? backupPath = null)
    {
        _configManager.CreateBackup(backupPath);
    }

    /// <summary>
    /// Resets configuration to default values
    /// </summary>
    public void ResetToDefault()
    {
        _configManager.ResetToDefault();
        RefreshParser();
    }

    /// <summary>
    /// Exports current configuration to a file
    /// </summary>
    public void ExportConfiguration(string filePath)
    {
        _configManager.ExportConfiguration(filePath);
    }

    /// <summary>
    /// Imports configuration from a file
    /// </summary>
    public void ImportConfiguration(string filePath)
    {
        _configManager.ImportConfiguration(filePath);
        RefreshParser();
    }

    /// <summary>
    /// Learns mappings from successful parsing examples
    /// </summary>
    public void LearnFromExamples(IEnumerable<LearningExample> examples)
    {
        foreach (var example in examples)
        {
            var parsed = _parser.ParseDescription(example.Description);
            
            // Learn from what was expected but not found
            if (example.ExpectedConcentration.HasValue && !parsed.Concentration.HasValue)
            {
                // Try to find a word in the description that could map to the concentration
                var words = example.Description.ToUpperInvariant().Split(' ');
                var unmappedWord = words.FirstOrDefault(w => 
                    !_configManager.CurrentConfiguration.ConcentrationDictionary.ContainsKey(w) &&
                    w.Length > 2);
                
                if (!string.IsNullOrEmpty(unmappedWord))
                {
                    AddConcentrationMapping(unmappedWord, example.ExpectedConcentration.Value);
                }
            }

            // Similar learning for other properties...
            if (example.ExpectedType.HasValue && !parsed.Type.HasValue)
            {
                var words = example.Description.ToUpperInvariant().Split(' ');
                var unmappedWord = words.FirstOrDefault(w => 
                    !_configManager.CurrentConfiguration.TypeDictionary.ContainsKey(w) &&
                    w.Length > 2);
                
                if (!string.IsNullOrEmpty(unmappedWord))
                {
                    AddTypeMapping(unmappedWord, example.ExpectedType.Value);
                }
            }
        }
    }
}

/// <summary>
/// Result of testing parsing with before/after comparison
/// </summary>
public class ParsingTestResult
{
    public string OriginalDescription { get; set; } = string.Empty;
    public ParsedProductInfo ParsedInfo { get; set; } = new();
    public Product OriginalProduct { get; set; } = new();
    public Product UpdatedProduct { get; set; } = new();
    public bool FoundMatches { get; set; }

    public string GetSummary()
    {
        var changes = new List<string>();

        if (UpdatedProduct.Concentration != OriginalProduct.Concentration)
            changes.Add($"Concentration: {OriginalProduct.Concentration} ? {UpdatedProduct.Concentration}");

        if (UpdatedProduct.Type != OriginalProduct.Type)
            changes.Add($"Type: {OriginalProduct.Type} ? {UpdatedProduct.Type}");

        if (UpdatedProduct.Gender != OriginalProduct.Gender)
            changes.Add($"Gender: {OriginalProduct.Gender} ? {UpdatedProduct.Gender}");

        if (UpdatedProduct.Size != OriginalProduct.Size)
            changes.Add($"Size: {OriginalProduct.Size} ? {UpdatedProduct.Size}");

        if (UpdatedProduct.Units != OriginalProduct.Units)
            changes.Add($"Units: {OriginalProduct.Units} ? {UpdatedProduct.Units}");

        if (UpdatedProduct.BrandID != OriginalProduct.BrandID)
            changes.Add($"Brand ID: {OriginalProduct.BrandID} ? {UpdatedProduct.BrandID}");

        return changes.Count > 0 ? string.Join(", ", changes) : "No changes detected";
    }
}

/// <summary>
/// Statistics about the current configuration
/// </summary>
public class ConfigurationStatistics
{
    public int ConcentrationMappings { get; set; }
    public int TypeMappings { get; set; }
    public int GenderMappings { get; set; }
    public int UnitsMappings { get; set; }
    public int BrandMappings { get; set; }
    public int ProductToBrandMappings { get; set; }
    public int ParsingRules { get; set; }
    public int IgnorePatterns { get; set; }
    public int TotalMappings => ConcentrationMappings + TypeMappings + GenderMappings + 
                               UnitsMappings + BrandMappings + ProductToBrandMappings;
}

/// <summary>
/// Example for learning new mappings
/// </summary>
public class LearningExample
{
    public string Description { get; set; } = string.Empty;
    public Concentration? ExpectedConcentration { get; set; }
    public PerfumeType? ExpectedType { get; set; }
    public Gender? ExpectedGender { get; set; }
    public Units? ExpectedUnits { get; set; }
    public string? ExpectedSize { get; set; }
    public int? ExpectedBrandId { get; set; }
}