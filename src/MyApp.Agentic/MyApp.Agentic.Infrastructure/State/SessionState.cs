namespace MyApp.Agentic.Infrastructure.State;

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SessionState
{
    public Guid SessionId { get; set; }
    public Guid AgentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<ConversationMessage> Messages { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}