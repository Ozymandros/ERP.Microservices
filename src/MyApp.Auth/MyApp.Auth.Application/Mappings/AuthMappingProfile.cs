using AutoMapper;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        // User mappings
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName));

        CreateMap<CreateUserDto, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username));

        CreateMap<UpdateUserDto, ApplicationUser>();

        // Role mappings
        CreateMap<ApplicationRole, RoleDto>();
        CreateMap<CreateRoleDto, ApplicationRole>();

        // External login mapping
        CreateMap<ExternalLoginDto, ApplicationUser>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName));

        // Permission mappings (Permission has primary constructor and required properties)
        // Note: PermissionService creates Permission manually, but mapping must compile
        CreateMap<Permission, PermissionDto>();
        // CreatePermissionDto -> Permission: must set required properties during construction
        CreateMap<CreatePermissionDto, Permission>()
            .ConstructUsing(src => CreatePermission(src.Module, src.Action, src.Description));
        // UpdatePermissionDto: Not mapped via AutoMapper (PermissionService updates manually)
    }

    private static Permission CreatePermission(string module, string action, string? description)
    {
        var permission = new Permission(Guid.NewGuid())
        {
            Module = module,
            Action = action,
            Description = description
        };
        return permission;
    }
}
