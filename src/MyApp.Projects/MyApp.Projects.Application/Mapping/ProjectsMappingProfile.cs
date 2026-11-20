using AutoMapper;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Domain.Entities;

namespace MyApp.Projects.Application.Mapping;

/// <summary>AutoMapper profile for the Projects service (enums are exposed as strings in read DTOs).</summary>
public class ProjectsMappingProfile : Profile
{
    /// <summary>Initializes a new instance of the <see cref="ProjectsMappingProfile"/> class.</summary>
    public ProjectsMappingProfile()
    {
        CreateMap<Project, ProjectDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Blocked, o => o.MapFrom(s => s.Blocked.ToString()));

        CreateMap<ProjectTask, ProjectTaskDto>()
            .ForMember(d => d.TaskType, o => o.MapFrom(s => s.TaskType.ToString()));

        CreateMap<ProjectPlanningLine, PlanningLineDto>()
            .ForMember(d => d.LineType, o => o.MapFrom(s => s.LineType.ToString()))
            .ForMember(d => d.Kind, o => o.MapFrom(s => s.Kind.ToString()))
            .ForMember(d => d.QuantityToInvoice, o => o.MapFrom(s => s.QuantityToInvoice))
            .ForMember(d => d.RemainingQuantity, o => o.MapFrom(s => s.RemainingQuantity));

        CreateMap<ProjectLedgerEntry, ProjectLedgerEntryDto>()
            .ForMember(d => d.EntryType, o => o.MapFrom(s => s.EntryType.ToString()))
            .ForMember(d => d.Kind, o => o.MapFrom(s => s.Kind.ToString()))
            .ForMember(d => d.SourceDocument, o => o.MapFrom(s => s.SourceDocument.ToString()));

        CreateMap<ProjectConsumptionOrder, ConsumptionOrderDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
        CreateMap<ProjectConsumptionOrderLine, ConsumptionOrderLineDto>();

        CreateMap<ProjectInvoiceRequest, InvoiceRequestDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
        CreateMap<ProjectInvoiceRequestLine, InvoiceRequestLineDto>();

        CreateMap<Resource, ResourceDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.ResourceGroupCode, o => o.MapFrom(s => s.ResourceGroup != null ? s.ResourceGroup.Code : null));
        CreateMap<ResourceGroup, ResourceGroupDto>();

        CreateMap<Currency, CurrencyDto>()
            .ForMember(d => d.IsLocalCurrency, o => o.Ignore())
            .ForMember(d => d.ExchangeRates, o => o.MapFrom(s => s.ExchangeRates.OrderByDescending(r => r.StartingDate)));
        CreateMap<ExchangeRate, ExchangeRateDto>();
    }
}
