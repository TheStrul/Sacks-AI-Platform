using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.DataLayer.XlsConverter.Models;
using System.Text.RegularExpressions;

namespace SacksAIPlatform.DataLayer.XlsConverter.Helpers;

/// <summary>
/// Configurable parser for extracting Product properties from text descriptions
/// Uses dictionary mappings and regex rules to parse complex product descriptions
/// </summary>
public class ProductDescriptionParser
{
    private readonly ProductParserConfiguration _configuration;
    private readonly Dictionary<PropertyType, Regex> _compiledRules;

    /// <summary>
    /// Initializes the parser with the provided configuration
    /// </summary>
    public ProductDescriptionParser(ProductParserConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _compiledRules = CompileRules(_configuration.ParsingRules);
    }

    /// <summary>
    /// Creates a parser with default configuration
    /// </summary>
    public static ProductDescriptionParser CreateDefault()
    {
        return new ProductDescriptionParser(ProductParserConfiguration.CreateDefault());
    }

    /// <summary>
    /// Parses a text description and extracts product information
    /// </summary>
    /// <param name="description">The text description to parse</param>
    /// <returns>Parsed product information</returns>
    public ParsedProductInfo ParseDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new ParsedProductInfo();

        var result = new ParsedProductInfo
        {
            OriginalText = description,
            CleanedText = CleanText(description)
        };

        // Apply all parsing rules
        foreach (var rule in _configuration.ParsingRules.OrderBy(r => r.Priority))
        {
            ApplyRule(rule, result);
            
            if (rule.StopOnMatch && result.HasMatches())
                break;
        }

        // Apply dictionary lookups for any unmatched properties
        ApplyDictionaryLookups(result);

