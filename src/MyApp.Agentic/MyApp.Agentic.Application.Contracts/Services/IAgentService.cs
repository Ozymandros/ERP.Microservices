using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Agentic.Application.Contracts.Services;

/// <summary>
/// Defines service operations for managing AI agents and their conversation sessions.
/// This service handles agent lifecycle (CRUD), message processing, and session management
/// within the Agentic microservice domain.
/// </summary>
/// <remarks>
/// <para>
/// The agent service provides two primary areas of functionality:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <strong>Agent Management:</strong> Create, retrieve, update, and delete agent definitions.
/// Agents can be owned by specific users and configured with different capabilities.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Session and Message Handling:</strong> Start conversation sessions, process messages,
/// and manage the lifecycle of agent interactions with users.
/// </description>
/// </item>
/// </list>
/// <para>
/// All session-related operations enforce user authorization to ensure users can only
/// access their own sessions and interact with agents they have permission to use.
/// </para>
/// </remarks>
public interface IAgentService
{
    /// <summary>
    /// Retrieves a single agent by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to retrieve.</param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the agent details
    /// as an <see cref="AgentDto"/>, or <c>null</c> if no agent with the specified ID exists.
    /// </returns>
    /// <remarks>
    /// This method returns the full agent details including configuration, capabilities,
    /// and metadata. Use <see cref="ListAsync"/> for lightweight agent listings.
    /// </remarks>
    Task<AgentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all available agents in the system.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a collection
    /// of <see cref="AgentListDto"/> objects representing all agents.
    /// </returns>
    /// <remarks>
    /// Returns a lightweight representation of agents suitable for list views and selection UI.
    /// For detailed agent information, use <see cref="GetByIdAsync"/>.
    /// </remarks>
    Task<IEnumerable<AgentListDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a filtered list of agents owned by a specific user.
    /// </summary>
    /// <param name="ownerUserId">
    /// The user identifier to filter by. If <c>null</c>, returns all agents regardless of ownership.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a collection
    /// of <see cref="AgentListDto"/> objects representing agents owned by the specified user.
    /// </returns>
    /// <remarks>
    /// This method is useful for displaying a user's personal agents or for administrative
    /// purposes when filtering agents by ownership.
    /// </remarks>
    Task<IEnumerable<AgentListDto>> ListByOwnerAsync(string? ownerUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new agent with the specified configuration.
    /// </summary>
    /// <param name="dto">The data transfer object containing the agent creation details.</param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the created
    /// agent as an <see cref="AgentDto"/> with its assigned unique identifier.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is <c>null</c>.</exception>
    /// <exception cref="ValidationException">
    /// Thrown when the provided agent data fails validation rules (e.g., invalid name, missing required fields).
    /// </exception>
    /// <remarks>
    /// The created agent will be assigned a new unique identifier and default configuration values
    /// will be applied where not explicitly specified in the DTO.
    /// </remarks>
    Task<AgentDto> CreateAsync(CreateAgentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing agent's configuration and properties.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to update.</param>
    /// <param name="dto">The data transfer object containing the updated agent details.</param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the updated
    /// agent as an <see cref="AgentDto"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is <c>null</c>.</exception>
    /// <exception cref="NotFoundException">
    /// Thrown when no agent with the specified <paramref name="id"/> exists.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the updated agent data fails validation rules.
    /// </exception>
    /// <remarks>
    /// Only the properties specified in the DTO will be updated. Unspecified properties
    /// will retain their current values.
    /// </remarks>
    Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent and all associated data from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to delete.</param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no agent with the specified <paramref name="id"/> exists.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This operation is permanent and will delete the agent along with any associated
    /// configuration and metadata. Active sessions using this agent may be affected.
    /// </para>
    /// <para>
    /// Consider implementing soft delete or archiving for production scenarios to preserve
    /// conversation history and audit trails.
    /// </para>
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a single message through an agent and returns the agent's response.
    /// </summary>
    /// <param name="request">The message processing request containing the message and agent context.</param>
    /// <param name="authenticatedUserId">
    /// The identifier of the authenticated user making the request. Used for authorization and auditing.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the agent's
    /// response as a <see cref="ProcessAgentMessageResponse"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> or <paramref name="authenticatedUserId"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the authenticated user does not have permission to use the specified agent.
    /// </exception>
    /// <remarks>
    /// This method is designed for stateless, one-off message processing. For conversational
    /// interactions with context preservation, use <see cref="StartSessionAsync"/> and
    /// <see cref="SendMessageAsync"/> instead.
    /// </remarks>
    Task<ProcessAgentMessageResponse> ProcessMessageAsync(
        ProcessAgentMessageRequest request,
        string authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a new conversation session with an agent.
    /// </summary>
    /// <param name="request">The session start request containing the agent ID and initial configuration.</param>
    /// <param name="authenticatedUserId">
    /// The identifier of the authenticated user starting the session. The session will be owned by this user.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the session
    /// details as a <see cref="StartSessionResponse"/> including the unique session identifier.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> or <paramref name="authenticatedUserId"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Thrown when the specified agent does not exist.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the authenticated user does not have permission to create sessions with the specified agent.
    /// </exception>
    /// <remarks>
    /// Sessions maintain conversation context and history, allowing for multi-turn interactions
    /// with the agent. Use <see cref="SendMessageAsync"/> to send messages within the session
    /// and <see cref="EndSessionAsync"/> to properly terminate the session when finished.
    /// </remarks>
    Task<StartSessionResponse> StartSessionAsync(
        StartSessionRequest request,
        string authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message within an existing conversation session and receives the agent's response.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the active session.</param>
    /// <param name="request">The message request containing the user's message content.</param>
    /// <param name="authenticatedUserId">
    /// The identifier of the authenticated user sending the message. Must match the session owner.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the agent's
    /// response as a <see cref="SendMessageResponse"/> along with any updated session metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> or <paramref name="authenticatedUserId"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Thrown when no session with the specified <paramref name="sessionId"/> exists.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the authenticated user is not the owner of the specified session.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the session has been ended or is in an invalid state for message processing.
    /// </exception>
    /// <remarks>
    /// Messages sent through this method benefit from the full conversation context maintained
    /// by the session, allowing the agent to provide contextually relevant responses based on
    /// the conversation history.
    /// </remarks>
    Task<SendMessageResponse> SendMessageAsync(
        Guid sessionId,
        SendMessageRequest request,
        string authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the details of an existing conversation session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to retrieve.</param>
    /// <param name="authenticatedUserId">
    /// The identifier of the authenticated user requesting the session details.
    /// Must match the session owner.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the session
    /// details as a <see cref="SessionDetailsResponse"/>, or <c>null</c> if the session does not exist.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the authenticated user is not the owner of the specified session.
    /// </exception>
    /// <remarks>
    /// The returned session details include the full conversation history, session metadata,
    /// and current session state. This method is useful for resuming sessions or displaying
    /// conversation history to users.
    /// </remarks>
    Task<SessionDetailsResponse?> GetSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all conversation sessions for the authenticated user.
    /// </summary>
    /// <param name="authenticatedUserId">
    /// The identifier of the authenticated user whose sessions should be listed.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a collection
    /// of <see cref="SessionListItemDto"/> objects representing the user's sessions.
    /// </returns>
    /// <remarks>
    /// Returns a lightweight representation of sessions suitable for list views. Sessions
    /// are typically ordered by most recent activity. For full session details including
    /// conversation history, use <see cref="GetSessionAsync"/>.
    /// </remarks>
    Task<IEnumerable<SessionListItemDto>> ListSessionsAsync(
        string authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates an active conversation session and performs cleanup operations.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to end.</param>
    /// <param name="authenticatedUserId">
    /// The identifier of the authenticated user ending the session. Must match the session owner.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the asynchronous session termination operation.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no session with the specified <paramref name="sessionId"/> exists.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the authenticated user is not the owner of the specified session.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Ending a session marks it as completed and may trigger cleanup operations such as
    /// archiving conversation history or releasing resources. Once ended, a session cannot
    /// be used to send additional messages.
    /// </para>
    /// <para>
    /// The conversation history is preserved even after the session ends and can be retrieved
    /// using <see cref="GetSessionAsync"/> for audit and review purposes.
    /// </para>
    /// </remarks>
    Task EndSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        CancellationToken cancellationToken = default);
}
