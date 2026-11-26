using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Ports;
using MyApp.Projects.Application.Internals;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Projects.Domain.Services;
using MyApp.Shared.Domain.Constants;

namespace MyApp.Projects.Application.UseCases.Posting;

public sealed class CreateItemConsumptionUseCase(
    IProjectRepository projects,
    IProjectPlanningLineRepository lines,
    IProjectConsumptionOrderRepository consumptionOrders,
    IOrdersCreationPort ordersCreation,
    IMapper mapper,
    IProjectsTransactionBoundary boundary,
    ILogger<CreateItemConsumptionUseCase> logger)
{
    public async Task<ConsumptionOrderDto> ExecuteAsync(
        Guid projectId,
        CreateItemConsumptionDto dto,
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {projectId} not found");
        project.EnsureCanPost();

        if (dto.Lines.Count == 0)
            throw new ArgumentException("At least one line is required.");

        var requested = dto.Lines
            .GroupBy(l => l.PlanningLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var planningLines = await lines.GetByIdsAsync(requested.Keys, cancellationToken);
        if (planningLines.Count != requested.Count || planningLines.Any(l => l.ProjectId != projectId))
            throw new ArgumentException("One or more planning lines do not belong to this project.");

        foreach (var line in planningLines)
        {
            if (line.Kind != ProjectLineKind.Item || line.ProductId is null)
                throw new ArgumentException($"Planning line {line.LineNo} is not an item line.");
            if (line.WarehouseId.HasValue && line.WarehouseId != dto.WarehouseId)
                throw new ArgumentException($"Planning line {line.LineNo} is planned for a different warehouse.");

            line.ReserveForConsumption(requested[line.Id]);
        }

        var order = await ordersCreation.CreateWithReservationAsync(
            new CreateProjectOrderWithReservationRequest(
                $"{project.ProjectNumber}-CONS",
                OrderTypes.Outbound,
                null,
                null,
                project.Id,
                DocumentSources.Project,
                dto.WarehouseId,
                DateTime.UtcNow,
                dto.DestinationAddress,
                planningLines.Select(l => new ProjectOrderLineRequest(l.ProductId!.Value, requested[l.Id])).ToList()),
            cancellationToken);

        var consumption = new ProjectConsumptionOrder(Guid.NewGuid())
        {
            ProjectId = projectId,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            WarehouseId = dto.WarehouseId,
            Status = ProjectConsumptionStatus.Pending,
            PostingDate = (dto.PostingDate ?? DateTime.UtcNow).Date
        };
        foreach (var line in planningLines)
        {
            consumption.Lines.Add(new ProjectConsumptionOrderLine(Guid.NewGuid())
            {
                ConsumptionOrderId = consumption.Id,
                PlanningLineId = line.Id,
                ProductId = line.ProductId!.Value,
                Quantity = requested[line.Id]
            });
        }

        try
        {
            await consumptionOrders.AddAsync(consumption);
            await boundary.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Order {OrderId} was created in Orders for project {ProjectNumber} but could not be tracked locally; cancel it in Orders",
                order.Id, project.ProjectNumber);
            throw;
        }

        logger.LogInformation(
            "Consumption order {OrderNumber} ({OrderId}) created for project {ProjectNumber}",
            order.OrderNumber, order.Id, project.ProjectNumber);

        return mapper.Map<ConsumptionOrderDto>(consumption);
    }
}