namespace SacksAIPlatform.InfrastructuresLayer.AI.Models;

/// <summary>
/// Response from the conversational AI agent
/// </summary>
public class AgentResponse
{
    public string Message { get; set; } = string.Empty;
    public AgentResponseType Type { get; set; } = AgentResponseType.Text;
    public List<AgentAction> Actions { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
    public bool RequiresUserConfirmation { get; set; }
    public string? ConfirmationPrompt { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Type of agent response
/// </summary>
public enum AgentResponseType
{
    Text,
    DataPresentation,
    ActionConfirmation,
    Question,
    Error,
    Teaching
}

/// <summary>
/// Action that the agent wants to perform
/// </summary>
public class AgentAction
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool Executed { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? Result { get; set; }
}

/// <summary>
/// Conversation message in the chat history
/// </summary>
public class ConversationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "agent"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Agent capability description
/// </summary>
public class AgentCapability
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
    public bool Available { get; set; } = true;
}

/// <summary>
/// Intent recognition result
/// </summary>
public class Intent
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, string> Entities { get; set; } = new();
    public string OriginalText { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Learning rule stored by the agent
/// </summary>
public class LearningRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Rule { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UsageCount { get; set; }
    public bool Active { get; set; } = true;
}
