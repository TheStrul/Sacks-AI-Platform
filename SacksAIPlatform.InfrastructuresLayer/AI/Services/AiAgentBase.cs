#pragma warning disable OPENAI001 // OpenAI API is in preview and subject to change

using OpenAI;
using OpenAI.Assistants;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Services;

/// <summary>
/// Simple wrapper around OpenAI Assistants API
/// </summary>
public class AiAgentBase : IConversationalAgent
{
    private readonly OpenAIClient _openAiClient;
    private readonly AssistantClient _assistantClient;
    private Assistant? _assistant;
    private readonly Dictionary<string, AssistantThread> _userThreads = new();

    public AiAgentBase(string apiKey)
    {
        _openAiClient = new OpenAIClient(apiKey);
        _assistantClient = _openAiClient.GetAssistantClient();
    }

    /// <summary>
    /// Initialize the assistant
    /// </summary>
    public async Task InitializeAsync()
    {
        var result = await _assistantClient.CreateAssistantAsync(
            "gpt-4o",
            new AssistantCreationOptions
            {
                Name = "Sacks AI Assistant",
                Instructions = "You are a helpful AI assistant for the Sacks AI Platform."
            });
        _assistant = result.Value;
    }

    public async Task<AgentResponse> ProcessMessageAsync(string userMessage, string userId = "user")
    {
        if (_assistant == null)
            throw new InvalidOperationException("Assistant not initialized. Call InitializeAsync first.");

        // Get or create thread for user
        var thread = await GetOrCreateThreadAsync(userId);

        // Add user message to thread
        await _assistantClient.CreateMessageAsync(thread.Id, MessageRole.User, [userMessage]);

        // Create and run the assistant
        var run = await _assistantClient.CreateRunAsync(thread.Id, _assistant.Id);

        // Wait for completion
        while (run.Value.Status == RunStatus.InProgress || run.Value.Status == RunStatus.Queued)
        {
            await Task.Delay(1000);
            run = await _assistantClient.GetRunAsync(thread.Id, run.Value.Id);
        }

        // Get the latest message
        var messages = _assistantClient.GetMessagesAsync(thread.Id);
        ThreadMessage? latestMessage = null;
        await foreach (var message in messages)
        {
            latestMessage = message;
            break;
        }

        if (latestMessage?.Content?.FirstOrDefault() is MessageContent textContent && textContent.Text != null)
        {
            return new AgentResponse
            {
                Message = textContent.Text,
                Timestamp = DateTime.UtcNow
            };
        }

        return new AgentResponse
        {
            Message = "No response generated",
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId = "user")
    {
        if (!_userThreads.TryGetValue(userId, out var thread))
            return new List<ConversationMessage>();

        var messages = new List<ConversationMessage>();
        var asyncMessages = _assistantClient.GetMessagesAsync(thread.Id);

        await foreach (var message in asyncMessages)
        {
            if (message.Content.FirstOrDefault() is MessageContent textContent && textContent.Text != null)
            {
                messages.Add(new ConversationMessage
                {
                    Role = message.Role.ToString(),
                    Content = textContent.Text,
                    Timestamp = message.CreatedAt.DateTime
                });
            }
        }

        return messages.OrderBy(m => m.Timestamp).ToList();
    }

    public async Task ClearConversationAsync(string userId = "user")
    {
        if (_userThreads.TryGetValue(userId, out var thread))
        {
            await _assistantClient.DeleteThreadAsync(thread.Id);
            _userThreads.Remove(userId);
        }
    }

    public Task TeachAsync(string rule, string example, string userId = "user")
    {
        // Simple implementation - could be enhanced to modify assistant instructions
        return Task.CompletedTask;
    }

    public Task<List<AgentCapability>> GetCapabilitiesAsync()
    {
        var capabilities = new List<AgentCapability>
        {
            new AgentCapability
            {
                Name = "Natural Language Conversation",
                Description = "Engage in natural language conversations using OpenAI GPT-4"
            }
        };

        return Task.FromResult(capabilities);
    }

    private async Task<AssistantThread> GetOrCreateThreadAsync(string userId)
    {
        if (_userThreads.TryGetValue(userId, out var existingThread))
            return existingThread;

        var result = await _assistantClient.CreateThreadAsync();
        var thread = result.Value;
        _userThreads[userId] = thread;
        return thread;
    }

    public void Dispose()
    {
        // OpenAIClient doesn't implement IDisposable in current version
        // No cleanup needed
    }
}
