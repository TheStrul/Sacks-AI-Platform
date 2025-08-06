#pragma warning disable OPENAI001 // OpenAI API is in preview and subject to change

using OpenAI;
using OpenAI.Assistants;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Capabilities;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Services;

/// <summary>
/// Simple wrapper around OpenAI Assistants API with integrated FilesystemFunctionHandler tools
/// Always includes FilesystemFunctionHandler capabilities and adds additional tools from configuration
/// </summary>
public class AiAgentBase : IConversationalAgent
{
    private readonly OpenAIClient _openAiClient;
    private readonly AssistantClient _assistantClient;
    private Assistant? _assistant;
    private AiAgentBaseConfiguration? _config;
    private readonly Dictionary<string, AssistantThread> _userThreads = new();

    public AiAgentBase(string apiKey, AiAgentBaseConfiguration? config = null)
    {
        _openAiClient = new OpenAIClient(apiKey);
        _config = config;
        _assistantClient = _openAiClient.GetAssistantClient();
    }

    /// <summary>
    /// Initialize the assistant with configuration from JSON file
    /// Always includes FilesystemFunctionHandler tools plus additional tools from configuration
    /// </summary>
    public async Task InitializeAsync()
    {
        // Load configuration from JSON file
        if (_config == null)
        {
            _config = await AiAgentBaseConfiguration.LoadFromFileAsync(AiAgentBaseConfiguration.GetDefaultConfigPath());
        }

        // Create assistant creation options from configuration
        var creationOptions = new AssistantCreationOptions
        {
            Name = _config.Name,
            Instructions = _config.Instructions
        };

        // Set optional properties if provided
        if (!string.IsNullOrEmpty(_config.Description))
        {
            creationOptions.Description = _config.Description;
        }

        if (_config.Temperature.HasValue)
        {
            creationOptions.Temperature = _config.Temperature.Value;
        }

        // ALWAYS add FilesystemFunctionHandler tools first (core functionality)
        var filesystemTools = FilesystemFunctionHandler.GetFunctionTools();
        var filesystemFunctionNames = new HashSet<string> { "list_files", "list_directories", "get_file_info", "search_files" };
        
        foreach (var tool in filesystemTools)
        {
            creationOptions.Tools.Add(tool);
        }

        // Add additional Function tools from configuration (excluding those already provided by FilesystemFunctionHandler)
        if (_config.Functions != null && _config.Functions.Count > 0)
        {
            foreach (var functionDef in _config.Functions)
            {
                // Skip functions that are already provided by FilesystemFunctionHandler
                if (filesystemFunctionNames.Contains(functionDef.Name))
                {
                    Console.WriteLine($"⚠️  Skipping duplicate function '{functionDef.Name}' - already provided by FilesystemFunctionHandler");
                    continue;
                }

                var functionTool = new FunctionToolDefinition()
                {
                    FunctionName = functionDef.Name,
                    Description = functionDef.Description,
                    Parameters = BinaryData.FromObjectAsJson(functionDef.Parameters)
                };

                creationOptions.Tools.Add(functionTool);
                Console.WriteLine($"✅ Added additional function tool: '{functionDef.Name}'");
            }
        }

        var result = await _assistantClient.CreateAssistantAsync(
            _config.Model,
            creationOptions);
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

        // Wait for completion and handle function calls
        while (run.Value.Status == RunStatus.InProgress || run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.RequiresAction)
        {
            if (run.Value.Status == RunStatus.RequiresAction)
            {
                // Handle function calls - delegate to appropriate tool handlers
                Console.WriteLine("🔧 Function call requested by assistant");
                
                try
                {
                    // Get the required actions (function calls) from the run
                    var requiredActions = run.Value.RequiredActions;
                    var toolOutputs = new List<ToolOutput>();

                    foreach (var action in requiredActions)
                    {
                        // Use dynamic typing to handle the action since the exact type may vary
                        dynamic functionCall = action;
                        
                        if (functionCall != null && functionCall.FunctionName != null)
                        {
                            string functionName = functionCall.FunctionName;
                            string arguments = functionCall.FunctionArguments ?? "{}";
                            string toolCallId = functionCall.ToolCallId ?? "";
                            
                            Console.WriteLine($"📞 Calling function: {functionName}");
                            Console.WriteLine($"📝 Arguments: {arguments}");
                            
                            // Delegate to the appropriate tool handler
                            string result = await ExecuteFunctionCallAsync(functionName, arguments);
                            
                            toolOutputs.Add(new ToolOutput(toolCallId, result));
                        }
                    }

                    // Submit the tool outputs back to the assistant
                    if (toolOutputs.Count > 0)
                    {
                        run = await _assistantClient.SubmitToolOutputsToRunAsync(thread.Id, run.Value.Id, toolOutputs);
                        Console.WriteLine($"✅ Submitted {toolOutputs.Count} tool outputs");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Function execution failed: {ex.Message}");
                    
                    // Cancel the run and provide an error message
                    try
                    {
                        await _assistantClient.CancelRunAsync(thread.Id, run.Value.Id);
                        await _assistantClient.CreateMessageAsync(thread.Id, MessageRole.Assistant, 
                            ["I encountered an error while trying to process your request. Please try again."]);
                    }
                    catch { }
                    
                    break;
                }
            }
            else
            {
                await Task.Delay(1000);
                run = await _assistantClient.GetRunAsync(thread.Id, run.Value.Id);
            }
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

    /// <summary>
    /// Get or create a thread for the specified user
    /// </summary>
    private async Task<AssistantThread> GetOrCreateThreadAsync(string userId)
    {
        if (!_userThreads.TryGetValue(userId, out var thread))
        {
            var newThread = await _assistantClient.CreateThreadAsync();
            thread = newThread.Value;
            _userThreads[userId] = thread;
        }
        return thread;
    }

    /// <summary>
    /// Execute a function call by delegating to the appropriate tool handler
    /// </summary>
    private async Task<string> ExecuteFunctionCallAsync(string functionName, string argumentsJson)
    {
        try
        {
            // Check if this is a filesystem function - delegate to FilesystemFunctionHandler
            var filesystemFunctions = new HashSet<string> { "list_files", "list_directories", "get_file_info", "search_files" };
            
            if (filesystemFunctions.Contains(functionName))
            {
                Console.WriteLine($"🗂️ Delegating '{functionName}' to FilesystemFunctionHandler");
                return await FilesystemFunctionHandler.ExecuteFunctionAsync(functionName, argumentsJson);
            }
            
            // For additional functions from configuration, you would delegate to other handlers here
            // Example: if (functionName == "custom_function") return await CustomHandler.ExecuteAsync(argumentsJson);
            
            // If no handler found, return error
            var errorMessage = $"Function '{functionName}' is not supported. Available functions: {string.Join(", ", filesystemFunctions)}";
            Console.WriteLine($"❌ {errorMessage}");
            return $"{{\"error\": \"{errorMessage}\"}}";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing function '{functionName}': {ex.Message}";
            Console.WriteLine($"❌ {errorMessage}");
            return $"{{\"error\": \"{errorMessage}\"}}";
        }
    }

    /// <summary>
    /// Get conversation history for a user
    /// </summary>
    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId = "user")
    {
        var history = new List<ConversationMessage>();
        
        if (_userThreads.TryGetValue(userId, out var thread))
        {
            var messages = _assistantClient.GetMessagesAsync(thread.Id);
            await foreach (var message in messages)
            {
                var content = message.Content?.FirstOrDefault()?.Text ?? "";
                history.Add(new ConversationMessage
                {
                    UserId = userId,
                    Content = content,
                    Role = message.Role == MessageRole.User ? "user" : "agent",
                    Timestamp = message.CreatedAt.DateTime
                });
            }
        }
        
        // Reverse to get chronological order (oldest first)
        history.Reverse();
        return history;
    }

    /// <summary>
    /// Clear conversation history for a user
    /// </summary>
    public async Task ClearConversationAsync(string userId = "user")
    {
        if (_userThreads.TryGetValue(userId, out var thread))
        {
            // Create a new thread to effectively clear history
            var newThread = await _assistantClient.CreateThreadAsync();
            _userThreads[userId] = newThread.Value;
        }
    }

    /// <summary>
    /// Teach the agent a new rule or behavior
    /// </summary>
    public async Task TeachAsync(string rule, string example, string userId = "user")
    {
        // For now, add the teaching as a system message to the conversation
        // This is a simplified implementation - in a full system you might store these in a knowledge base
        var teachingMessage = $"New rule learned: {rule}\nExample: {example}";
        
        var thread = await GetOrCreateThreadAsync(userId);
        await _assistantClient.CreateMessageAsync(thread.Id, MessageRole.User, 
            [$"Please remember this rule: {rule}. Here's an example: {example}"]);
    }

    /// <summary>
    /// Get available capabilities and commands
    /// </summary>
    public Task<List<AgentCapability>> GetCapabilitiesAsync()
    {
        var capabilities = new List<AgentCapability>();
        
        // Add filesystem capabilities
        capabilities.Add(new AgentCapability
        {
            Name = "File System Operations",
            Description = "I can list files, search directories, and get file information",
            Examples = new List<string> { "list files", "search files", "file info", "list directories" }
        });
        
        // Add conversation capabilities
        capabilities.Add(new AgentCapability
        {
            Name = "Conversation Management",
            Description = "I can maintain conversation history and learn new rules",
            Examples = new List<string> { "clear history", "show history", "teach rule" }
        });
        
        // Add any additional capabilities from configuration
        if (_config?.Functions != null && _config.Functions.Count > 0)
        {
            var additionalCommands = _config.Functions.Select(f => f.Name).ToList();
            capabilities.Add(new AgentCapability
            {
                Name = "Additional Functions",
                Description = "Custom functions defined in configuration",
                Examples = additionalCommands
            });
        }
        
        return Task.FromResult(capabilities);
    }

    public void Dispose()
    {
        // OpenAIClient does not implement IDisposable in current version
        // No cleanup needed for now
    }
}
