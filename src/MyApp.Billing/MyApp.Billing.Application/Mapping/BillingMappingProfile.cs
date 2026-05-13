
using AutoMapper;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Application.Mapping;

/// <summary>
/// Provides Billing Mapping Profile functionality.
/// </summary>
public class BillingMappingProfile : Profile
{
    /// <summary>
    /// Billing Mapping Profile constructor. Configures the mappings between domain entities and DTOs, including custom mappings for status properties.
    /// </summary>
    public BillingMappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<CreditNote, CreditNoteDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        //CreateMap<CreditNoteLineData, CreditNoteLineDataDto>();
    }
}

