using SacksAIPlatform.DataLayer.XlsConverter.Models;
using System.Text.Json;

namespace SacksAIPlatform.DataLayer.XlsConverter.Helpers;

/// <summary>
/// Manages loading, saving, and runtime updates of ProductParserConfiguration
/// Supports JSON serialization for persistent storage
/// </summary>
public class ProductParserConfigurationManager
{
    private readonly string _configurationFilePath;
    private ProductParserConfiguration _currentConfiguration;

    /// <summary>
    /// Initializes the configuration manager
    /// </summary>
    /// <param name="configurationFilePath">Path to the JSON configuration file</param>
    public ProductParserConfigurationManager(string configurationFilePath = "product-parser-config.json")
    {
        _configurationFilePath = configurationFilePath;
        _currentConfiguration = LoadConfiguration();
    }

    /// <summary>
    /// Gets the current configuration
    /// </summary>
    public ProductParserConfiguration CurrentConfiguration => _currentConfiguration;

    /// <summary>
    /// Loads configuration from file or creates default if file doesn't exist
    /// </summary>
    public ProductParserConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configurationFilePath))
            {
                var json = File.ReadAllText(_configurationFilePath);
                var config = JsonSerializer.Deserialize<ProductParserConfiguration>(json, GetJsonOptions());
                _currentConfiguration = config ?? ProductParserConfiguration.CreateDefault();
            }
            else
            {
                _currentConfiguration = ProductParserConfiguration.CreateDefault();
                SaveConfiguration(); // Save default configuration
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration from {_configurationFilePath}: {ex.Message}", ex);
        }

        return _currentConfiguration;
    }

    /// <summary>
    /// Saves the current configuration to file
    /// </summary>
    public void SaveConfiguration()
    {
        try
        {
            var json = JsonSerializer.Serialize(_currentConfiguration, GetJsonOptions());
            var directory = Path.GetDirectoryName(_configurationFilePath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_configurationFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration to {_configurationFilePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates the configuration and saves it
    /// </summary>
    public void UpdateConfiguration(ProductParserConfiguration newConfiguration)
    {
        _currentConfiguration = newConfiguration ?? throw new ArgumentNullException(nameof(newConfiguration));
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new concentration mapping at runtime
    /// </summary>
    public void AddConcentrationMapping(string keyword, SacksAIPlatform.DataLayer.Enums.Concentration concentration)
    {
        _currentConfiguration.AddConcentrationMapping(keyword, concentration);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new type mapping at runtime
    /// </summary>
    public void AddTypeMapping(string keyword, SacksAIPlatform.DataLayer.Enums.PerfumeType type)
    {
        _currentConfiguration.AddTypeMapping(keyword, type);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new units mapping at runtime
    /// </summary>
    public void AddUnitsMapping(string keyword, SacksAIPlatform.DataLayer.Enums.Units units)
    {
        _currentConfiguration.AddUnitsMapping(keyword, units);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new gender mapping at runtime
    /// </summary>
    public void AddGenderMapping(string keyword, SacksAIPlatform.DataLayer.Enums.Gender gender)
    {
        _currentConfiguration.AddGenderMapping(keyword, gender);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new brand name to ID mapping at runtime
    /// </summary>
    public void AddBrandMapping(string brandName, int brandId)
    {
        _currentConfiguration.AddBrandMapping(brandName, brandId);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new product name to brand ID mapping at runtime
    /// </summary>
    public void AddProductToBrandMapping(string productName, int brandId)
    {
        _currentConfiguration.AddProductToBrandMapping(productName, brandId);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new parsing rule at runtime
    /// </summary>
    public void AddParsingRule(ParsingRule rule)
    {
        _currentConfiguration.AddParsingRule(rule);
        SaveConfiguration();
    }

    /// <summary>
    /// Adds a new ignore pattern at runtime
    /// </summary>
    public void AddIgnorePattern(string pattern)
    {
        _currentConfiguration.AddIgnorePattern(pattern);
        SaveConfiguration();
    }

    /// <summary>
    /// Removes a concentration mapping
    /// </summary>
    public void RemoveConcentrationMapping(string keyword)
    {
        _currentConfiguration.ConcentrationDictionary.Remove(keyword);
        SaveConfiguration();
    }

    /// <summary>
    /// Removes a type mapping
    /// </summary>
    public void RemoveTypeMapping(string keyword)
    {
        _currentConfiguration.TypeDictionary.Remove(keyword);
        SaveConfiguration();
    }

    /// <summary>
    /// Removes a brand mapping
    /// </summary>
    public void RemoveBrandMapping(string brandName)
    {
        _currentConfiguration.BrandNameToIdDictionary.Remove(brandName);
        SaveConfiguration();
    }

    /// <summary>
    /// Removes a product to brand mapping
    /// </summary>
    public void RemoveProductToBrandMapping(string productName)
    {
        _currentConfiguration.ProductNameToBrandIdDictionary.Remove(productName);
        SaveConfiguration();
    }

    /// <summary>
    /// Removes a parsing rule by name
    /// </summary>
    public void RemoveParsingRule(string ruleName)
    {
        _currentConfiguration.ParsingRules.RemoveAll(r => r.Name == ruleName);
        SaveConfiguration();
    }

    /// <summary>
    /// Gets all concentration mappings
    /// </summary>
    public Dictionary<string, SacksAIPlatform.DataLayer.Enums.Concentration> GetConcentrationMappings()
    {
        return new Dictionary<string, SacksAIPlatform.DataLayer.Enums.Concentration>(_currentConfiguration.ConcentrationDictionary);
    }

    /// <summary>
    /// Gets all type mappings
    /// </summary>
    public Dictionary<string, SacksAIPlatform.DataLayer.Enums.PerfumeType> GetTypeMappings()
    {
        return new Dictionary<string, SacksAIPlatform.DataLayer.Enums.PerfumeType>(_currentConfiguration.TypeDictionary);
    }

    /// <summary>
    /// Gets all brand mappings
    /// </summary>
    public Dictionary<string, int> GetBrandMappings()
    {
        return new Dictionary<string, int>(_currentConfiguration.BrandNameToIdDictionary);
    }

    /// <summary>
    /// Gets all product to brand mappings
    /// </summary>
    public Dictionary<string, int> GetProductToBrandMappings()
    {
        return new Dictionary<string, int>(_currentConfiguration.ProductNameToBrandIdDictionary);
    }

    /// <summary>
    /// Gets all parsing rules
    /// </summary>
    public List<ParsingRule> GetParsingRules()
    {
        return new List<ParsingRule>(_currentConfiguration.ParsingRules);
    }

    /// <summary>
    /// Exports configuration to a different file
    /// </summary>
    public void ExportConfiguration(string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(_currentConfiguration, GetJsonOptions());
            var directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export configuration to {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Imports configuration from a different file
    /// </summary>
    public void ImportConfiguration(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<ProductParserConfiguration>(json, GetJsonOptions());
            
            if (config == null)
                throw new InvalidOperationException("Failed to deserialize configuration");

            _currentConfiguration = config;
            SaveConfiguration();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import configuration from {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resets configuration to default
    /// </summary>
    public void ResetToDefault()
    {
        _currentConfiguration = ProductParserConfiguration.CreateDefault();
        SaveConfiguration();
    }

    /// <summary>
    /// Creates a backup of the current configuration
    /// </summary>
    public void CreateBackup(string? backupPath = null)
    {
        backupPath ??= $"{_configurationFilePath}.backup.{DateTime.Now:yyyyMMdd-HHmmss}";
        ExportConfiguration(backupPath);
    }

    /// <summary>
    /// Gets JSON serialization options for configuration persistence
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Validates the current configuration
    /// </summary>
    public List<string> ValidateConfiguration()
    {
        var errors = new List<string>();

        // Validate parsing rules
        foreach (var rule in _currentConfiguration.ParsingRules)
        {
            if (string.IsNullOrEmpty(rule.Name))
                errors.Add("Parsing rule with empty name found");

            if (string.IsNullOrEmpty(rule.Pattern))
                errors.Add($"Parsing rule '{rule.Name}' has empty pattern");

            // Test regex pattern
            try
            {
                _ = new System.Text.RegularExpressions.Regex(rule.Pattern);
            }
            catch (ArgumentException)
            {
                errors.Add($"Parsing rule '{rule.Name}' has invalid regex pattern: {rule.Pattern}");
            }
        }

        // Validate ignore patterns
        foreach (var pattern in _currentConfiguration.IgnorePatterns)
        {
            try
            {
                _ = new System.Text.RegularExpressions.Regex(pattern);
            }
            catch (ArgumentException)
            {
                errors.Add($"Invalid ignore pattern: {pattern}");
            }
        }

        return errors;
    }
}