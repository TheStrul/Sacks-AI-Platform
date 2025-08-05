using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Xunit;
using DotNetEnv;
using System.Text.Json;

namespace SacksAIPlatform.Tests.Infrastructures.AI;

/// <summary>
/// Tests for GenericLLMAgent functionality
/// Tests the agent's capability loading, conversation handling, and configuration management
/// </summary>
public class GenericLLMAgentTests
{
    private readonly IIntentRecognitionService _intentService;
    private readonly IConfiguration _configuration;

    public GenericLLMAgentTests()
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
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _intentService = new OpenAIIntentRecognitionService(logger, _configuration);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithValidConfiguration()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;

        // Act
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);

        // Assert
        Assert.NotNull(agent);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldReturnConfiguredCapabilities()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);

        // Act
        var capabilities = await agent.GetCapabilitiesAsync();

        // Assert
        Assert.NotNull(capabilities);
        Assert.NotEmpty(capabilities);
        
        // Check that capabilities have required properties
        foreach (var capability in capabilities)
        {
            Assert.NotNull(capability.Name);
            Assert.NotEmpty(capability.Name);
            Assert.NotNull(capability.Description);
            Assert.NotEmpty(capability.Description);
            Assert.True(capability.Available);
        }
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidMessage_ShouldReturnResponse()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var message = "Hello, can you help me?";
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);
        Assert.Equal(AgentResponseType.Text, response.Type);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithGreeting_ShouldReturnFriendlyResponse()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var message = "Hi there!";
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        Assert.True(response.Message.Length > 5); // Should be a meaningful response
        Assert.Equal(AgentResponseType.Text, response.Type);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithComplexQuery_ShouldHandleGracefully()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var message = "I need to analyze perfume inventory data and generate insights about sales trends";
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        Assert.True(response.Message.Length > 10); // Should be a detailed response
        Assert.Equal(AgentResponseType.Text, response.Type);
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ShouldReturnEmptyList()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var userId = "test-user";

        // Act
        var history = await agent.GetConversationHistoryAsync(userId);

        // Assert
        Assert.NotNull(history);
        Assert.Empty(history); // Currently returns empty list
    }

    [Fact]
    public async Task ClearConversationAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var userId = "test-user";

        // Act & Assert
        await agent.ClearConversationAsync(userId); // Should not throw
    }

    [Fact]
    public async Task TeachAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var rule = "Always greet users politely";
        var example = "Hello! How can I help you today?";
        var userId = "test-user";

        // Act & Assert
        await agent.TeachAsync(rule, example, userId); // Should not throw
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task ProcessMessageAsync_WithEmptyOrWhitespaceMessage_ShouldHandleGracefully(string message)
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var agent = new GenericLLMAgent(logger, _configuration, _intentService);
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithCustomActionHandler_ShouldUseCustomHandler()
    {
        // Arrange
        var logger = NullLogger<GenericLLMAgent>.Instance;
        var customMessage = "Custom handler response";
        
        Func<string, string, Task<AgentResponse>> customHandler = async (message, userId) =>
        {
            await Task.Delay(1); // Simulate async work
            return new AgentResponse
            {
                Message = customMessage,
                Type = AgentResponseType.Text
            };
        };

        var agent = new GenericLLMAgent(logger, _configuration, _intentService, customHandler);
        var message = "Test message";
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(customMessage, response.Message);
        Assert.Equal(AgentResponseType.Text, response.Type);
    }
}
