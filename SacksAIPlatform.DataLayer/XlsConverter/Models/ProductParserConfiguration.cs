using SacksAIPlatform.DataLayer.Enums;
using System.Text.Json.Serialization;

namespace SacksAIPlatform.DataLayer.XlsConverter.Models;

/// <summary>
/// Configuration for the Product Parser Dictionary system
/// Allows runtime configuration of parsing rules and dictionaries
/// </summary>
public class ProductParserConfiguration
{
    /// <summary>
    /// Dictionary mapping keywords to Concentration enum values
    /// </summary>
    [JsonPropertyName("concentrationDictionary")]
    public Dictionary<string, Concentration> ConcentrationDictionary { get; set; } = new();

    /// <summary>
    /// Dictionary mapping keywords to PerfumeType enum values
    /// </summary>
    [JsonPropertyName("typeDictionary")]
    public Dictionary<string, PerfumeType> TypeDictionary { get; set; } = new();

    /// <summary>
    /// Dictionary mapping keywords to Units enum values
    /// </summary>
    [JsonPropertyName("unitsDictionary")]
    public Dictionary<string, Units> UnitsDictionary { get; set; } = new();

    /// <summary>
    /// Dictionary mapping keywords to Gender enum values
    /// </summary>
    [JsonPropertyName("genderDictionary")]
    public Dictionary<string, Gender> GenderDictionary { get; set; } = new();

    /// <summary>
    /// Dictionary mapping brand names to brand IDs
    /// </summary>
    [JsonPropertyName("brandNameToIdDictionary")]
    public Dictionary<string, int> BrandNameToIdDictionary { get; set; } = new();

    /// <summary>
    /// Dictionary mapping product names to brand IDs (for products that indicate brand)
    /// </summary>
    [JsonPropertyName("productNameToBrandIdDictionary")]
    public Dictionary<string, int> ProductNameToBrandIdDictionary { get; set; } = new();

    /// <summary>
    /// Collection of parsing rules for extracting information from text
    /// </summary>
    [JsonPropertyName("parsingRules")]
    public List<ParsingRule> ParsingRules { get; set; } = new();

    /// <summary>
    /// Collection of words/patterns to ignore during parsing
    /// </summary>
    [JsonPropertyName("ignorePatterns")]
    public List<string> IgnorePatterns { get; set; } = new();

    /// <summary>
    /// Case sensitivity setting for dictionary lookups
    /// </summary>
    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Creates a default configuration with common perfume industry terms
    /// </summary>
    public static ProductParserConfiguration CreateDefault()
    {
        var config = new ProductParserConfiguration
        {
            CaseSensitive = false,
            ConcentrationDictionary = new Dictionary<string, Concentration>(StringComparer.OrdinalIgnoreCase)
            {
                // Standard abbreviations
                { "EDT", Concentration.EDT },
                { "EDP", Concentration.EDP },
                { "EDC", Concentration.EDC },
                { "EDF", Concentration.EDF },
                { "ADP", Concentration.Parfum }, // Based on your example
                { "PARFUM", Concentration.Parfum },
                { "COLOGNE", Concentration.EDC },
                
                // Full names
                { "EAU DE TOILETTE", Concentration.EDT },
                { "EAU DE PARFUM", Concentration.EDP },
                { "EAU DE COLOGNE", Concentration.EDC },
                { "EAU DE FRAICHE", Concentration.EDF },
                { "PARFUM INTENSE", Concentration.Parfum },
                { "ELIXIR", Concentration.Parfum }
            },

            TypeDictionary = new Dictionary<string, PerfumeType>(StringComparer.OrdinalIgnoreCase)
            {
                { "SPRAY", PerfumeType.Spray },
                { "SP", PerfumeType.Spray },
                { "COLOGNE", PerfumeType.Cologne },
                { "SPLASH", PerfumeType.Splash },
                { "FL", PerfumeType.Splash },
                { "OIL", PerfumeType.Oil },
                { "SOLID", PerfumeType.Solid },
                { "ROLLETTE", PerfumeType.Rollette },
                { "ROLL-ON", PerfumeType.Rollette }
            },

            UnitsDictionary = new Dictionary<string, Units>(StringComparer.OrdinalIgnoreCase)
            {
                { "ML", Units.ml },
                { "MILLILITER", Units.ml },
                { "MILLILITERS", Units.ml },
                { "OZ", Units.oz },
                { "FL OZ", Units.oz },
                { "FLUID OUNCE", Units.oz },
                { "FLUID OUNCES", Units.oz },
                { "G", Units.g },
                { "GRAM", Units.g },
                { "GRAMS", Units.g }
            },

            GenderDictionary = new Dictionary<string, Gender>(StringComparer.OrdinalIgnoreCase)
            {
                { "M", Gender.Male },
                { "MALE", Gender.Male },
                { "MEN", Gender.Male },
                { "MAN", Gender.Male },
                { "W", Gender.Female },
                { "F", Gender.Female },
                { "FEMALE", Gender.Female },
                { "WOMEN", Gender.Female },
                { "WOMAN", Gender.Female },
                { "U", Gender.Unisex },
                { "UNISEX", Gender.Unisex }
            },

            IgnorePatterns = new List<string>
            {
                @"\d+\.\d+ML",  // Patterns like "29.6ml" at the end
                @"\d+\.\d+OZ",  // Patterns like "1.7oz" at the end
                @"^\d+$",       // Pure numbers
                "NEW",
                "ORIGINAL",
                "AUTHENTIC",
                "TESTER"
            }
        };

        // Add default parsing rules
        config.ParsingRules.AddRange(CreateDefaultParsingRules());

        return config;
    }

