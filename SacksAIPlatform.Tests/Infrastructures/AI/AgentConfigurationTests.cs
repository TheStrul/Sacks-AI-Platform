using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using System.Text.Json;

namespace SacksAIPlatform.Tests.Infrastructures.AI;

/// <summary>
/// Tests for AgentConfiguration and related models
/// Tests the new capability-based configuration system
/// </summary>
public class AgentConfigurationTests
{
    [Fact]
    public void AgentConfiguration_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new AgentConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Agent);
        Assert.NotNull(config.Agent.Capabilities);
        Assert.NotNull(config.Agent.ConversationRules);
        Assert.NotNull(config.Agent.ErrorHandling);
    }

    [Fact]
    public void AgentSettings_DefaultConstructor_ShouldInitializeCollections()
    {
        // Act
        var settings = new AgentSettings();

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.Name);
        Assert.NotNull(settings.Description);
        Assert.NotNull(settings.Personality);
        Assert.NotNull(settings.SystemPrompt);
        Assert.NotNull(settings.Capabilities);
        Assert.NotNull(settings.ConversationRules);
        Assert.NotNull(settings.ErrorHandling);
        Assert.Empty(settings.Capabilities);
        Assert.Empty(settings.ConversationRules);
        Assert.Empty(settings.ErrorHandling);
    }

    [Fact]
    public void AgentCapabilityConfig_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var capability = new AgentCapabilityConfig();

        // Assert
        Assert.NotNull(capability);
        Assert.NotNull(capability.Id);
        Assert.NotNull(capability.Name);
        Assert.NotNull(capability.Description);
        Assert.True(capability.IsEnabled); // Should default to enabled
        Assert.NotNull(capability.Tools);
        Assert.NotNull(capability.Examples);
        Assert.NotNull(capability.Configuration);
        Assert.Empty(capability.Tools);
        Assert.Empty(capability.Examples);
        Assert.Empty(capability.Configuration);
    }

    [Fact]
    public void AgentCapabilityConfig_WithConfiguration_ShouldStoreData()
    {
        // Arrange
        var capability = new AgentCapabilityConfig
        {
            Id = "test-capability",
            Name = "Test Capability",
            Description = "A test capability for unit testing",
            IsEnabled = true,
            Tools = new List<string> { "Tool1", "Tool2" },
            Examples = new List<string> { "Example 1", "Example 2" },
            Configuration = new Dictionary<string, object>
            {
                { "setting1", "value1" },
                { "setting2", 42 },
                { "setting3", true }
            }
        };

        // Assert
        Assert.Equal("test-capability", capability.Id);
        Assert.Equal("Test Capability", capability.Name);
        Assert.Equal("A test capability for unit testing", capability.Description);
        Assert.True(capability.IsEnabled);
        Assert.Equal(2, capability.Tools.Count);
        Assert.Equal(2, capability.Examples.Count);
        Assert.Equal(3, capability.Configuration.Count);
        Assert.Equal("value1", capability.Configuration["setting1"]);
        Assert.Equal(42, capability.Configuration["setting2"]);
        Assert.Equal(true, capability.Configuration["setting3"]);
    }

    [Fact]
    public void DatabaseCapabilityConfiguration_DefaultConstructor_ShouldInitializeCollections()
    {
        // Act
        var dbConfig = new DatabaseCapabilityConfiguration();

        // Assert
        Assert.NotNull(dbConfig);
        Assert.NotNull(dbConfig.Repositories);
        Assert.NotNull(dbConfig.Operations);
        Assert.Empty(dbConfig.Repositories);
        Assert.Empty(dbConfig.Operations);
    }

    [Fact]
    public void DatabaseCapabilityConfiguration_WithData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var dbConfig = new DatabaseCapabilityConfiguration
        {
            Repositories = new List<string> { "PerfumeRepository", "BrandRepository" },
            Operations = new List<string> { "Create", "Read", "Update", "Delete" }
        };

        // Assert
        Assert.Equal(2, dbConfig.Repositories.Count);
        Assert.Equal(4, dbConfig.Operations.Count);
        Assert.Contains("PerfumeRepository", dbConfig.Repositories);
        Assert.Contains("Create", dbConfig.Operations);
        Assert.Contains("Read", dbConfig.Operations);
        Assert.Contains("Update", dbConfig.Operations);
        Assert.Contains("Delete", dbConfig.Operations);
    }

    [Fact]
    public void FileHandlingCapabilityConfiguration_DefaultConstructor_ShouldInitializeCollections()
    {
        // Act
        var fileConfig = new FileHandlingCapabilityConfiguration();

        // Assert
        Assert.NotNull(fileConfig);
        Assert.NotNull(fileConfig.SupportedFormats);
        Assert.NotNull(fileConfig.InputDirectories);
        Assert.NotNull(fileConfig.MaxFileSize);
        Assert.NotNull(fileConfig.AllowedOperations);
        Assert.Empty(fileConfig.SupportedFormats);
        Assert.Empty(fileConfig.InputDirectories);
        Assert.Empty(fileConfig.AllowedOperations);
    }

    [Fact]
    public void FileHandlingCapabilityConfiguration_WithData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var fileConfig = new FileHandlingCapabilityConfiguration
        {
            SupportedFormats = new List<string> { ".xlsx", ".csv", ".xls" },
            InputDirectories = new List<string> { "Inputs", "Data" },
            MaxFileSize = "10MB",
            AllowedOperations = new List<string> { "Read", "Import", "Export" }
        };

        // Assert
        Assert.Equal(3, fileConfig.SupportedFormats.Count);
        Assert.Equal(2, fileConfig.InputDirectories.Count);
        Assert.Equal("10MB", fileConfig.MaxFileSize);
        Assert.Equal(3, fileConfig.AllowedOperations.Count);
        Assert.Contains(".xlsx", fileConfig.SupportedFormats);
        Assert.Contains("Inputs", fileConfig.InputDirectories);
        Assert.Contains("Import", fileConfig.AllowedOperations);
    }

    [Fact]
    public void AgentConfiguration_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            Agent = new AgentSettings
            {
                Name = "Test Agent",
                Description = "A test agent",
                Capabilities = new List<AgentCapabilityConfig>
                {
                    new AgentCapabilityConfig
                    {
                        Id = "database-access",
                        Name = "Database Access",
                        IsEnabled = true,
                        Configuration = new Dictionary<string, object>
                        {
                            { "repositories", new List<string> { "PerfumeRepository" } },
                            { "operations", new List<string> { "Read", "Write" } }
                        }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var deserializedConfig = JsonSerializer.Deserialize<AgentConfiguration>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal("Test Agent", deserializedConfig.Agent.Name);
        Assert.Equal("A test agent", deserializedConfig.Agent.Description);
        Assert.Single(deserializedConfig.Agent.Capabilities);
        
        var capability = deserializedConfig.Agent.Capabilities.First();
        Assert.Equal("database-access", capability.Id);
        Assert.Equal("Database Access", capability.Name);
        Assert.True(capability.IsEnabled);
        Assert.Equal(2, capability.Configuration.Count);
    }

    [Fact]
    public void AgentConfiguration_LoadFromValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
          "agent": {
            "name": "Test Agent",
            "description": "Test Description",
            "capabilities": [
              {
                "id": "file-processing",
                "name": "File Processing",
                "isEnabled": true,
                "tools": ["ExcelHandler"],
                "examples": ["Import Excel file"],
                "configuration": {
                  "supportedFormats": [".xlsx", ".csv"],
                  "maxFileSize": "5MB"
                }
              }
            ],
            "conversationRules": ["Be helpful", "Be clear"],
            "errorHandling": {
              "fileNotFound": "File not found error message"
            }
          }
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<AgentConfiguration>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Test Agent", config.Agent.Name);
        Assert.Equal("Test Description", config.Agent.Description);
        Assert.Single(config.Agent.Capabilities);
        Assert.Equal(2, config.Agent.ConversationRules.Count);
        Assert.Single(config.Agent.ErrorHandling);
        
        var capability = config.Agent.Capabilities.First();
        Assert.Equal("file-processing", capability.Id);
        Assert.True(capability.IsEnabled);
        Assert.Single(capability.Tools);
        Assert.Equal(2, capability.Configuration.Count);
    }

    [Fact]
    public void AgentCapabilityConfig_CanBeDisabled_ShouldRespectIsEnabledFlag()
    {
        // Arrange
        var enabledCapability = new AgentCapabilityConfig
        {
            Id = "enabled-feature",
            IsEnabled = true
        };

        var disabledCapability = new AgentCapabilityConfig
        {
            Id = "disabled-feature",
            IsEnabled = false
        };

        // Act & Assert
        Assert.True(enabledCapability.IsEnabled);
        Assert.False(disabledCapability.IsEnabled);
    }

    [Fact]
    public void AgentConfiguration_ComplexCapabilityConfiguration_ShouldHandleNestedObjects()
    {
        // Arrange
        var capability = new AgentCapabilityConfig
        {
            Id = "complex-capability",
            Configuration = new Dictionary<string, object>
            {
                { "simpleString", "value" },
                { "simpleNumber", 42 },
                { "simpleBoolean", true },
                { "complexObject", new { nested = "value", count = 5 } },
                { "arrayData", new[] { "item1", "item2", "item3" } }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(capability);
        var deserialized = JsonSerializer.Deserialize<AgentCapabilityConfig>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("complex-capability", deserialized.Id);
        Assert.Equal(5, deserialized.Configuration.Count);
        Assert.Contains("simpleString", deserialized.Configuration.Keys);
        Assert.Contains("complexObject", deserialized.Configuration.Keys);
        Assert.Contains("arrayData", deserialized.Configuration.Keys);
    }
}
