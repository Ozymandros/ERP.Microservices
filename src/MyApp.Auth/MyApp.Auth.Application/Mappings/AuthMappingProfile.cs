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

        CreateMap<UpdateUserDto, ApplicationUser>();

        // Role mappings
        CreateMap<ApplicationRole, RoleDto>();
        CreateMap<CreateRoleDto, ApplicationRole>();

        // External login mapping
        CreateMap<ExternalLoginDto, ApplicationUser>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName));

        // Permission mappings
        CreateMap<Permission, PermissionDto>();
        CreateMap<CreatePermissionDto, Permission>();
        CreateMap<UpdatePermissionDto, Permission>();
    }
}
