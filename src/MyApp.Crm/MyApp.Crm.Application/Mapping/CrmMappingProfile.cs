using AutoMapper;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Domain.Accounts;
using MyApp.Crm.Domain.Activities;
using MyApp.Crm.Domain.Leads;
using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Application.Mapping;

public class CrmMappingProfile : Profile
{
    public CrmMappingProfile()
    {
        CreateMap<Account, AccountDto>();
        CreateMap<Contact, ContactDto>();

        CreateMap<Lead, LeadDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<Opportunity, OpportunityDto>()
            .ForMember(d => d.Stage, opt => opt.MapFrom(s => s.Stage.ToString()));

        CreateMap<OpportunityLine, OpportunityLineDto>()
            .ForMember(d => d.LineTotal, opt => opt.MapFrom(s => s.LineTotal));

        CreateMap<Activity, ActivityDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}

