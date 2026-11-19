namespace MyApp.Projects.Application.Contracts.Ports;

public interface IOrdersCreationPort
{
    Task<ProjectOrderSnapshot> CreateWithReservationAsync(
        CreateProjectOrderWithReservationRequest request,
        CancellationToken cancellationToken = default);
}