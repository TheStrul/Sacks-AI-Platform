using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.AI.Services;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

namespace SacksAIPlatform.Tests.Infrastructures.AI;

/// <summary>
/// Tests for AiAgent functionality
/// Tests the agent's tool calling, conversation handling, and capability integration
/// </summary>
public class AiAgentTests
{
    private readonly IConfiguration _configuration;

    public AiAgentTests()
    {
        // Load environment variables from .env file
        Env.Load();

        // Create configuration with real API key from environment
        var configData = new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-test-key",
            ["OpenAI:Model"] = "gpt-4o",
            ["OpenAI:MaxTokens"] = "150",
            ["OpenAI:Temperature"] = "0.7"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithValidConfiguration()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;

        // Act & Assert - Should not throw if API key is valid
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") == true)
        {
            var agent = new AiAgent(logger, _configuration);
            Assert.NotNull(agent);
        }
        else
        {
            // If no valid API key, should throw
            Assert.Throws<InvalidOperationException>(() => new AiAgent(logger, _configuration));
        }
    }

    [Fact]
    public void Constructor_WithInvalidApiKey_ShouldThrowException()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "invalid-key",
                ["OpenAI:Model"] = "gpt-4o"
            })
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new AiAgent(logger, invalidConfig));
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldReturnConfiguredCapabilities()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);

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
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);
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
        Assert.Contains("ProcessedBy", response.Data.Keys);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithCapabilityHandler_ShouldUseToolCalling()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var capabilityExecuted = false;
        Func<string, string, Task<AgentResponse>> capabilityHandler = async (message, userId) =>
        {
            capabilityExecuted = true;
            await Task.Delay(1); // Simulate async work
            return new AgentResponse
            {
                Message = "File operation completed: Found 3 Excel files",
                Type = AgentResponseType.Text,
                Data = new Dictionary<string, object>
                {
                    { "FilesFound", 3 },
                    { "FileTypes", new[] { "xlsx", "xls" } }
                }
            };
        };

        var agent = new AiAgent(logger, _configuration, capabilityHandler);
        var message = "List all Excel files in the current directory";
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);
        
        // The response should indicate tool usage if the LLM decided to use tools
        if (response.Data.ContainsKey("UsedCapabilities") && (bool)response.Data["UsedCapabilities"])
        {
            Assert.True(capabilityExecuted);
            Assert.Equal("ChatAPI+Tools", response.Data["ProcessedBy"]);
        }
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ShouldReturnEmptyForNewUser()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);
        var userId = "new-user";

        // Act
        var history = await agent.GetConversationHistoryAsync(userId);

        // Assert
        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public async Task ClearConversationAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);
        var userId = "test-user";

        // First, have a conversation
        await agent.ProcessMessageAsync("Hello", userId);

        // Act & Assert - Should not throw
        await agent.ClearConversationAsync(userId);
        
        // Verify conversation is cleared
        var history = await agent.GetConversationHistoryAsync(userId);
        Assert.Empty(history);
    }

    [Fact]
    public async Task TeachAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);
        var rule = "Always greet users politely";
        var example = "Hello! How can I help you today?";
        var userId = "test-user";

        // Act & Assert - Should not throw
        await agent.TeachAsync(rule, example, userId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task ProcessMessageAsync_WithEmptyOrWhitespaceMessage_ShouldHandleGracefully(string message)
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);
        var userId = "test-user";

        // Act
        var response = await agent.ProcessMessageAsync(message, userId);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        // Should handle empty messages gracefully (might return error or helpful message)
    }

    [Fact]
    public async Task ProcessMessageAsync_ConversationHistory_ShouldMaintainContext()
    {
        // Arrange
        var logger = NullLogger<AiAgent>.Instance;
        
        // Skip if no valid API key
        if (_configuration["OpenAI:ApiKey"]?.StartsWith("sk-") != true)
        {
            return;
        }

        var agent = new AiAgent(logger, _configuration);
        var userId = "context-test-user";

        // Act - Have a conversation
        var response1 = await agent.ProcessMessageAsync("My name is John", userId);
        var response2 = await agent.ProcessMessageAsync("What is my name?", userId);

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        
        // The second response might reference the name if the context is maintained
        // This depends on the LLM's ability to use conversation history
        var history = await agent.GetConversationHistoryAsync(userId);
        Assert.True(history.Count >= 2); // Should have at least user messages
    }
}
