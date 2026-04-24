
using AutoMapper;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Entities;

namespace MyApp.Billing.Application.Mapping;

public class BillingMappingProfile : Profile
{
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

