using AutoMapper;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Domain;

namespace MyApp.Audit.Application.Mapping;

/// <summary>AutoMapper profile for Audit domain entities and DTOs.</summary>
public class AuditMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the AuditMappingProfile class.
    /// including string conversion for the <see cref="ChangeTypeEnum"/> status field.
    /// </summary>
    public AuditMappingProfile()
    {
        CreateMap<PropertyChange, PropertyChangeDto>();

        CreateMap<EntityChange, EntityChangeDto>()
            .ForMember(d => d.ChangeType, opt => opt.MapFrom(s => s.ChangeType.ToString()));
    }
}
