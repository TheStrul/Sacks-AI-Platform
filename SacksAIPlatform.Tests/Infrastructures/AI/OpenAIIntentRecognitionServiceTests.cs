using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

namespace SacksAIPlatform.Tests.Infrastructures.AI;

/// <summary>
/// Tests for OpenAIIntentRecognitionService functionality
/// Note: These tests use real OpenAI API calls with the configured API key
/// </summary>
public class OpenAIIntentRecognitionServiceTests
{
    private readonly IIntentRecognitionService _intentService;

    public OpenAIIntentRecognitionServiceTests()
    {
        // Load environment variables from .env file
        Env.Load();
        
        // Create logger using NullLogger for tests
        var logger = NullLogger<OpenAIIntentRecognitionService>.Instance;

        // Create configuration with real API key from environment
        var configData = new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-test-key",
            ["OpenAI:Model"] = "gpt-3.5-turbo",
            ["OpenAI:MaxTokens"] = "150",
            ["OpenAI:Temperature"] = "0.7"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _intentService = new OpenAIIntentRecognitionService(logger, configuration);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Arrange
        var logger = NullLogger<OpenAIIntentRecognitionService>.Instance;
        var configData = new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = "test-key"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var service = new OpenAIIntentRecognitionService(logger, configuration);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var logger = NullLogger<OpenAIIntentRecognitionService>.Instance;
        var configData = new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = ""
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new OpenAIIntentRecognitionService(logger, configuration));
    }

    [Fact]
    public void Constructor_WithPlaceholderApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var logger = NullLogger<OpenAIIntentRecognitionService>.Instance;
        var configData = new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = "your-openai-api-key-here"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new OpenAIIntentRecognitionService(logger, configuration));
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithGreeting_ShouldReturnValidResponse()
    {
        // Arrange
        var message = "Hello there!";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(message, result.OriginalText);
        Assert.True(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.NotNull(result.Entities);
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithImportRequest_ShouldReturnRelevantResponse()
    {
        // Arrange
        var message = "I need to import a CSV file";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(message, result.OriginalText);
        Assert.True(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        // OpenAI should understand and respond to import requests
        Assert.True(result.Description.Length > 10); // Should be a meaningful response
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithDataQuery_ShouldReturnSearchResponse()
    {
        // Arrange
        var message = "Show me all perfume data";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(message, result.OriginalText);
        Assert.True(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        // Should provide a meaningful response to data queries
        Assert.True(result.Description.Length > 10);
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithAnalysisRequest_ShouldReturnAnalysisResponse()
    {
        // Arrange
        var message = "Can you analyze the sales statistics?";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(message, result.OriginalText);
        Assert.True(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.True(result.Description.Length > 10);
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithHelpRequest_ShouldReturnHelpResponse()
    {
        // Arrange
        var message = "What can you do for me?";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(message, result.OriginalText);
        Assert.True(result.Confidence >= 0.0 && result.Confidence <= 1.0);
        Assert.True(result.Description.Length > 10);
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithEmptyMessage_ShouldReturnDefaultResponse()
    {
        // Arrange
        var message = "";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.NotEmpty(result.Description);
    }

    [Fact]
    public async Task RecognizeIntentAsync_WithWhitespaceMessage_ShouldReturnDefaultResponse()
    {
        // Arrange
        var message = "   \t\n   ";
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.NotEmpty(result.Description);
    }

    [Theory]
    [InlineData("hi")]
    [InlineData("good morning")]
    [InlineData("hey there")]
    public async Task RecognizeIntentAsync_WithVariousGreetings_ShouldReturnValidResponse(string greeting)
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(greeting, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(greeting, result.OriginalText);
        Assert.True(result.Description.Length > 5); // Should be a meaningful response
    }

    [Theory]
    [InlineData("import excel")]
    [InlineData("load csv file")]
    [InlineData("upload data")]
    public async Task RecognizeIntentAsync_WithFileOperations_ShouldReturnValidResponse(string message)
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _intentService.RecognizeIntentAsync(message, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Description);
        Assert.NotEmpty(result.Name);
        Assert.Equal(message, result.OriginalText);
        Assert.True(result.Description.Length > 10); // Should be a meaningful response
    }
}
