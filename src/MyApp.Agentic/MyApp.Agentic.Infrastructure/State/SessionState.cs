namespace MyApp.Agentic.Infrastructure.State;

/// <summary>Represents a single message within a persisted conversation session.</summary>
public class ConversationMessage
{
    /// <summary>Gets or sets the role of the message author (e.g. "user" or "assistant").</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>Gets or sets the text content of the message.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the UTC timestamp when the message was recorded.</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>Dapr state-store document representing the conversation history for an agent–user pair.</summary>
public class SessionState
{
    /// <summary>Gets or sets the unique identifier of the session.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the identifier of the agent associated with the session.</summary>
    public Guid AgentId { get; set; }
    /// <summary>Gets or sets the identifier of the user who owns the session.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Gets or sets the ordered list of conversation messages in this session.</summary>
    public List<ConversationMessage> Messages { get; set; } = new();
    /// <summary>Gets or sets the UTC timestamp of the most recent update to this session.</summary>
    public DateTime LastUpdated { get; set; }
}