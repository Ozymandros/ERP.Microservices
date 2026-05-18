using AutoMapper;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;

namespace MyApp.Audit.Application.Mapping;

/// <summary>AutoMapper profile for Audit domain entities and DTOs.</summary>
public class AuditMappingProfile : Profile
{
    public AuditMappingProfile()
    {
        CreateMap<PropertyChange, PropertyChangeDto>();

        CreateMap<EntityChange, EntityChangeDto>()
            .ForMember(d => d.ChangeType, opt => opt.MapFrom(s => s.ChangeType.ToString()));
    }
}
