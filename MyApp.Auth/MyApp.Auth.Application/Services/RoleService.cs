using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;

namespace MyApp.Auth.Application.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string name)
    {
        var role = await _roleManager.FindByNameAsync(name);
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        var roleExists = await _roleManager.RoleExistsAsync(createRoleDto.Name);
        if (roleExists)
        {
            _logger.LogWarning("Role already exists: {RoleName}", createRoleDto.Name);
            return null;
        }

        var role = new ApplicationRole(createRoleDto.Name)
        {
            Name = createRoleDto.Name,
            Description = createRoleDto.Description,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create role: {RoleName}", createRoleDto.Name);
            return null;
        }

        return _mapper.Map<RoleDto>(role);
    }

    public async Task<bool> UpdateRoleAsync(Guid roleId, CreateRoleDto updateRoleDto)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", roleId);
            return false;
        }

        role.Name = updateRoleDto.Name;
        role.Description = updateRoleDto.Description;
        role.UpdatedAt = DateTime.UtcNow;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to update role: {RoleId}", roleId);
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", roleId);
            return false;
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete role: {RoleId}", roleId);
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName)
    {
        var users = await _userRepository.GetByRoleAsync(roleName);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }
}
