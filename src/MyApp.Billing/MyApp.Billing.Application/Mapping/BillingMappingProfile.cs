
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
    /// Initializes a new instance of the BillingMappingProfile class.
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

