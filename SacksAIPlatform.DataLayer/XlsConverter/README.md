# Product Description Parser System

A configurable dictionary helper and parser system for converting raw text data into structured Product objects. This system allows runtime configuration of parsing rules and dictionaries to handle complex product descriptions.

## Overview

The system consists of several key components:

1. **ProductParserConfiguration** - Configurable dictionaries and parsing rules
2. **ProductDescriptionParser** - Main parser that extracts product information from text
3. **ProductParserConfigurationManager** - Manages loading/saving configuration with JSON persistence
4. **ProductParserRuntimeManager** - Provides easy runtime configuration updates
5. **FileToProductConverter Integration** - Seamlessly integrates with existing file conversion process

## Features

- **Runtime Configurable**: Add new dictionary mappings and parsing rules at runtime
- **JSON Persistence**: Configuration saved to JSON files for persistence
- **Regex-Based Rules**: Flexible pattern matching with priority-based rule execution
- **Dictionary Mappings**: Support for concentration, type, gender, units, brand, and product mappings
- **Learning Capability**: Can learn from examples to improve parsing accuracy
- **Testing Framework**: Built-in testing and validation tools

## Basic Usage

### 1. Simple Parsing

```csharp
// Create parser with default configuration
var parser = ProductDescriptionParser.CreateDefault();

// Parse a complex description
var description = "ADP BLU MEDITERRANEO MIRTO DI PANAREA 30ML EDT SPRAY 29.6ml";
var result = parser.ParseDescription(description);

Console.WriteLine(result.GetSummary());
// Output: Concentration: Parfum, Type: Spray, Size: 30, Units: ml
```

### 2. Update Product Object

```csharp
var product = new Product { Code = "TEST001" };
parser.ParseAndUpdateProduct(product, description);

// Product now has parsed values:
// - Concentration: Parfum (from "ADP")
// - Type: Spray (from "SPRAY")
// - Size: "30" (from "30ML")
// - Units: ml (from "30ML")
```

### 3. Runtime Configuration

```csharp
// Create configuration manager
var configManager = new ProductParserConfigurationManager();
var runtimeManager = new ProductParserRuntimeManager(configManager);

// Add custom mappings at runtime
runtimeManager.AddConcentrationMapping("AQUA", Concentration.EDC);
runtimeManager.AddBrandMapping("CHANEL", 1);
runtimeManager.AddTypeMapping("ATOMIZER", PerfumeType.Spray);

// Test with new mappings
var result = runtimeManager.TestParsing("CHANEL NO 5 100ML AQUA ATOMIZER");
```

## Configuration Structure

### Dictionary Mappings

The system supports several types of dictionary mappings:

```csharp
// Concentration mappings
"EDT" -> Concentration.EDT
"ADP" -> Concentration.Parfum  // Custom mapping

// Type mappings
"SPRAY" -> PerfumeType.Spray
"ATOMIZER" -> PerfumeType.Spray  // Custom mapping

// Brand mappings
"CHANEL" -> 1 (Brand ID)
"DIOR" -> 2 (Brand ID)

// Product to Brand mappings
"NO 5" -> 1 (Maps to Chanel brand)
```

### Parsing Rules

Regex-based rules for extracting structured information:

```csharp
new ParsingRule
{
    Name = "ExtractSizeWithUnits",
    Pattern = @"(\d+(?:\.\d+)?)\s*(ML|OZ|G)",
    PropertyType = PropertyType.Size,
    Priority = 1,
    ExtractGroups = new List<int> { 1, 2 } // Extract both number and unit
}
```

## Integration with FileToProductConverter

### Basic Integration

```csharp
// Create converter with custom parser configuration
var configManager = new ProductParserConfigurationManager();
var converter = new FiletoProductConverter(configManager);

// The converter now uses the parser for AnalyzeDescriptionFields
```

### Advanced Integration

```csharp
// Configure parser before using converter
var runtimeManager = new ProductParserRuntimeManager(converter.ConfigurationManager);

// Add brand mappings from database
var brands = databaseService.GetAllBrands();
runtimeManager.AddBrandMappingsFromEntities(brands);

// Add custom concentration mappings
runtimeManager.AddConcentrationMapping("INTENSE", Concentration.Parfum);
runtimeManager.AddConcentrationMapping("PURE", Concentration.EDP);
```

