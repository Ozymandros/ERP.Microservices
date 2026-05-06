using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Contracts.Services;

public interface IAgentService
{
    Task<AgentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentListDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentListDto>> ListByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    Task<AgentDto> CreateAsync(CreateAgentDto dto, CancellationToken cancellationToken = default);
    Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProcessAgentMessageResponse> ProcessMessageAsync(
        ProcessAgentMessageRequest request,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<StartSessionResponse> StartSessionAsync(
        StartSessionRequest request,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<SendMessageResponse> SendMessageAsync(
        Guid sessionId,
        SendMessageRequest request,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<SessionDetailsResponse?> GetSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<SessionListItemDto>> ListSessionsAsync(
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task EndSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        CancellationToken cancellationToken = default);
}