        return result;
    }

    /// <summary>
    /// Updates a Product object with parsed information from a description
    /// </summary>
    /// <param name="product">The product to update</param>
    /// <param name="description">The description to parse</param>
    /// <param name="overwriteExisting">Whether to overwrite existing non-default values</param>
    public void ParseAndUpdateProduct(Product product, string description, bool overwriteExisting = false)
    {
        if (product == null || string.IsNullOrWhiteSpace(description))
            return;

        var parsed = ParseDescription(description);

        // Update concentration
        if (parsed.Concentration.HasValue && 
            (overwriteExisting || product.Concentration == Concentration.EDT)) // EDT is default
        {
            product.Concentration = parsed.Concentration.Value;
        }

        // Update type
        if (parsed.Type.HasValue && 
            (overwriteExisting || product.Type == PerfumeType.Spray)) // Spray is default
        {
            product.Type = parsed.Type.Value;
        }

        // Update gender
        if (parsed.Gender.HasValue && 
            (overwriteExisting || product.Gender == Gender.Unisex)) // Unisex is default
        {
            product.Gender = parsed.Gender.Value;
        }

        // Update size and units
        if (!string.IsNullOrEmpty(parsed.Size) && 
            (overwriteExisting || string.IsNullOrEmpty(product.Size)))
        {
            product.Size = parsed.Size;
        }

        if (parsed.Units.HasValue && 
            (overwriteExisting || product.Units == Units.ml)) // ml is default
        {
            product.Units = parsed.Units.Value;
        }

        // Update brand ID if found
        if (parsed.BrandId.HasValue && 
            (overwriteExisting || product.BrandID == 0))
        {
            product.BrandID = parsed.BrandId.Value;
        }

        // Update product name if extracted
        if (!string.IsNullOrEmpty(parsed.ExtractedProductName) && 
            (overwriteExisting || string.IsNullOrEmpty(product.Name)))
        {
            product.Name = parsed.ExtractedProductName;
        }
    }

    /// <summary>
    /// Compiles regex patterns from parsing rules for better performance
    /// </summary>
    private Dictionary<PropertyType, Regex> CompileRules(List<ParsingRule> rules)
    {
        var compiled = new Dictionary<PropertyType, Regex>();

        foreach (var rule in rules)
        {
            try
            {
                var options = RegexOptions.Compiled;
                if (!rule.CaseSensitive && !_configuration.CaseSensitive)
                    options |= RegexOptions.IgnoreCase;

                compiled[rule.PropertyType] = new Regex(rule.Pattern, options);
            }
            catch (ArgumentException ex)
            {
                // Log invalid regex pattern
                throw new InvalidOperationException($"Invalid regex pattern in rule '{rule.Name}': {rule.Pattern}", ex);
            }
        }

        return compiled;
    }

    /// <summary>
    /// Cleans the input text by removing ignore patterns
    /// </summary>
    private string CleanText(string text)
    {
        var cleaned = text.ToUpperInvariant().Trim();

        // Remove ignore patterns
        foreach (var pattern in _configuration.IgnorePatterns)
        {
            try
            {
                cleaned = Regex.Replace(cleaned, pattern, "", RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                // Skip invalid regex patterns
                continue;
            }
        }

        // Clean multiple spaces
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    /// <summary>
    /// Applies a single parsing rule to the result
    /// </summary>
    private void ApplyRule(ParsingRule rule, ParsedProductInfo result)
    {
        if (!_compiledRules.TryGetValue(rule.PropertyType, out var regex))
            return;

        var matches = regex.Matches(result.CleanedText);

        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            switch (rule.PropertyType)
            {
                case PropertyType.Size:
                    ExtractSizeInfo(rule, match, result);
                    break;
                case PropertyType.Concentration:
                    ExtractConcentration(rule, match, result);
                    break;
                case PropertyType.Type:
                    ExtractType(rule, match, result);
                    break;
                case PropertyType.Gender:
                    ExtractGender(rule, match, result);
                    break;
                case PropertyType.Brand:
                    ExtractBrand(rule, match, result);
                    break;
                case PropertyType.Name:
                    ExtractProductName(rule, match, result);
                    break;
            }

            // Mark as matched
            result.MatchedRules.Add(rule.Name);

            if (rule.StopOnMatch)
                break;
        }
    }

    /// <summary>
    /// Extracts size and units information from regex match
    /// </summary>
    private void ExtractSizeInfo(ParsingRule rule, Match match, ParsedProductInfo result)
    {
        if (rule.ExtractGroups.Count >= 2)
        {
            // Extract size number (first group)
            if (rule.ExtractGroups.Count > 0 && match.Groups.Count > rule.ExtractGroups[0])
            {
                var sizeText = match.Groups[rule.ExtractGroups[0]].Value;
                if (decimal.TryParse(sizeText, out var size))
                {
                    result.Size = size.ToString("0.#");
                }
            }

            // Extract units (second group)
            if (rule.ExtractGroups.Count > 1 && match.Groups.Count > rule.ExtractGroups[1])
            {
                var unitsText = match.Groups[rule.ExtractGroups[1]].Value.ToUpperInvariant();
                if (_configuration.UnitsDictionary.TryGetValue(unitsText, out var units))
                {
                    result.Units = units;
                }
            }
        }
    }

    /// <summary>
    /// Extracts concentration from regex match
    /// </summary>
    private void ExtractConcentration(ParsingRule rule, Match match, ParsedProductInfo result)
    {
        if (rule.ExtractGroups.Count > 0 && match.Groups.Count > rule.ExtractGroups[0])
        {
            var concentrationText = match.Groups[rule.ExtractGroups[0]].Value.ToUpperInvariant();
            if (_configuration.ConcentrationDictionary.TryGetValue(concentrationText, out var concentration))
            {
                result.Concentration = concentration;
            }
        }
    }

    /// <summary>
    /// Extracts perfume type from regex match
    /// </summary>
    private void ExtractType(ParsingRule rule, Match match, ParsedProductInfo result)
    {
        if (rule.ExtractGroups.Count > 0 && match.Groups.Count > rule.ExtractGroups[0])
        {
            var typeText = match.Groups[rule.ExtractGroups[0]].Value.ToUpperInvariant();
            if (_configuration.TypeDictionary.TryGetValue(typeText, out var type))
            {
                result.Type = type;
            }
        }
    }

    /// <summary>
    /// Extracts gender from regex match
    /// </summary>
    private void ExtractGender(ParsingRule rule, Match match, ParsedProductInfo result)
    {
        if (rule.ExtractGroups.Count > 0 && match.Groups.Count > rule.ExtractGroups[0])
        {
            var genderText = match.Groups[rule.ExtractGroups[0]].Value.ToUpperInvariant();
            if (_configuration.GenderDictionary.TryGetValue(genderText, out var gender))
            {
                result.Gender = gender;
            }
        }
    }

    /// <summary>
    /// Extracts brand information from regex match
    /// </summary>
    private void ExtractBrand(ParsingRule rule, Match match, ParsedProductInfo result)
    {
        if (rule.ExtractGroups.Count > 0 && match.Groups.Count > rule.ExtractGroups[0])
        {
            var brandText = match.Groups[rule.ExtractGroups[0]].Value.ToUpperInvariant();
            if (_configuration.BrandNameToIdDictionary.TryGetValue(brandText, out var brandId))
            {
                result.BrandId = brandId;
                result.ExtractedBrandName = brandText;
            }
        }
    }

    /// <summary>
    /// Extracts product name from regex match
    /// </summary>
    private void ExtractProductName(ParsingRule rule, Match match, ParsedProductInfo result)
    {
        if (rule.ExtractGroups.Count > 0 && match.Groups.Count > rule.ExtractGroups[0])
        {
            var productName = match.Groups[rule.ExtractGroups[0]].Value.Trim();
            result.ExtractedProductName = productName;

            // Check if this product name maps to a brand
            if (_configuration.ProductNameToBrandIdDictionary.TryGetValue(productName.ToUpperInvariant(), out var brandId))
            {
                result.BrandId = brandId;
            }
        }
    }

    /// <summary>
    /// Applies dictionary lookups for simple word matching
    /// </summary>
    private void ApplyDictionaryLookups(ParsedProductInfo result)
    {
        var words = result.CleanedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var upperWord = word.ToUpperInvariant();

            // Check concentration dictionary
            if (!result.Concentration.HasValue && _configuration.ConcentrationDictionary.TryGetValue(upperWord, out var concentration))
            {
                result.Concentration = concentration;
            }

            // Check type dictionary
            if (!result.Type.HasValue && _configuration.TypeDictionary.TryGetValue(upperWord, out var type))
            {
                result.Type = type;
            }

            // Check gender dictionary
            if (!result.Gender.HasValue && _configuration.GenderDictionary.TryGetValue(upperWord, out var gender))
            {
                result.Gender = gender;
            }

            // Check brand dictionary
            if (!result.BrandId.HasValue && _configuration.BrandNameToIdDictionary.TryGetValue(upperWord, out var brandId))
            {
                result.BrandId = brandId;
                result.ExtractedBrandName = upperWord;
            }
        }
    }

    /// <summary>
    /// Gets the current configuration
    /// </summary>
    public ProductParserConfiguration GetConfiguration() => _configuration;

    /// <summary>
    /// Updates the parser configuration at runtime
    /// </summary>
    public void UpdateConfiguration(ProductParserConfiguration newConfiguration)
    {
        // This would require recreating the parser instance in practice
        throw new NotSupportedException("Configuration updates require creating a new parser instance");
    }
}