## Examples from Your Use Case

### Example 1: "ADP BLU MEDITERRANEO MIRTO DI PANAREA 30ML EDT SPRAY 29.6ml"

**Expected Parsing:**
- "ADP" ? Concentration.Parfum
- "30ML" ? Size: "30", Units: ml
- "SPRAY" ? PerfumeType.Spray
- "29.6ml" ? Ignored (via ignore patterns)

**Configuration needed:**
```csharp
// This is included in default configuration
configManager.AddConcentrationMapping("ADP", Concentration.Parfum);
configManager.AddIgnorePattern(@"\d+\.\d+ML");  // Ignore "29.6ml"
```

### Example 2: Runtime Brand Learning

```csharp
// Learn brand mappings from successful conversions
var products = GetProcessedProducts();
runtimeManager.AddProductMappingsFromEntities(products);

// Now "BLU MEDITERRANEO" could map to a brand ID if found in products
```

## Configuration Management

### Save/Load Configuration

```csharp
// Configuration is automatically saved to JSON
var configManager = new ProductParserConfigurationManager("my-config.json");

// Export configuration
configManager.ExportConfiguration("backup-config.json");

// Import configuration
configManager.ImportConfiguration("shared-config.json");
```

### Backup and Reset

```csharp
// Create backup before making changes
runtimeManager.CreateBackup();

// Reset to default if needed
runtimeManager.ResetToDefault();
```

## Testing and Validation

### Test Parsing Results

```csharp
// Test a description and see what changes
var testResult = runtimeManager.TestParsingWithComparison(description);
Console.WriteLine($"Changes: {testResult.GetSummary()}");
```

### Validate Configuration

```csharp
// Check for configuration errors
var errors = runtimeManager.ValidateConfiguration();
if (errors.Count > 0)
{
    foreach (var error in errors)
        Console.WriteLine($"Error: {error}");
}
```

### Configuration Statistics

```csharp
var stats = runtimeManager.GetStatistics();
Console.WriteLine($"Total mappings: {stats.TotalMappings}");
Console.WriteLine($"Parsing rules: {stats.ParsingRules}");
```

## Best Practices

1. **Start with Default Configuration**: The default configuration includes common perfume industry terms
2. **Add Brand Mappings Early**: Load brand mappings from your database at startup
3. **Use Priority in Rules**: Lower priority numbers execute first
4. **Test Before Deploying**: Use the testing framework to validate changes
5. **Backup Before Major Changes**: Always backup configuration before bulk updates
6. **Learn from Data**: Use the learning functionality to improve parsing from successful examples

## File Structure

```
SacksAIPlatform.DataLayer/XlsConverter/
??? Models/
?   ??? ProductParserConfiguration.cs    # Configuration classes
??? Helpers/
?   ??? ProductDescriptionParser.cs      # Main parser
?   ??? ProductParserConfigurationManager.cs  # Config management
?   ??? ProductParserRuntimeManager.cs   # Runtime management
??? Examples/
?   ??? ProductParserUsageExample.cs     # Usage examples
??? Implementations/
    ??? FileToProductConverter.cs        # Updated with parser integration
```

## JSON Configuration Format

The configuration is stored in JSON format:

```json
{
  "concentrationDictionary": {
    "EDT": "EDT",
    "ADP": "Parfum"
  },
  "typeDictionary": {
    "SPRAY": "Spray",
    "ATOMIZER": "Spray"
  },
  "brandNameToIdDictionary": {
    "CHANEL": 1,
    "DIOR": 2
  },
  "parsingRules": [
    {
      "name": "ExtractSizeWithUnits",
      "pattern": "(\\d+(?:\\.\\d+)?)\\s*(ML|OZ|G)",
      "propertyType": "Size",
      "priority": 1,
      "extractGroups": [1, 2]
    }
  ],
  "ignorePatterns": [
    "\\d+\\.\\d+ML"
  ]
}
```

This system provides a flexible, configurable solution for parsing complex product descriptions into structured data, with full runtime configurability as requested.