    /// <summary>
    /// Creates default parsing rules for common patterns
    /// </summary>
    private static List<ParsingRule> CreateDefaultParsingRules()
    {
        return new List<ParsingRule>
        {
            // Size extraction rules
            new ParsingRule
            {
                Name = "ExtractSizeWithUnits",
                Pattern = @"(\d+(?:\.\d+)?)\s*(ML|OZ|G)",
                PropertyType = PropertyType.Size,
                Priority = 1,
                ExtractGroups = new List<int> { 1, 2 }, // Extract both number and unit
                Description = "Extracts size like '30ML', '1.7OZ', '50G'"
            },

            // Concentration rules
            new ParsingRule
            {
                Name = "ExtractConcentration",
                Pattern = @"\b(EDT|EDP|EDC|EDF|ADP|PARFUM)\b",
                PropertyType = PropertyType.Concentration,
                Priority = 2,
                ExtractGroups = new List<int> { 1 },
                Description = "Extracts concentration abbreviations"
            },

            // Type rules
            new ParsingRule
            {
                Name = "ExtractType",
                Pattern = @"\b(SPRAY|SP|COLOGNE|SPLASH|FL|OIL|SOLID|ROLLETTE|ROLL-ON)\b",
                PropertyType = PropertyType.Type,
                Priority = 3,
                ExtractGroups = new List<int> { 1 },
                Description = "Extracts perfume type"
            },

            // Brand extraction at beginning
            new ParsingRule
            {
                Name = "ExtractBrandAtStart",
                Pattern = @"^(\w+)\s+",
                PropertyType = PropertyType.Brand,
                Priority = 4,
                ExtractGroups = new List<int> { 1 },
                Description = "Extracts potential brand name at the beginning"
            }
        };
    }

    /// <summary>
    /// Adds or updates a concentration mapping
    /// </summary>
    public void AddConcentrationMapping(string keyword, Concentration concentration)
    {
        ConcentrationDictionary[keyword] = concentration;
    }

    /// <summary>
    /// Adds or updates a type mapping
    /// </summary>
    public void AddTypeMapping(string keyword, PerfumeType type)
    {
        TypeDictionary[keyword] = type;
    }

    /// <summary>
    /// Adds or updates a units mapping
    /// </summary>
    public void AddUnitsMapping(string keyword, Units units)
    {
        UnitsDictionary[keyword] = units;
    }

    /// <summary>
    /// Adds or updates a gender mapping
    /// </summary>
    public void AddGenderMapping(string keyword, Gender gender)
    {
        GenderDictionary[keyword] = gender;
    }

    /// <summary>
    /// Adds or updates a brand name to ID mapping
    /// </summary>
    public void AddBrandMapping(string brandName, int brandId)
    {
        BrandNameToIdDictionary[brandName] = brandId;
    }

    /// <summary>
    /// Adds or updates a product name to brand ID mapping
    /// </summary>
    public void AddProductToBrandMapping(string productName, int brandId)
    {
        ProductNameToBrandIdDictionary[productName] = brandId;
    }

    /// <summary>
    /// Adds a new parsing rule
    /// </summary>
    public void AddParsingRule(ParsingRule rule)
    {
        ParsingRules.Add(rule);
        // Sort by priority (lower number = higher priority)
        ParsingRules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// Adds an ignore pattern
    /// </summary>
    public void AddIgnorePattern(string pattern)
    {
        IgnorePatterns.Add(pattern);
    }
}

/// <summary>
/// Represents a parsing rule for extracting specific information from text
/// </summary>
public class ParsingRule
{
    /// <summary>
    /// Name of the rule for identification
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Regular expression pattern to match
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Property type this rule extracts
    /// </summary>
    [JsonPropertyName("propertyType")]
    public PropertyType PropertyType { get; set; }

    /// <summary>
    /// Priority of the rule (lower number = higher priority)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 10;

    /// <summary>
    /// Groups to extract from the regex match (1-based)
    /// </summary>
    [JsonPropertyName("extractGroups")]
    public List<int> ExtractGroups { get; set; } = new();

    /// <summary>
    /// Description of what this rule does
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this rule should be case sensitive
    /// </summary>
    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Whether to stop processing other rules if this one matches
    /// </summary>
    [JsonPropertyName("stopOnMatch")]
    public bool StopOnMatch { get; set; } = false;
}