/// <summary>
/// Result of parsing a product description
/// </summary>
public class ParsedProductInfo
{
    public string OriginalText { get; set; } = string.Empty;
    public string CleanedText { get; set; } = string.Empty;
    public Concentration? Concentration { get; set; }
    public PerfumeType? Type { get; set; }
    public Gender? Gender { get; set; }
    public Units? Units { get; set; }
    public string Size { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public string ExtractedBrandName { get; set; } = string.Empty;
    public string ExtractedProductName { get; set; } = string.Empty;
    public List<string> MatchedRules { get; set; } = new();

    /// <summary>
    /// Indicates if any matches were found
    /// </summary>
    public bool HasMatches() => 
        Concentration.HasValue || Type.HasValue || Gender.HasValue || 
        Units.HasValue || !string.IsNullOrEmpty(Size) || BrandId.HasValue;

    /// <summary>
    /// Gets a summary of extracted information
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        if (Concentration.HasValue) parts.Add($"Concentration: {Concentration}");
        if (Type.HasValue) parts.Add($"Type: {Type}");
        if (Gender.HasValue) parts.Add($"Gender: {Gender}");
        if (!string.IsNullOrEmpty(Size)) parts.Add($"Size: {Size}");
        if (Units.HasValue) parts.Add($"Units: {Units}");
        if (BrandId.HasValue) parts.Add($"Brand ID: {BrandId}");
        if (!string.IsNullOrEmpty(ExtractedBrandName)) parts.Add($"Brand: {ExtractedBrandName}");
        if (!string.IsNullOrEmpty(ExtractedProductName)) parts.Add($"Product: {ExtractedProductName}");

        return string.Join(", ", parts);
    }
}